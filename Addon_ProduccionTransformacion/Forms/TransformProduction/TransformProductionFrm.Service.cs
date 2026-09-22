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

                return true;
            }
            catch (Exception ex)
            {
                ConnectionSDK.DIAPI.EndTransaction(BoWfTransOpt.wf_RollBack);   
                NotificationService.MostrarError($"Error creando la producción (Entrada/Salida de mercancía): {ex.Message}");
                return false;
            }
        }
    }
}