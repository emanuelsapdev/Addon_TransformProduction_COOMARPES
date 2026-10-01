using Addon_TransformProduction.Common;
using Addon_TransformProduction.Models;
using Addon_TransformProduction.Services;
using Addon_TransformProduction.Tools;
using SAPbobsCOM;
using System;
using System.Collections.Generic;

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

            // Mismo artículo: no recargar la grilla (pisaría cantidades/precios editados) ni
            // descartar los lotes.
            if (string.Equals(itemCode, LeerCodigoArticulo(oForm) ?? string.Empty, StringComparison.OrdinalIgnoreCase)) return;

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

            // Los lotes elegidos eran del artículo anterior: se descartan para que la salida no
            // salga con ellos.
            DescartarSeleccionLotes(oForm);

            if (string.IsNullOrWhiteSpace(itemCode))
                LimpiarGrillaMateriales(oForm);
            else
                ManejarCodigoArticuloPerdidaFoco(oForm);

            HabilitarBotonSeleccionLotes(oForm);
        }

        /// <summary>
        /// Descarta la selección de lotes de la salida (contexto y etiqueta) y cierra el
        /// formulario de lotes si quedó abierto.
        /// </summary>
        private void DescartarSeleccionLotes(SAPbouiCOM.Form oForm)
        {
            var ctx = ContextManager.ObtenerOCrear(oForm.TypeCount.ToString());
            ctx.PrincipalItemCode = LeerCodigoArticulo(oForm) ?? string.Empty;
            ctx.BatchHeadXml = string.Empty;
            ctx.InventoryGenExitsData = new InventoryGenModel();

            try
            {
                if (ctx.FormBatches != null) ctx.FormBatches.Close();
            }
            catch
            {
                // El formulario de lotes ya estaba cerrado.
            }
            ctx.FormBatches = null;

            LimpiarEtiquetaLotes(oForm);
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
        /// Menú "Nuevo" (modo agregar): reinicia el contexto y deja botones y edición como
        /// corresponde a un documento nuevo (ver FormularioEnModoAgregar); antes quedaban como en
        /// el registro navegado (p.ej. Revertir activo o la grilla bloqueada). Producto lo habilita
        /// SincronizarCampoProducto.
        /// </summary>
        public void ManejarModoAgregar(string formUid)
        {
            SAPbouiCOM.Form oForm = null;
            try
            {
                oForm = ConnectionSDK.UIAPI.Forms.Item(formUid);
                if (oForm.Mode != SAPbouiCOM.BoFormMode.fm_ADD_MODE) return;

                // Documento nuevo: se descarta el contexto del registro anterior y se resetean
                // botones y edición (si no, quedaban como en el registro navegado antes) (TP-11).
                var ctx = ContextManager.ObtenerOCrear(oForm.TypeCount.ToString());
                ctx.ResetearContexto();
                DescartarSeleccionLotes(oForm); // etiqueta y form de lotes del registro anterior

                FormularioEnModoAgregar(oForm);
                HabilitarBotonSeleccionLotes(oForm);
            }
            finally
            {
                if (oForm != null) MarshalGC.LiberarComObject(oForm);
            }
        }

        /// <summary>
        /// Activación del formulario (et_FORM_ACTIVATE): copia al contexto el artículo y la
        /// cantidad consumida de la cabecera, que usa el formulario de lotes. No toca el estado
        /// ni la entrada: el estado lo fijan la carga del registro y los flujos de
        /// Crear/Confirmar/Revertir, y la entrada se arma desde la grilla al momento de usarla.
        /// </summary>
        public void ManejarActivacionContexto(string formUid)
        {
            SAPbouiCOM.Form oForm = null;
            try
            {
                oForm = ConnectionSDK.UIAPI.Forms.Item(formUid);

                var ctx = ContextManager.ObtenerOCrear(oForm.TypeCount.ToString());
                ctx.PrincipalItemCode = LeerCodigoArticulo(oForm);
                ctx.PrincipalQuantityConsumed = EsCantidadConsumidaValida(LeerCantidadConsumida(oForm), out double qty) ? qty : 0;
            }
            finally
            {
                if (oForm != null) MarshalGC.LiberarComObject(oForm);
            }
        }

        /// <summary>
        /// Abre el registro <paramref name="docEntry"/> del UDO (después de Agregar o Confirmar) y
        /// le aplica el contexto, el campo Producto y la habilitación según su estado (p.ej.
        /// Completado: Documentos y Revertir activos). No depende de et_FORM_DATA_LOAD, que
        /// OpenForm no siempre dispara; si también se dispara, el resultado es el mismo.
        /// </summary>
        public void AbrirRegistro(string docEntry)
        {
            if (string.IsNullOrWhiteSpace(docEntry)) return;

            ConnectionSDK.UIAPI.OpenForm(SAPbouiCOM.BoFormObjectEnum.fo_UserDefinedObject, CONSTANTS.UDO.OBJECT_CODE, docEntry);

            SAPbouiCOM.Form oForm = null;
            try
            {
                oForm = ConnectionSDK.UIAPI.Forms.ActiveForm;
                if (oForm == null || oForm.TypeEx != FormType) return;

                string formUid = oForm.UniqueID;
                ManejarSincronizacionProducto(formUid);
                ManejarRegistroCargado(formUid);
            }
            finally
            {
                if (oForm != null) MarshalGC.LiberarComObject(oForm);
            }
        }

        /// <summary>
        /// Después de Agregar el formulario sigue abierto en un documento nuevo: se descarta la
        /// selección de lotes (cierra el form de lotes) y se reinicia el contexto, sin eliminarlo,
        /// para que se conserve la referencia al formulario (FormTransfProd) y la etiqueta de lotes
        /// siga actualizándose (TP-12).
        /// </summary>
        public void ReiniciarContextoFormulario(string formUid)
        {
            SAPbouiCOM.Form oForm = null;
            try
            {
                oForm = ConnectionSDK.UIAPI.Forms.Item(formUid);
                DescartarSeleccionLotes(oForm);
                ContextManager.ObtenerOCrear(oForm.TypeCount.ToString()).ResetearContexto();
            }
            finally
            {
                if (oForm != null) MarshalGC.LiberarComObject(oForm);
            }
        }

        /// <summary>
        /// Después de Confirmar el formulario sigue abierto mostrando el registro (ya recargado):
        /// se descarta la selección de lotes y se reconstruye el contexto desde el registro, sin
        /// eliminarlo (TP-12).
        /// </summary>
        public void ManejarProduccionConfirmada(string formUid)
        {
            SAPbouiCOM.Form oForm = null;
            try
            {
                oForm = ConnectionSDK.UIAPI.Forms.Item(formUid);
                DescartarSeleccionLotes(oForm);
            }
            finally
            {
                if (oForm != null) MarshalGC.LiberarComObject(oForm);
            }

            ManejarRegistroCargado(formUid);
        }

        /// <summary>
        /// Registro recién cargado (et_FORM_DATA_LOAD con ActionSuccess): reinicia el contexto,
        /// lo reconstruye desde el registro cargado, limpia la etiqueta de lotes y aplica la
        /// habilitación de campos/botones según su estado.
        /// </summary>
        public void ManejarRegistroCargado(string formUid)
        {
            SAPbouiCOM.Form oForm = null;
            try
            {
                oForm = ConnectionSDK.UIAPI.Forms.Item(formUid);

                var ctx = ContextManager.ObtenerOCrear(oForm.TypeCount.ToString());
                ctx.ResetearContexto();
                ReconstruirContextoDesdeForm(oForm, ctx);
                LimpiarEtiquetaLotes(oForm);

                oForm.Freeze(true);
                AplicarHabilitacionPorEstado(oForm, ctx.PrincipalStatus);
            }
            finally
            {
                if (oForm != null)
                {
                    oForm.Freeze(false);
                    MarshalGC.LiberarComObject(oForm);
                }
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
        /// vía documentos referenciados y actualiza el registro <paramref name="udoDocEntry"/> del
        /// UDO a Completado con sus DocEntry. Los documentos y el update del UDO van en la misma
        /// transacción: si algo falla se deshace todo y el registro sigue Pendiente (sin stock
        /// movido), así no se puede volver a confirmar duplicando movimientos.
        /// </summary>
        public bool CrearProduccion(TransformProductionContext ctx, int udoDocEntry, out int entryDocEntry, out int exitDocEntry)
        {
            entryDocEntry = 0;
            exitDocEntry = 0;
            try
            {
                if (udoDocEntry <= 0) throw new ArgumentOutOfRangeException(nameof(udoDocEntry));

                ConnectionSDK.DIAPI.StartTransaction();
                // Entrada de mercancía: subproductos obtenidos de la transformación.
                entryDocEntry = CrearEntradaMercancia(ctx.InventoryGenEntriesData);

                // Salida de mercancía: artículo principal consumido, grabada ya referenciando la
                // entrada recién creada ("Documentos referenciados").
                exitDocEntry = CrearSalidaMercancia(ctx.InventoryGenExitsData,
                    entryDocEntry, ReferencedObjectTypeEnum.rot_GoodsReceipt);

                // La entrada se creó antes que la salida: su referencia a la salida se agrega acá,
                // dentro de la transacción; si falla, se deshace todo (TP-14).
                ReferenciarDocs(entryDocEntry, BoObjectTypes.oInventoryGenEntry,
                    exitDocEntry, ReferencedObjectTypeEnum.rot_GoodsIssue);

                if (!ActualizarResultadoTransformacion(udoDocEntry, CONSTANTS.STAGING_STATUS.COMPLETED, entryDocEntry, exitDocEntry))
                    throw new Exception(CONSTANTS.MESSAGES.COMPLETE_UDO_UPDATE_ERROR);

                ConnectionSDK.DIAPI.EndTransaction(BoWfTransOpt.wf_Commit);

                ctx.PrincipalStatus = CONSTANTS.STAGING_STATUS.COMPLETED;

                return true;
            }
            catch (Exception ex)
            {
                if (ConnectionSDK.DIAPI.InTransaction)
                    ConnectionSDK.DIAPI.EndTransaction(BoWfTransOpt.wf_RollBack);

                entryDocEntry = 0;
                exitDocEntry = 0;
                NotificationService.MostrarError($"Error creando la producción (Entrada/Salida de mercancía): {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Lee la cabecera del UDO desde la base (Repository → Mapper). Devuelve null si el
        /// registro no existe.
        /// </summary>
        public TransformProductionUdoModel LeerCabeceraUdoPersistida(int docEntry)
        {
            if (docEntry <= 0) return null;

            Recordset oRecordSet = null;
            try
            {
                oRecordSet = (Recordset)ConnectionSDK.DIAPI.GetBusinessObject(BoObjectTypes.BoRecordset);
                oRecordSet = ObtenerCabeceraUdoPersistida(oRecordSet, docEntry);
                return MapearCabeceraUdo(oRecordSet);
            }
            finally
            {
                if (oRecordSet != null) MarshalGC.LiberarComObject(oRecordSet);
            }
        }
    

        /// <summary>
        /// After-add del UDO (et_FORM_DATA_ADD con ActionSuccess). El registro ya está grabado
        /// en estado Pendiente; si el usuario eligió "Crear" se generan la Entrada/Salida con los
        /// datos validados en el BeforeAction y solo si se crean se pasa a Completado. Si fallan,
        /// el documento queda Pendiente para confirmarlo después.
        /// </summary>
        public void ManejarProduccionAgregada(int docEntry, TransformProductionContext ctx)
        {
            if (!ctx.CompletarAlAgregar) return;

            ctx.CompletarAlAgregar = false;
            ctx.InventoryGenEntriesData = ctx.EntradasAlAgregar ?? new InventoryGenModel();
            ctx.InventoryGenExitsData = ctx.SalidasAlAgregar ?? new InventoryGenModel();
            ctx.EntradasAlAgregar = null;
            ctx.SalidasAlAgregar = null;

            if (docEntry <= 0)
            {
                NotificationService.MostrarAlerta(CONSTANTS.MESSAGES.CREATE_DOCS_FAILED_PENDING);
                return;
            }

            // Entrada/Salida + update a Completado en una sola transacción (TP-04).
            if (!CrearProduccion(ctx, docEntry, out int entryDocEntry, out int exitDocEntry))
            {
                ctx.PrincipalStatus = CONSTANTS.STAGING_STATUS.PENDING;
                NotificationService.MostrarAlerta(CONSTANTS.MESSAGES.CREATE_DOCS_FAILED_PENDING);
                return;
            }

            ctx.InventoryGenEntriesDocEntry = entryDocEntry;
            ctx.InventoryGenExitsDocEntry = exitDocEntry;

            AbrirDocumentosRelacionados(ctx);
        }
    

        /// <summary>
        /// Códigos de almacén que existen en SAP (OWHS) entre los indicados (Repository → Mapper).
        /// </summary>
        public HashSet<string> ObtenerAlmacenesExistentes(ICollection<string> whsCodes)
        {
            if (whsCodes == null || whsCodes.Count == 0) return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            Recordset oRecordSet = null;
            try
            {
                oRecordSet = (Recordset)ConnectionSDK.DIAPI.GetBusinessObject(BoObjectTypes.BoRecordset);
                oRecordSet = ObtenerAlmacenesExistentes(oRecordSet, whsCodes);
                return MapearCodigosAlmacen(oRecordSet);
            }
            finally
            {
                if (oRecordSet != null) MarshalGC.LiberarComObject(oRecordSet);
            }
        }
    

        /// <summary>
        /// Salida a revertir reconstruida desde el Goods Issue original (lotes y cantidades) con
        /// el costo unitario con el que salió cada línea: la reversión la reingresa a ese mismo
        /// valor y no al costo actual. Si una línea no tiene costo, se deja en 0 (SAP valúa al
        /// costo actual).
        /// </summary>
        public InventoryGenModel ObtenerSalidaParaReversion(int exitDocEntry)
        {
            var model = ObtenerExitDataDesdeDocumento(exitDocEntry);
            if (model.Items.Count == 0) return model;

            Recordset oRecordSet = null;
            try
            {
                oRecordSet = (Recordset)ConnectionSDK.DIAPI.GetBusinessObject(BoObjectTypes.BoRecordset);
                oRecordSet = ObtenerCostosLineasSalida(oRecordSet, exitDocEntry);
                var costos = MapearCostosPorLinea(oRecordSet);

                // Las líneas del modelo están en el orden del documento (VisOrder).
                for (int i = 0; i < model.Items.Count; i++)
                {
                    if (costos.TryGetValue(i, out decimal costo) && costo > 0)
                        model.Items[i].Price = costo;
                }
            }
            finally
            {
                if (oRecordSet != null) MarshalGC.LiberarComObject(oRecordSet);
            }

            return model;
        }
    

        /// <summary>
        /// Códigos de artículo que se manejan por lotes entre los indicados (Repository → Mapper).
        /// </summary>
        public HashSet<string> ObtenerArticulosConLote(ICollection<string> itemCodes)
        {
            if (itemCodes == null || itemCodes.Count == 0) return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            Recordset oRecordSet = null;
            try
            {
                oRecordSet = (Recordset)ConnectionSDK.DIAPI.GetBusinessObject(BoObjectTypes.BoRecordset);
                oRecordSet = ObtenerArticulosConLote(oRecordSet, itemCodes);
                return MapearCodigosArticulo(oRecordSet);
            }
            finally
            {
                if (oRecordSet != null) MarshalGC.LiberarComObject(oRecordSet);
            }
        }
    

        /// <summary>
        /// Códigos de moneda que existen en SAP (OCRN) entre los indicados (Repository → Mapper).
        /// </summary>
        public HashSet<string> ObtenerMonedasExistentes(ICollection<string> currencyCodes)
        {
            if (currencyCodes == null || currencyCodes.Count == 0) return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            Recordset oRecordSet = null;
            try
            {
                oRecordSet = (Recordset)ConnectionSDK.DIAPI.GetBusinessObject(BoObjectTypes.BoRecordset);
                oRecordSet = ObtenerMonedasExistentes(oRecordSet, currencyCodes);
                return MapearColumnaTexto(oRecordSet, "CurrCode");
            }
            finally
            {
                if (oRecordSet != null) MarshalGC.LiberarComObject(oRecordSet);
            }
        }

        /// <summary>
        /// Moneda local (OADM."MainCurncy") y moneda de sistema (OADM."SysCurrncy").
        /// </summary>
        public void ObtenerMonedasSociedad(out string monedaLocal, out string monedaSistema)
        {
            monedaLocal = string.Empty;
            monedaSistema = string.Empty;

            Recordset oRecordSet = null;
            try
            {
                oRecordSet = (Recordset)ConnectionSDK.DIAPI.GetBusinessObject(BoObjectTypes.BoRecordset);
                oRecordSet = ObtenerMonedasSociedad(oRecordSet);
                if (oRecordSet.EoF) return;

                monedaLocal = Convert.ToString(oRecordSet.Fields.Item("MainCurncy").Value)?.Trim() ?? string.Empty;
                monedaSistema = Convert.ToString(oRecordSet.Fields.Item("SysCurrncy").Value)?.Trim() ?? string.Empty;
            }
            finally
            {
                if (oRecordSet != null) MarshalGC.LiberarComObject(oRecordSet);
            }
        }

        /// <summary>
        /// Monedas, entre las indicadas, con tipo de cambio de hoy mayor que cero (Repository → Mapper).
        /// </summary>
        public HashSet<string> ObtenerMonedasConTipoCambioHoy(ICollection<string> currencyCodes)
        {
            if (currencyCodes == null || currencyCodes.Count == 0) return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            Recordset oRecordSet = null;
            try
            {
                oRecordSet = (Recordset)ConnectionSDK.DIAPI.GetBusinessObject(BoObjectTypes.BoRecordset);
                oRecordSet = ObtenerMonedasConTipoCambioHoy(oRecordSet, currencyCodes);
                return MapearColumnaTexto(oRecordSet, "Currency");
            }
            finally
            {
                if (oRecordSet != null) MarshalGC.LiberarComObject(oRecordSet);
            }
        }
    }
}