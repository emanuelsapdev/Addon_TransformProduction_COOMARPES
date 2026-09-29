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

            //try
            //{
            //    if (!pVal.BeforeAction && pVal.MenuUID == CONSTANTS.MENU_UID)
            //        ManejarMenuRegistrado();
            //}
            //catch (Exception ex)
            //{
            //    NotificationService.MostrarError($"(OnMenuEvent) {pVal.MenuUID}: {ex.Message}");
            //}
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
                var oForm = ConnectionSDK.UIAPI.Forms.Item(pVal.FormUID);
                var ctx = ContextManager.ObtenerOCrear(oForm.TypeCount.ToString());
                try
                {
                    if (ctx.FormBatches != null) ctx.FormBatches.Close();
                }
                catch { }

                ContextManager.Eliminar(oForm.TypeCount.ToString());
                return;
            }
            #endregion

            #region Al activar el formulario setear el contexto
            if (!pVal.BeforeAction && pVal.EventType == BoEventTypes.et_FORM_ACTIVATE)
            {
                SAPbouiCOM.Form oForm = null;
                try
                {
                    oForm = ConnectionSDK.UIAPI.Forms.Item(pVal.FormUID);
                    var ctx = ContextManager.ObtenerOCrear(oForm.TypeCount.ToString());
                    ctx.PrincipalItemCode = LeerCodigoArticulo(oForm);
                    ctx.PrincipalQuantityConsumed = EsCantidadConsumidaValida(LeerCantidadConsumida(oForm), out double qty) ? qty : 0;
                    ctx.PrincipalStatus = LeereEstadoCabecera(oForm);
                    ctx.InventoryGenEntriesData = ObtenerInfoLineas(oForm);

                    //string ctxJson = JsonSerializer.Serialize(ctx, new JsonSerializerOptions { WriteIndented = true });
                    //NotificationService.MostrarAlerta(ctxJson);

                }
                catch { }
                finally
                {
                    if (oForm != null)
                    {
                        MarshalGC.LiberarComObject(oForm);
                        oForm = null;
                    }
                }
            } 
            #endregion

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

            // Al perder el foco el código de artículo se consulta el BOM y se pinta la grilla.
            if (pVal.ActionSuccess && pVal.EventType == BoEventTypes.et_LOST_FOCUS
                && pVal.ItemUID == CONSTANTS.UID.HEADER.ITEM_CODE)
            {
                SAPbouiCOM.Form oForm = null;
                try
                {
                    oForm = ConnectionSDK.UIAPI.Forms.Item(FormUID);

                    ManejarCodigoArticuloPerdidaFoco(oForm);
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

                    ManejarConfirmarProduccion(oForm, out BubbleEvent);
                    string docEntry = ObtenerDocEntry(oForm);
                    ManejarCierreFormulario(pVal.FormUID);
                    ConnectionSDK.UIAPI.OpenForm(BoFormObjectEnum.fo_UserDefinedObject, CONSTANTS.UDO.OBJECT_CODE, docEntry);
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
            if (pVal.BeforeAction && pVal.EventType == BoEventTypes.et_ITEM_PRESSED
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

                    if (respuesta == 2) // Crear - Estado Completado (transaccionar Entrada y Salida)
                    {
                        ctx.PrincipalStatus = CONSTANTS.STAGING_STATUS.COMPLETED;
                    }
                    else if (respuesta == 1) // Crear - Estado Pendiente
                    {
                        ctx.PrincipalStatus = CONSTANTS.STAGING_STATUS.PENDING;
                    }
                    else
                    {
                        BubbleEvent = false;
                    }
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

            if (boi.FormTypeEx == FormType && boi.EventType == BoEventTypes.et_FORM_DATA_ADD && boi.ActionSuccess)
            {
                SAPbouiCOM.Form oForm = null;

                try
                {
                    oForm = ConnectionSDK.UIAPI.Forms.Item(boi.FormUID);
                    string docEntry = ObtenerDocEntry(oForm);

                    var ctx = ContextManager.ObtenerOCrear(oForm.TypeCount.ToString());

                    try
                    {

                        switch (ctx.PrincipalStatus)
                        {
                            case CONSTANTS.STAGING_STATUS.COMPLETED:

                                ManejarCreacionProduccion(oForm, out BubbleEvent);   // crea IGN+IGO y setea ctx.DocEntries

                                ActualizarResultadoTransformacion(
                                Convert.ToInt32(docEntry),
                                ctx.PrincipalStatus,
                                ctx.InventoryGenEntriesDocEntry > 0 ? ctx.InventoryGenEntriesDocEntry : (int?)null,
                                ctx.InventoryGenExitsDocEntry > 0 ? ctx.InventoryGenExitsDocEntry : (int?)null);

                                AbrirDocumentosRelacionados(ctx);

                                break;
                            case CONSTANTS.STAGING_STATUS.PENDING:
                                break;

                        }
                    }
                    catch (Exception ex)
                    {
                        NotificationService.MostrarError(ex.Message);
                        BubbleEvent = false;
                        return;
                    }

                    ManejarCierreFormulario(boi.FormUID);

                    LimpiarEtiquetaLotes(oForm);

                    ConnectionSDK.UIAPI.OpenForm(BoFormObjectEnum.fo_UserDefinedObject, CONSTANTS.UDO.OBJECT_CODE, docEntry);
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

            if (boi.FormTypeEx == FormType && boi.EventType == BoEventTypes.et_FORM_DATA_LOAD && boi.BeforeAction)
            {
                SAPbouiCOM.Form oForm = null;
                try
                {
                    oForm = ConnectionSDK.UIAPI.Forms.Item(boi.FormUID);

                    var ctxLoad = ContextManager.ObtenerOCrear(oForm.TypeCount.ToString());

                    ctxLoad.ResetearContexto();
                    ReconstruirContextoDesdeForm(oForm, ctxLoad);
                    LimpiarEtiquetaLotes(oForm);

                    oForm.Freeze(true);
                    FormularioEnCualquierEstado(oForm);

                    switch (ctxLoad.PrincipalStatus)
                    {
                        case CONSTANTS.STAGING_STATUS.COMPLETED:
                                FormularioEnEstadoCompletado(oForm);
                            break;
                        case CONSTANTS.STAGING_STATUS.PENDING:
                                FormularioEnEstadoPendiente(oForm);
                            break;
                        case CONSTANTS.STAGING_STATUS.REVERT:
                                FormularioEnEstadoRevertido(oForm);
                            break;
                        default:
                            break;
                    }
                    oForm.Freeze(false);

                }
                finally
                {
                    oForm.Freeze(false);

                    if (oForm != null)
                    {
                        MarshalGC.LiberarComObject(oForm);
                        oForm = null;
                    }
                }
            }
        }
    }
}