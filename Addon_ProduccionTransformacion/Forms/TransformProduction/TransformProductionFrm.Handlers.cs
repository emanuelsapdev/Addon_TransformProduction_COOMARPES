using Addon_TransformProduction.Common;
using Addon_TransformProduction.Forms.WindowBatches;
using Addon_TransformProduction.Services;
using Addon_TransformProduction.Tools;
using SAPbobsCOM;
using SAPbouiCOM;
using System;

namespace Addon_TransformProduction.Forms.TransformProduction
{
    public partial class TransformProductionFrm
    {
        /// <summary>
        /// Al abrir el formulario (et_FORM_LOAD) crea el contexto de la producción, guarda la
        /// referencia al formulario y aplica el post-proceso de la UI (editabilidad de columnas).
        /// El campo "Producto" con CFL filtrado se crea recién en et_FORM_ACTIVATE (ver
        /// ManejarActivacionCampoProducto).
        /// </summary>
        public void ManejarCargaFormulario(string formUid)
        {
            var oForm = ConnectionSDK.UIAPI.Forms.Item(formUid);
            var ctx = ContextManager.ObtenerOCrear(oForm.TypeCount.ToString());
            ctx.FormTransfProd = oForm;

            //InicializarFormulario(oForm);
        }

        /// <summary>
        /// Al cerrar el formulario (et_FORM_CLOSE) cierra el formulario de lotes asociado (si
        /// quedó abierto) y elimina el contexto de la producción.
        /// </summary>
        public void ManejarCierreFormulario(string formUid)
        {
            var oForm = ConnectionSDK.UIAPI.Forms.Item(formUid);
            var ctx = ContextManager.ObtenerOCrear(oForm.TypeCount.ToString());
            try
            {
                if (ctx.FormBatches != null) ctx.FormBatches.Close();
            }
            catch { }

            ContextManager.Eliminar(oForm.TypeCount.ToString());
        }

        /// <summary>
        /// Abre el formulario de selección de lotes al presionar el botón de cabecera
        /// (CONSTANTS.UID.BUTTONS.LOTE_SELECT). Delegado desde OnItemEvent. Le pasa el
        /// TypeCount del formulario de producción para que se cree/use el formulario de
        /// lotes específico de esa instancia.
        /// </summary>
        public void AbrirFormularioLotes(SAPbouiCOM.Form oForm)
        {
            try
            {
                var windowBatches = new WindowBatchesFrm();
                windowBatches.MostrarFormulario(oForm.TypeCount.ToString());
            }
            catch (Exception ex)
            {
                NotificationService.MostrarError($"Error abriendo el formulario de lotes: {ex.Message}");
            }
        }

        /// <summary>
        /// Activa el botón de lotes cuando hay un artículo cargado y una cantidad válida en la
        /// cabecera; lo desactiva en caso contrario.
        /// </summary>
        public void HabilitarBotonSeleccionLotes(SAPbouiCOM.Form oForm)
        {
            string itemCode = LeerCodigoArticulo(oForm);
            string qtyConsumed = LeerCantidadConsumida(oForm);
            string status = LeereEstadoCabecera(oForm);

            if(status == CONSTANTS.STAGING_STATUS.PENDING)
            {
                SAPbouiCOM.Item oItem = oForm.Items.Item(CONSTANTS.UID.BUTTONS.LOTE_SELECT);
                oItem.Enabled = !string.IsNullOrWhiteSpace(itemCode) && EsCantidadConsumidaValida(qtyConsumed, out _);
            }
        }

        /// <summary>
        /// El usuario seleccionó el menú del addon que abre este formulario: reutiliza la
        /// instancia existente si ya está abierta, o crea un documento nuevo de producción.
        /// </summary>
        public void ManejarMenuRegistrado()
        {
            if (MostrarFormularioExistente()) return;

            AbrirFormularioNuevo();
        }

        private bool MostrarFormularioExistente()
        {
            try
            {
                var oExisting = ConnectionSDK.UIAPI.Forms.Item(CONSTANTS.UID.FORM);
                oExisting.Select();
                oExisting.Visible = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void ManejarCreacionProduccion(SAPbouiCOM.Form oForm, out bool BubbleEvent)
        {
            BubbleEvent = true;
            var ctx = ContextManager.ObtenerOCrear(oForm.TypeCount.ToString());
            if (!ValidarFormulario(oForm, ctx)) 
            { 
                BubbleEvent = false; 
                return; 
            }

            var oMatrix = (Matrix)oForm.Items.Item(CONSTANTS.UID.GRID).Specific;
            oMatrix.FlushToDataSource();

            
            ctx.InventoryGenEntriesData = ObtenerInfoLineas(oForm);

            if (!CrearProduccion(ctx, out int entryDocEntry, out int exitDocEntry))
            {
                BubbleEvent = false;
                return;
            }

            EscribirEstadoCabecera(oForm, ctx.PrincipalStatus);

            ctx.InventoryGenEntriesDocEntry = entryDocEntry;
            ctx.InventoryGenExitsDocEntry = exitDocEntry;
        }
        public void ManejarCreacionProduccionPendiente(SAPbouiCOM.Form oForm, out bool BubbleEvent)
        {
            BubbleEvent = true;

            var ctx = ContextManager.ObtenerOCrear(oForm.TypeCount.ToString());
            if (!ValidarFormulario(oForm, ctx))
            {
                BubbleEvent = false;
                return;
            }

            //CrearProduccionPendiente(ctx);
        }

        /// <summary>
        /// Crea los documentos definitivos (IGN de subproductos + IGO del principal) desde un
        /// documento Pendiente reabierto: la entrada se reconstruye desde las líneas del UDO y
        /// la salida desde la selección de lotes en memoria (el usuario la re-elige al abrir el
        /// documento Pendiente; no se persiste en la base).
        /// </summary>
        public void ManejarConfirmarProduccion(SAPbouiCOM.Form oForm, out bool BubbleEvent)
        {
            BubbleEvent = true;
            var ctx = ContextManager.ObtenerOCrear(oForm.TypeCount.ToString());

            if (!ValidarFormulario(oForm, ctx)) { BubbleEvent = false; return; }

            var oMatrix = (Matrix)oForm.Items.Item(CONSTANTS.UID.GRID).Specific;
            oMatrix.FlushToDataSource();
            ctx.InventoryGenEntriesData = ObtenerInfoLineas(oForm); // entrada desde líneas UDO

            if (!CrearProduccion(ctx, out int entryDocEntry, out int exitDocEntry))
            { BubbleEvent = false; return; }

            ctx.InventoryGenEntriesDocEntry = entryDocEntry;
            ctx.InventoryGenExitsDocEntry = exitDocEntry;

            ActualizarResultadoTransformacion(
                Convert.ToInt32(ObtenerDocEntry(oForm)),
                CONSTANTS.STAGING_STATUS.COMPLETED,
                entryDocEntry, exitDocEntry);

            EscribirEstadoCabecera(oForm, ctx.PrincipalStatus);
            AbrirDocumentosRelacionados(ctx);
        }

        /// <summary>
        /// Abre los documentos de mercancía (IGN/IGO) desde los DocEntry persistidos en la
        /// cabecera del UDO (restart-safe), no desde el contexto en memoria.
        /// </summary>
        public void ManejarVerDocumentos(string formUid)
        {
            SAPbouiCOM.Form oForm = null;
            try
            {
                oForm = ConnectionSDK.UIAPI.Forms.Item(formUid);
                var ctx = ContextManager.ObtenerOCrear(oForm.TypeCount.ToString());
                ReconstruirContextoDesdeForm(oForm, ctx);
                AbrirDocumentosRelacionados(ctx);
            }
            finally
            {
                if (oForm != null)
                {
                    MarshalGC.LiberarComObject(oForm);
                    oForm = null;
                }
            }
        }

        /// <summary>
        /// Reversión cruzada: la entrada (IGN de subproductos) se revierte con una salida (IGO)
        /// y la salida (IGO del principal) con una entrada (IGN). Solo se permite una vez, sobre
        /// un documento Completado y sin documentos de reversión, validado contra la base (el
        /// formulario puede estar desactualizado). Los documentos de reversión y el update del
        /// UDO (estado Revertido + DocEntry de reversión) van en la misma transacción; al
        /// terminar se recarga el registro para que el formulario quede en estado Revertido.
        /// </summary>
        public void ManejarReversionTransformacion(SAPbouiCOM.Form oForm, out bool BubbleEvent)
        {
            BubbleEvent = true;
            var ctx = ContextManager.ObtenerOCrear(oForm.TypeCount.ToString());

            int.TryParse(ObtenerDocEntry(oForm), out int docEntryUnit);
            var udo = LeerCabeceraUdoPersistida(docEntryUnit);

            if (!PuedeRevertirse(udo, out string motivo))
            {
                NotificationService.MostrarAlerta(motivo);
                if (udo != null)
                {
                    RecargarRegistro(oForm, docEntryUnit);
                    AplicarHabilitacionPorEstado(oForm, udo.Status);
                }
                else
                {
                    HabilitarBotonRevertir(oForm, false);
                }
                return;
            }

            AplicarCabeceraUdoAContexto(udo, ctx);

            // La salida (lotes del principal) no se persiste en el UDO: si el documento fue
            // reabierto y no quedó selección en memoria, se reconstruye desde el Goods Issue
            // original (OIGE) para poder revertirla.
            if (ctx.InventoryGenExitsDocEntry > 0 && ctx.InventoryGenExitsData.Items.Count == 0)
                ctx.InventoryGenExitsData = ObtenerExitDataDesdeDocumento(ctx.InventoryGenExitsDocEntry);

            int respuesta = ConnectionSDK.UIAPI.MessageBox(
                "¿Confirma la reversión de la producción/transformación? Se generarán los documentos de reversión de entrada y salida.",
                1, "Cancelar", "Reversión");

            if (respuesta != 2) return; // 1 = botón "Cancelar"

            // Se deshabilita antes de generar documentos para que un segundo click no dispare
            // otra reversión mientras se procesa.
            HabilitarBotonRevertir(oForm, false);

            int entryRevDocEntry = 0, exitRevDocEntry = 0;

            try
            {
                ConnectionSDK.DIAPI.StartTransaction();

                // Revertir la SALIDA (IGO del principal) ⇒ generar una ENTRADA (IGN).
                if (ctx.InventoryGenExitsDocEntry > 0 && ctx.InventoryGenExitsData.Items.Count > 0)
                {
                    entryRevDocEntry = CrearEntradaMercancia(ctx.InventoryGenExitsData);
                    ReferenciarDocs(entryRevDocEntry, BoObjectTypes.oInventoryGenEntry,
                                    ctx.InventoryGenExitsDocEntry, ReferencedObjectTypeEnum.rot_GoodsIssue);
                }

                // Revertir la ENTRADA (IGN de subproductos) ⇒ generar una SALIDA (IGO).
                if (ctx.InventoryGenEntriesDocEntry > 0)
                {
                    var entriesData = ObtenerInfoLineas(oForm); // subproductos de la línea UDO
                    if (entriesData.Items.Count > 0)
                    {
                        exitRevDocEntry = CrearSalidaMercancia(entriesData);
                        ReferenciarDocs(exitRevDocEntry, BoObjectTypes.oInventoryGenExit,
                                        ctx.InventoryGenEntriesDocEntry, ReferencedObjectTypeEnum.rot_GoodsReceipt);
                    }
                }

                // El estado Revertido se persiste en la misma transacción: si falla, se deshacen
                // también los documentos de reversión y el registro sigue Completado.
                bool actualizado = ActualizarResultadoTransformacion(docEntryUnit, CONSTANTS.STAGING_STATUS.REVERT,
                    entryRevDocEntry: entryRevDocEntry > 0 ? entryRevDocEntry : (int?)null,
                    exitRevDocEntry: exitRevDocEntry > 0 ? exitRevDocEntry : (int?)null);

                if (!actualizado)
                    throw new Exception(CONSTANTS.MESSAGES.REVERT_UDO_UPDATE_ERROR);

                ConnectionSDK.DIAPI.EndTransaction(BoWfTransOpt.wf_Commit);
            }
            catch (Exception ex)
            {
                if (ConnectionSDK.DIAPI.InTransaction)
                    ConnectionSDK.DIAPI.EndTransaction(BoWfTransOpt.wf_RollBack);

                NotificationService.MostrarError($"Error revirtiendo la producción: {ex.Message}");
                HabilitarBotonRevertir(oForm, true);
                return;
            }

            ctx.PrincipalStatus = CONSTANTS.STAGING_STATUS.REVERT;

            // Recargar el registro (en vez de escribir el combo) deja el formulario en modo OK
            // con el estado y los DocEntry de reversión persistidos.
            RecargarRegistro(oForm, docEntryUnit);
            AplicarHabilitacionPorEstado(oForm, ctx.PrincipalStatus);
        }
    }
}