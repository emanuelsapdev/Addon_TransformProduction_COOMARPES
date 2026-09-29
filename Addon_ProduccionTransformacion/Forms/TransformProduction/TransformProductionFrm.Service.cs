using Addon_TransformProduction.Common;
using Addon_TransformProduction.Models;
using Addon_TransformProduction.Services;
using Addon_TransformProduction.Tools;
using SAPbobsCOM;
using System;

namespace Addon_TransformProduction.Forms.TransformProduction
{
    public partial class TransformProductionFrm
    {
        /// <summary>
        /// Orquesta el flujo al perder el foco el campo de código de artículo de la cabecera:
        /// lee el código, lo guarda en el contexto, consulta el BOM (Repository), lo mapea
        /// (Mapper) y pinta la grilla (FormReader).
        /// </summary>
        public void ManejarCodigoArticuloPerdidaFoco(SAPbouiCOM.Form oForm)
        {
            string itemCode = LeerCodigoArticulo(oForm);
            if (string.IsNullOrWhiteSpace(itemCode)) return;

            var ctx = ContextManager.ObtenerOCrear(oForm.TypeCount.ToString());
            ctx.PrincipalItemCode = itemCode;

            Recordset oRecordSet = null;
            try
            {
                oRecordSet = (Recordset)ConnectionSDK.DIAPI.GetBusinessObject(BoObjectTypes.BoRecordset);
                oRecordSet = ObtenerListaMateriales(oRecordSet, itemCode);

                var data = MapearListaMateriales(ref oRecordSet);

                if (data != null && data.Count > 0)
                {
                    PoblarGrillaMateriales(oForm, data);
                }
                else
                {
                    LimpiarGrillaMateriales(oForm);
                    NotificationService.MostrarAlerta(
                        $"No se encontraron datos para el código de artículo: {itemCode}");
                }
            }
            finally
            {
                if (oRecordSet != null) MarshalGC.LiberarComObject(oRecordSet);
            }
        }

        /// <summary>
        /// El usuario eligió un artículo en el CFL filtrado del campo espejo de "Producto":
        /// lo escribe en el UDF de la cabecera y dispara el mismo flujo que el cambio de
        /// artículo (BOM → grilla, botón de lotes).
        /// </summary>
        public void ManejarSeleccionProductoCfl(SAPbouiCOM.Form oForm, SAPbouiCOM.DataTable oSeleccion)
        {
            if (oSeleccion == null || oSeleccion.Rows.Count == 0) return; // CFL cancelado

            string itemCode = Convert.ToString(oSeleccion.GetValue(CONSTANTS.UID.CHOOSE_FROM_LIST.ALIAS, 0))?.Trim();
            if (string.IsNullOrWhiteSpace(itemCode)) return;

            AplicarCambioProducto(oForm, itemCode);
        }

        /// <summary>
        /// Al salir del campo espejo de "Producto" (código tipeado a mano): si cambió respecto
        /// del UDF de la cabecera, lo copia y dispara el flujo de cambio de artículo. Si no
        /// cambió (p.ej. ya se aplicó desde el CFL) no hace nada, para no repintar la grilla.
        /// </summary>
        public void ManejarProductoPerdidaFoco(SAPbouiCOM.Form oForm)
        {
            string itemCode = LeerCodigoArticuloEspejo(oForm) ?? string.Empty;
            if (string.Equals(itemCode, LeerCodigoArticulo(oForm) ?? string.Empty, StringComparison.OrdinalIgnoreCase)) return;

            AplicarCambioProducto(oForm, itemCode);
        }

        private void AplicarCambioProducto(SAPbouiCOM.Form oForm, string itemCode)
        {
            EscribirCodigoArticulo(oForm, itemCode);
            ManejarCodigoArticuloPerdidaFoco(oForm);
            HabilitarBotonSeleccionLotes(oForm);
        }

        /// <summary>
        /// Al activar el formulario asegura el campo "Producto" con CFL filtrado (idempotente:
        /// solo trabaja la primera vez o si un paso anterior falló) y, si se acaba de crear, lo
        /// sincroniza con el registro.
        /// </summary>
        public void ManejarActivacionCampoProducto(string formUid)
        {
            SAPbouiCOM.Form oForm = null;
            try
            {
                oForm = ConnectionSDK.UIAPI.Forms.Item(formUid);
                if (AsegurarCampoProductoConFiltro(oForm))
                    SincronizarCampoProducto(oForm);
            }
            finally
            {
                if (oForm != null) MarshalGC.LiberarComObject(oForm);
            }
        }

        /// <summary>
        /// Re-sincroniza el campo espejo de "Producto" con el registro cargado (navegación,
        /// cambio de modo, reactivación del formulario).
        /// </summary>
        public void ManejarSincronizacionProducto(string formUid)
        {
            SAPbouiCOM.Form oForm = null;
            try
            {
                oForm = ConnectionSDK.UIAPI.Forms.Item(formUid);
                SincronizarCampoProducto(oForm);
            }
            finally
            {
                if (oForm != null) MarshalGC.LiberarComObject(oForm);
            }
        }

        /// <summary>
        /// Después de agregar el documento el formulario queda en uno nuevo: vacía el campo
        /// espejo de "Producto".
        /// </summary>
        public void ManejarProductoTrasAgregar(string formUid)
        {
            SAPbouiCOM.Form oForm = null;
            try
            {
                oForm = ConnectionSDK.UIAPI.Forms.Item(formUid);
                LimpiarCampoProductoEspejo(oForm);
            }
            finally
            {
                if (oForm != null) MarshalGC.LiberarComObject(oForm);
            }
        }

        /// <summary>
        /// Orquesta el flujo al perder el foco el campo de cantidad consumida de la cabecera:
        /// lee la cantidad, la valida y actualiza el contexto.
        /// </summary>
        public void ManejarCantidadConsumidaPerdidaFoco(SAPbouiCOM.Form oForm)
        {
            string qtyConsumed = LeerCantidadConsumida(oForm);

            if (!EsCantidadConsumidaValida(qtyConsumed, out double qty)) return;

            var context = ContextManager.ObtenerOCrear(oForm.TypeCount.ToString());
            context.PrincipalQuantityConsumed = qty;
        }

        /// <summary>
        /// Orquesta la creación completa de la producción: crea la entrada de mercancía
        /// (subproductos obtenidos, <see cref="TransformProductionContext.InventoryGenEntriesData"/>)
        /// y la salida de mercancía (artículo principal consumido,
        /// <see cref="TransformProductionContext.InventoryGenExitsData"/>), las relaciona entre sí
        /// vía documentos referenciados y, si ambas se crean sin error, deja que continúe la
        /// creación del registro del UDO en estado Completado.
        /// </summary>
        public bool CrearProduccion(TransformProductionContext ctx, out int entryDocEntry, out int exitDocEntry)
        {
            entryDocEntry = 0;
            exitDocEntry = 0;
            try
            {
                ConnectionSDK.DIAPI.StartTransaction();
                // Entrada de mercancía: subproductos obtenidos de la transformación.
                entryDocEntry = CrearEntradaMercancia(ctx.InventoryGenEntriesData);

                // Salida de mercancía: artículo principal consumido, referenciando la entrada
                // recién creada para que ambos documentos queden vinculados entre sí
                // ("Documentos Referenciados").
                exitDocEntry = CrearSalidaMercancia(ctx.InventoryGenExitsData);

                ctx.PrincipalStatus = CONSTANTS.STAGING_STATUS.COMPLETED;

                ConnectionSDK.DIAPI.EndTransaction(BoWfTransOpt.wf_Commit);

                ReferenciarDocs(entryDocEntry, BoObjectTypes.oInventoryGenEntry, exitDocEntry, ReferencedObjectTypeEnum.rot_GoodsIssue);
                ReferenciarDocs(exitDocEntry, BoObjectTypes.oInventoryGenExit, entryDocEntry, ReferencedObjectTypeEnum.rot_GoodsReceipt);

                return true;
            }
            catch (Exception ex)
            {
                NotificationService.MostrarError($"Error creando la producción (Entrada/Salida de mercancía): {ex.Message}");
                ConnectionSDK.DIAPI.EndTransaction(BoWfTransOpt.wf_RollBack);   
                return false;
            }
        }
    }
}