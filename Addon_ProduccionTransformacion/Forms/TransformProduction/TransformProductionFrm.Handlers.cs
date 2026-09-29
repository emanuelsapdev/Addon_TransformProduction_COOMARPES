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
        /// referencia al formulario, crea el campo "Producto" con el CFL filtrado por lista de
        /// materiales y aplica el post-proceso de la UI (editabilidad de columnas).
        /// </summary>
        public void ManejarCargaFormulario(string formUid)
        {
            var oForm = ConnectionSDK.UIAPI.Forms.Item(formUid);
            var ctx = ContextManager.ObtenerOCrear(oForm.TypeCount.ToString());
            ctx.FormTransfProd = oForm;

            CrearCampoProductoConFiltro(oForm);
            SincronizarCampoProducto(oForm);

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
        /// y la salida (IGO del principal) con una entrada (IGN). Persiste los DocEntry de
        /// reversión y deja el documento en estado Revertido.
        /// </summary>
        public void ManejarReversionTransformacion(SAPbouiCOM.Form oForm, out bool BubbleEvent)
        {
            BubbleEvent = true;
            var ctx = ContextManager.ObtenerOCrear(oForm.TypeCount.ToString());

            ReconstruirContextoDesdeForm(oForm, ctx);
            int docEntryUnit = Convert.ToInt32(ObtenerDocEntry(oForm));

            // La salida (lotes del principal) no se persiste en el UDO: si el documento fue
            // reabierto y no quedó selección en memoria, se reconstruye desde el Goods Issue
            // original (OIGE) para poder revertirla.
            if (ctx.InventoryGenExitsDocEntry > 0 && ctx.InventoryGenExitsData.Items.Count == 0)
                ctx.InventoryGenExitsData = ObtenerExitDataDesdeDocumento(ctx.InventoryGenExitsDocEntry);

            int respuesta = ConnectionSDK.UIAPI.MessageBox(
                "¿Confirma la reversión de la producción/transformación? Se generarán los documentos de reversión de entrada y salida.",
                1, "Cancelar", "Reversión");

            if (respuesta != 2) return; // 1 = botón "Cancelar"

            int entryRevDocEntry = 0, exitRevDocEntry = 0;

            try
            {
                ConnectionSDK.DIAPI.StartTransaction();

                // Revertir la SALIDA (IGO del principal) ⇒ generar una ENTRADA (IGN).
                if (ctx.InventoryGenExitsDocEntry > 0 && ctx.InventoryGenExitsData.Items.Count > 0)
                {
                    exitRevDocEntry = CrearEntradaMercancia(ctx.InventoryGenExitsData);
                    ReferenciarDocs(exitRevDocEntry, BoObjectTypes.oInventoryGenEntry,
                                    ctx.InventoryGenExitsDocEntry, ReferencedObjectTypeEnum.rot_GoodsIssue);
                }

                // Revertir la ENTRADA (IGN de subproductos) ⇒ generar una SALIDA (IGO).
                if (ctx.InventoryGenEntriesDocEntry > 0)
                {
                   
                    var entriesData = ObtenerInfoLineas(oForm); // subproductos de la línea UDO
                    if (entriesData.Items.Count > 0)
                    {
                        entryRevDocEntry = CrearSalidaMercancia(entriesData);
                        ReferenciarDocs(entryRevDocEntry, BoObjectTypes.oInventoryGenExit,
                                        ctx.InventoryGenEntriesDocEntry, ReferencedObjectTypeEnum.rot_GoodsReceipt);
                    }
                }

                ConnectionSDK.DIAPI.EndTransaction(BoWfTransOpt.wf_Commit);
            }
            catch (Exception ex)
            {
                NotificationService.MostrarError($"Error revirtiendo la producción: {ex.Message}");
                ConnectionSDK.DIAPI.EndTransaction(BoWfTransOpt.wf_RollBack);
                return;
            }

            ctx.PrincipalStatus = CONSTANTS.STAGING_STATUS.REVERT;
            ActualizarResultadoTransformacion(docEntryUnit, CONSTANTS.STAGING_STATUS.REVERT,
                entryRevDocEntry: entryRevDocEntry > 0 ? entryRevDocEntry : (int?)null,
                exitRevDocEntry: exitRevDocEntry > 0 ? exitRevDocEntry : (int?)null);

            EscribirEstadoCabecera(oForm, ctx.PrincipalStatus);
        }
    }
}