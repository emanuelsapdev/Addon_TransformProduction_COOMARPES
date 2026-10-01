using Addon_TransformProduction.Common;
using Addon_TransformProduction.Forms.WindowBatches;
using Addon_TransformProduction.Models;
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
        }

        /// <summary>
        /// Al cerrar el formulario (et_FORM_CLOSE) cierra el formulario de lotes asociado (si
        /// quedó abierto) y elimina el contexto de la producción. Solo para el cierre real: si el
        /// formulario sigue abierto se reinicia el contexto (ver ReiniciarContextoFormulario).
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

            // Documento nuevo (modo agregar) o Pendiente: se pueden elegir lotes.
            if (status == CONSTANTS.STAGING_STATUS.PENDING || oForm.Mode == SAPbouiCOM.BoFormMode.fm_ADD_MODE)
            {
                SAPbouiCOM.Item oItem = oForm.Items.Item(CONSTANTS.UID.BUTTONS.LOTE_SELECT);
                oItem.Enabled = !string.IsNullOrWhiteSpace(itemCode) && EsCantidadConsumidaValida(qtyConsumed, out _);
            }
        }

        /// <summary>
        /// BeforeAction de "Crear" con la opción Completado: valida el formulario (artículo,
        /// cantidad, lotes y líneas de subproductos) mientras todavía no se grabó el UDO, y
        /// guarda en el contexto la entrada/salida a generar al agregarse. Devuelve false si no
        /// es válido (el caller corta con BubbleEvent = false y no se graba nada).
        /// </summary>
        public bool PrepararCreacionCompletada(SAPbouiCOM.Form oForm, TransformProductionContext ctx)
        {
            if (!ValidarFormulario(oForm, ctx)) return false;

            var oMatrix = (Matrix)oForm.Items.Item(CONSTANTS.UID.GRID).Specific;
            oMatrix.FlushToDataSource();

            var entradas = ObtenerInfoLineas(oForm);
            if (entradas.Items.Count == 0)
            {
                NotificationService.MostrarError(CONSTANTS.MESSAGES.CREATE_NO_ENTRY_LINES);
                return false;
            }

            ctx.EntradasAlAgregar = entradas;
            ctx.SalidasAlAgregar = ctx.InventoryGenExitsData;
            ctx.CompletarAlAgregar = true;
            return true;
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

            // Se valida contra la base (no el formulario): solo un Pendiente sin Entrada/Salida.
            int.TryParse(ObtenerDocEntry(oForm), out int docEntry);
            if (!PuedeConfirmarse(LeerCabeceraUdoPersistida(docEntry), out string motivo))
            {
                NotificationService.MostrarAlerta(motivo);
                BubbleEvent = false;
                return;
            }

            // En Pendiente la grilla y la cantidad son editables: se revalida el detalle
            // (lotes, cantidades, almacén, precio, moneda, fechas y tolerancia ±5%).
            if (!ValidarLineasDetalle(oForm)) { BubbleEvent = false; return; }

            // Tipo de cambio de hoy para cada moneda de las líneas (y la de sistema), salvo la local.
            if (!ValidarTipoCambioLineas(oForm)) { BubbleEvent = false; return; }

            if (!ValidarFormulario(oForm, ctx)) { BubbleEvent = false; return; }

            // Se pregunta recién con todo validado, antes de mover stock.
            int respuesta = ConnectionSDK.UIAPI.MessageBox(CONSTANTS.MESSAGES.CONFIRM_QUESTION, 2, "Confirmar", "Cancelar");
            if (respuesta != 1) { BubbleEvent = false; return; } // 2 = "Cancelar"

            var oMatrix = (Matrix)oForm.Items.Item(CONSTANTS.UID.GRID).Specific;
            oMatrix.FlushToDataSource();
            ctx.InventoryGenEntriesData = ObtenerInfoLineas(oForm); // entrada desde líneas UDO

            // Entrada/Salida + update a Completado en una sola transacción (TP-04).
            if (!CrearProduccion(ctx, docEntry, out int entryDocEntry, out int exitDocEntry))
            { BubbleEvent = false; return; }

            ctx.InventoryGenEntriesDocEntry = entryDocEntry;
            ctx.InventoryGenExitsDocEntry = exitDocEntry;

            // Recargar el registro (en vez de escribir el combo) deja el formulario en modo OK con
            // el estado y los DocEntry persistidos: si quedara en modo Actualizar con el combo
            // cambiado, grabarlo desde la UI pisaría los DocEntry guardados por DI API (TP-10).
            RecargarRegistro(oForm, docEntry);
            AplicarHabilitacionPorEstado(oForm, ctx.PrincipalStatus);

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

            // La salida a revertir se reconstruye siempre desde el Goods Issue original (OIGE):
            // lotes, cantidades y costo con el que salió, para reingresarla a ese mismo valor.
            if (ctx.InventoryGenExitsDocEntry > 0)
                ctx.InventoryGenExitsData = ObtenerSalidaParaReversion(ctx.InventoryGenExitsDocEntry);

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
                    // Se graba ya referenciando la salida original (TP-14).
                    entryRevDocEntry = CrearEntradaMercancia(ctx.InventoryGenExitsData,
                        ctx.InventoryGenExitsDocEntry, ReferencedObjectTypeEnum.rot_GoodsIssue);
                }

                // Revertir la ENTRADA (IGN de subproductos) ⇒ generar una SALIDA (IGO).
                if (ctx.InventoryGenEntriesDocEntry > 0)
                {
                    var entriesData = ObtenerInfoLineas(oForm); // subproductos de la línea UDO
                    if (entriesData.Items.Count > 0)
                    {
                        // Se graba ya referenciando la entrada original (TP-14).
                        exitRevDocEntry = CrearSalidaMercancia(entriesData,
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