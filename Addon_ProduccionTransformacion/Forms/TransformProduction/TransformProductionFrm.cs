using Addon_TransformProduction.Common;
using Addon_TransformProduction.Services;
using Addon_TransformProduction.Tools;
using SAPbobsCOM;
using SAPbouiCOM;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml;

namespace Addon_TransformProduction.Forms.TransformProduction
{
    public partial class TransformProductionFrm : IFormEventHandler
    {
        public const string FormType = CONSTANTS.FORM_TYPE;
        public const string FormUniqueID = CONSTANTS.FORM_TYPE;

        public static string FatherItemCode = string.Empty;

        /// <summary>
        /// Shell de eventos: determina qué item/evento/columna dispara cada evento y
        /// delega a un único método por evento (vive en Service/Handlers/UIBuilder).
        /// </summary>
        public void OnMenuEvent(ref MenuEvent pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;

            // Buscar / Nuevo: re-sincronizar el campo espejo de "Producto" con el registro vacío.
            if (!pVal.BeforeAction && (pVal.MenuUID == CONSTANTS.SAP_MENUS.FIND || pVal.MenuUID == CONSTANTS.SAP_MENUS.ADD || pVal.MenuUID == CONSTANTS.MENU_UID))
            {
                try
                {
                    var oActive = ConnectionSDK.UIAPI.Forms.ActiveForm;
                    if (oActive != null && oActive.TypeEx == FormType)
                    {
                        ManejarSincronizacionProducto(oActive.UniqueID);

                        if (pVal.MenuUID == CONSTANTS.SAP_MENUS.ADD || pVal.MenuUID == CONSTANTS.MENU_UID)
                            ManejarModoAgregar(oActive.UniqueID);


                    }
                }
                catch (Exception ex)
                {
                    NotificationService.MostrarError($"(OnMenuEvent) {pVal.MenuUID}: {ex.Message}");
                }
            }

        }

        public void OnItemEvent(string FormUID, ref ItemEvent pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;

            
            #region Al abrir el formulario se crea un contexto para almacenar la información de la producción.
            if (!pVal.BeforeAction && pVal.EventType == BoEventTypes.et_FORM_LOAD)
            {
                ManejarCargaFormulario(pVal.FormUID);
                return;
            }
            #endregion

            #region Al cerrar el formulario se elimina el contexto creado para la producción.
            if (!pVal.BeforeAction && pVal.EventType == BoEventTypes.et_FORM_CLOSE)
            {
                ManejarCierreFormulario(pVal.FormUID);
                return;
            }
            #endregion

            // Al activar el formulario se asegura el campo Producto con CFL filtrado por lista de materiales.
            if (!pVal.BeforeAction && pVal.EventType == BoEventTypes.et_FORM_ACTIVATE)
            {
                try
                {
                    ManejarActivacionCampoProducto(pVal.FormUID);
                }
                catch (Exception ex)
                {
                    NotificationService.MostrarError($"(OnItemEvent) {pVal.EventType}: {ex.Message}");
                }
            }

            // Al activar el formulario se sincronizan artículo y cantidad en el contexto (los usa el
            // formulario de lotes). El estado no se toca: lo fijan la carga del registro y los flujos
            // de Crear/Confirmar/Revertir.
            if (!pVal.BeforeAction && pVal.EventType == BoEventTypes.et_FORM_ACTIVATE)
            {
                try
                {
                    ManejarActivacionContexto(pVal.FormUID);
                }
                catch (Exception ex)
                {
                    NotificationService.MostrarError($"(OnItemEvent) {pVal.EventType}: {ex.Message}");
                }
            }

            // Abrir formulario de selección de lotes al clickear el botón de cabecera.
            if (pVal.BeforeAction && pVal.EventType == BoEventTypes.et_ITEM_PRESSED
                && pVal.ItemUID == CONSTANTS.UID.BUTTONS.LOTE_SELECT)
            {
                SAPbouiCOM.Form oForm = null;
                try
                {
                    oForm = ConnectionSDK.UIAPI.Forms.Item(pVal.FormUID);

                    AbrirFormularioLotes(oForm);
                }
                finally
                {
                    if (oForm != null)
                    {
                        MarshalGC.LiberarComObject(oForm);
                        oForm = null;
                    }
                }
                return;
            }

            // Artículo elegido en el CFL filtrado (solo con lista de materiales) del campo Producto.
            if (pVal.ActionSuccess && pVal.EventType == BoEventTypes.et_CHOOSE_FROM_LIST
                && pVal.ItemUID == CONSTANTS.UID.CHOOSE_FROM_LIST.ITEM_CODE_MIRROR)
            {
                SAPbouiCOM.Form oForm = null;
                try
                {
                    oForm = ConnectionSDK.UIAPI.Forms.Item(FormUID);
                    ManejarSeleccionProductoCfl(oForm, ((IChooseFromListEvent)pVal).SelectedObjects);
                }
                finally
                {
                    if (oForm != null)
                    {
                        MarshalGC.LiberarComObject(oForm);
                        oForm = null;
                    }
                }
                return;
            }

            // Al perder el foco el campo Producto se copia al UDF, se consulta el BOM y se pinta la grilla.
            if (pVal.ActionSuccess && pVal.EventType == BoEventTypes.et_LOST_FOCUS
                && pVal.ItemUID == CONSTANTS.UID.CHOOSE_FROM_LIST.ITEM_CODE_MIRROR)
            {
                SAPbouiCOM.Form oForm = null;
                try
                {
                    oForm = ConnectionSDK.UIAPI.Forms.Item(FormUID);

                    ManejarProductoPerdidaFoco(oForm);
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

            // Al perder el foco la cantidad consumida se valida y se actualiza el contexto.
            if (pVal.ActionSuccess && pVal.EventType == BoEventTypes.et_LOST_FOCUS
                && pVal.ItemUID == CONSTANTS.UID.HEADER.QUANTITY)
            {
                SAPbouiCOM.Form oForm = null;
                try
                {
                    oForm = ConnectionSDK.UIAPI.Forms.Item(FormUID);
                    ManejarCantidadConsumidaPerdidaFoco(oForm);
                    HabilitarBotonSeleccionLotes(oForm);
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

            // CONFIRM_PROD (Item_2): confirma la producción de un documento Pendiente.
            if (pVal.BeforeAction && pVal.EventType == BoEventTypes.et_ITEM_PRESSED
                && pVal.ItemUID == CONSTANTS.UID.BUTTONS.CONFIRM_PROD)
            {
                SAPbouiCOM.Form oForm = null;
                try
                {
                    oForm = ConnectionSDK.UIAPI.Forms.Item(pVal.FormUID);

                    Recordset oRec = ConnectionSDK.DIAPI.GetBusinessObject(BoObjectTypes.BoRecordset);
                    string q = $@"SELECT ""Rate"" FROM ORTT WHERE ""Currency"" = 'USD' AND ""RateDate"" = CURRENT_DATE";
                    oRec.DoQuery(q);

                    // SI ES 0 ABRIR EL FORMULARIO DE TIPO DE CAMBIO PARA QUE EL USUARIO LO CARGUE
                    if (oRec.Fields.Item(0).Value == 0)
                    {
                        ConnectionSDK.UIAPI.ActivateMenuItem("3333");
                        NotificationService.MostrarAlerta("Debes indicar el tipo de cambio de hoy");
                        // EL BUUBLE EVENT SE SETEA EN FALSE PARA QUE NO SE GRABE EL DOCUMENTO HASTA QUE EL USUARIO CARGUE EL TIPO DE CAMBIO
                        BubbleEvent = false;
                        return;
                    }

                    // Si la confirmación falla o se cancela, se corta acá: no se cierra el contexto
                    // ni se reabre el documento (TP-09).
                    ManejarConfirmarProduccion(oForm, out BubbleEvent);
                    if (BubbleEvent == false) return;

                    string docEntry = ObtenerDocEntry(oForm);

                    // Esta ventana sigue abierta mostrando el registro confirmado: se reconstruye su
                    // contexto en vez de borrarlo (TP-12).
                    ManejarProduccionConfirmada(pVal.FormUID);
                    AbrirRegistro(docEntry);
                    
                }
                finally
                {
                    if (oForm != null)
                    {
                        MarshalGC.LiberarComObject(oForm);
                        oForm = null;
                    }
                }
                return;
            }

            // SHOW_DOCUMENTS (Item_5): abre los documentos de mercancía de la producción.
            if (!pVal.BeforeAction && pVal.EventType == BoEventTypes.et_CLICK
                && pVal.ItemUID == CONSTANTS.UID.BUTTONS.SHOW_DOCUMENTS)
            {
                ManejarVerDocumentos(FormUID);
                return;
            }

            // REVERT (Item_3): revierte la producción generando los documentos de reversión.
            if (pVal.BeforeAction && pVal.EventType == BoEventTypes.et_ITEM_PRESSED
                && pVal.ItemUID == CONSTANTS.UID.BUTTONS.REVERT)
            {
                SAPbouiCOM.Form oForm = null;
                try
                {
                    oForm = ConnectionSDK.UIAPI.Forms.Item(pVal.FormUID);
                    ManejarReversionTransformacion(oForm, out BubbleEvent);
                }
                finally
                {
                    if (oForm != null)
                    {
                        MarshalGC.LiberarComObject(oForm);
                        oForm = null;
                    }
                }
                return;
            }

            if (pVal.BeforeAction && pVal.EventType == BoEventTypes.et_ITEM_PRESSED
                && pVal.ItemUID == CONSTANTS.UID.BUTTONS.CREATE && pVal.FormMode == (int)BoFormMode.fm_ADD_MODE)
            {
                SAPbouiCOM.Form oForm = null;
                try
                {
                    oForm = ConnectionSDK.UIAPI.Forms.Item(pVal.FormUID);

                    // Líneas de detalle (vale para Pendiente y Crear): si no hay líneas o alguna
                    // no tiene lote, no se graba el UDO.
                    if (!ValidarLineasDetalle(oForm))
                    {
                        BubbleEvent = false;
                        return;
                    }

                    // VALIDAR QUE HAYA CARGADO EL TIPO DE CAMBIO DEL DIA -------------
                    // TRAER TIPO DE CAMBIO DEL DIA Y VALIDAR QUE NO SEA 0
                    Recordset oRec = ConnectionSDK.DIAPI.GetBusinessObject(BoObjectTypes.BoRecordset);
                    string q = $@"SELECT ""Rate"" FROM ORTT WHERE ""Currency"" = 'USD' AND ""RateDate"" = CURRENT_DATE";
                    oRec.DoQuery(q);

                    // SI ES 0 ABRIR EL FORMULARIO DE TIPO DE CAMBIO PARA QUE EL USUARIO LO CARGUE
                    if (oRec.RecordCount == 0) {
                        ConnectionSDK.UIAPI.ActivateMenuItem("3333");
                        NotificationService.MostrarAlerta("Debes indicar el tipo de cambio de hoy");
                        // EL BUUBLE EVENT SE SETEA EN FALSE PARA QUE NO SE GRABE EL DOCUMENTO HASTA QUE EL USUARIO CARGUE EL TIPO DE CAMBIO
                        BubbleEvent = false;
                        return;
                    }


                    int respuesta = ConnectionSDK.UIAPI.MessageBox("¿Confirma la creación y continuación con las transacciones correspondientes? De lo contrario, quedará pendiente para su posterior gestión.", 1, "Pendiente", "Crear", "Cancelar");
                    var ctx = ContextManager.ObtenerOCrear(oForm.TypeCount.ToString());

                    ctx.CompletarAlAgregar = false;

                    if (respuesta == 2) // Crear - Estado Completado (transaccionar Entrada y Salida)
                    {
                        // Se valida ANTES de grabar: si falla, BubbleEvent = false y el UDO no se agrega.
                        if (!PrepararCreacionCompletada(oForm, ctx))
                        {
                            BubbleEvent = false;
                            return;
                        }
                    }
                    else if (respuesta != 1) // Cancelar
                    {
                        BubbleEvent = false;
                        return;
                    }

                    // La elección queda en ctx.CompletarAlAgregar (Crear) y el registro se graba
                    // siempre Pendiente, escrito en el combo en este mismo click: pasa a Completado
                    // recién cuando se crean la Entrada/Salida en el after-add
                    // (ManejarProduccionAgregada). Así et_FORM_ACTIVATE no puede cambiar el resultado.
                    ctx.PrincipalStatus = CONSTANTS.STAGING_STATUS.PENDING;
                    EscribirEstadoCabecera(oForm, CONSTANTS.STAGING_STATUS.PENDING);
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

            // Red de seguridad: al pasar de OK_MODE a ADD_MODE (botón "Nuevo" después de grabar),
            // resetear el contexto antes de cargar un nuevo registro. La transición se detecta
            // comparando el modo actual con el último observado (sin depender de FormModeEx).
            //if (!pVal.BeforeAction && pVal.EventType == BoEventTypes.et_ALL_EVENTS)
            //{
            //    SAPbouiCOM.Form oFormTrans = null;
            //    try
            //    {
            //        oFormTrans = ConnectionSDK.UIAPI.Forms.Item(pVal.FormUID);
            //        var ctxTrans = ContextManager.ObtenerOCrear(oFormTrans.TypeCount.ToString());

            //        if (ctxTrans.UltimoModoFormulario == BoFormMode.fm_OK_MODE
            //            && oFormTrans.Mode == BoFormMode.fm_ADD_MODE)
            //        {
            //            ctxTrans.ResetearContexto();
            //        }

            //        ctxTrans.UltimoModoFormulario = oFormTrans.Mode;
            //    }
            //    catch { }
            //    finally
            //    {
            //        if (oFormTrans != null)
            //        {
            //            MarshalGC.LiberarComObject(oFormTrans);
            //            oFormTrans = null;
            //        }
            //    }
            //}

        }

        public void OnFormDataEvent(ref BusinessObjectInfo boi, out bool BubbleEvent)
        {
            BubbleEvent = true;

            // Campo espejo de "Producto": reflejar el registro recién cargado.
            if (boi.FormTypeEx == FormType && boi.EventType == BoEventTypes.et_FORM_DATA_LOAD && boi.ActionSuccess)
            {
                try
                {
                    ManejarSincronizacionProducto(boi.FormUID);
                }
                catch (Exception ex)
                {
                    NotificationService.MostrarError($"(OnFormDataEvent) {boi.EventType}: {ex.Message}");
                }
            }

            // Campo espejo de "Producto": después de agregar el formulario queda en un documento nuevo.
            if (boi.FormTypeEx == FormType && boi.EventType == BoEventTypes.et_FORM_DATA_ADD && boi.ActionSuccess)
            {
                try
                {
                    ManejarProductoTrasAgregar(boi.FormUID);
                }
                catch (Exception ex)
                {
                    NotificationService.MostrarError($"(OnFormDataEvent) {boi.EventType}: {ex.Message}");
                }
            }

            if (boi.FormTypeEx == FormType && boi.EventType == BoEventTypes.et_FORM_DATA_ADD && boi.ActionSuccess)
            {
                SAPbouiCOM.Form oForm = null;

                try
                {
                    oForm = ConnectionSDK.UIAPI.Forms.Item(boi.FormUID);

                    // Tras agregar, el formulario queda en un documento nuevo: el DocEntry se
                    // toma del ObjectKey del evento (no del formulario).
                    int docEntry = ObtenerDocEntryAgregado(boi.ObjectKey);

                    var ctx = ContextManager.ObtenerOCrear(oForm.TypeCount.ToString());
                    ManejarProduccionAgregada(docEntry, ctx);

                    // Esta ventana sigue abierta (en un documento nuevo): se reinicia su contexto en
                    // vez de borrarlo (TP-12).
                    ReiniciarContextoFormulario(boi.FormUID);

                    if (docEntry > 0)
                        AbrirRegistro(docEntry.ToString());
                }
                catch (Exception ex)
                {
                    NotificationService.MostrarError($"(OnFormDataEvent) {boi.EventType}: {ex.Message}");
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



            if (boi.FormTypeEx == FormType && boi.EventType == BoEventTypes.et_FORM_DATA_UPDATE && !boi.BeforeAction)
            {
                SAPbouiCOM.Form oForm = null;
                try
                {
                    oForm = ConnectionSDK.UIAPI.Forms.Item(boi.FormUID);

                    var ctxUpdate = ContextManager.ObtenerOCrear(oForm.TypeCount.ToString());

                    // Al guardar (actualizar) el documento se reinicia el contexto: la selección
                    // de lotes en memoria se descarta y la cabecera se re-sincroniza desde el
                    // registro para que el usuario re-elija la salida (estado Pendiente).
                    ctxUpdate.ResetearContexto();
                    ReconstruirContextoDesdeForm(oForm, ctxUpdate);
                    LimpiarEtiquetaLotes(oForm);
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

            // Registro cargado (navegar/buscar): recién con ActionSuccess el DBDataSource tiene los
            // datos del registro nuevo; en el BeforeAction todavía tiene los del anterior.
            if (boi.FormTypeEx == FormType && boi.EventType == BoEventTypes.et_FORM_DATA_LOAD && boi.ActionSuccess)
            {
                try
                {
                    ManejarRegistroCargado(boi.FormUID);
                }
                catch (Exception ex)
                {
                    NotificationService.MostrarError($"(OnFormDataEvent) {boi.EventType}: {ex.Message}");
                }
            }
        }
    }
}