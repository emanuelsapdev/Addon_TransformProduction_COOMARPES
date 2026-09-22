using Addon_TransformProduction.Common;
using Addon_TransformProduction.Services;
using Addon_TransformProduction.Tools;
using SAPbobsCOM;
using SAPbouiCOM;
using System;

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

            try
            {
                // Al abrir el formulario se crea un contexto para almacenar la información de la producción.
                if (!pVal.BeforeAction && pVal.EventType == BoEventTypes.et_FORM_LOAD)
                {
                    ManejarCargaFormulario(pVal.FormUID);
                    return;
                }

                // Al cerrar el formulario se elimina el contexto creado para la producción.
                if (!pVal.BeforeAction && pVal.EventType == BoEventTypes.et_FORM_CLOSE)
                {
                    ManejarCierreFormulario(pVal.FormUID);
                    return;
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

                if (pVal.BeforeAction && pVal.EventType == BoEventTypes.et_ITEM_PRESSED
                    && pVal.ItemUID == CONSTANTS.UID.BUTTONS.CREATE && pVal.FormMode == (int)BoFormMode.fm_ADD_MODE)
                {
                    SAPbouiCOM.Form oForm = null;
                    try
                    {
                        oForm = ConnectionSDK.UIAPI.Forms.Item(pVal.FormUID);

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
            }
            catch (Exception ex)
            {
                NotificationService.MostrarError($"(OnItemEvent) {pVal.EventType}|{pVal.ItemUID}: {ex.Message}");
            }
        }

        public void OnFormDataEvent(ref BusinessObjectInfo boi, out bool BubbleEvent)
        {
            BubbleEvent = true;

            if (boi.FormTypeEx == FormType && boi.EventType == BoEventTypes.et_FORM_DATA_ADD && !boi.BeforeAction)
            {
                SAPbouiCOM.Form oForm = null;
                try
                {
                    oForm = ConnectionSDK.UIAPI.Forms.Item(boi.FormUID);
                    string docEntry = ObtenerDocEntry(oForm);

                    ConnectionSDK.UIAPI.OpenForm(BoFormObjectEnum.fo_UserDefinedObject, CONSTANTS.UDO.OBJECT_CODE, docEntry);

                    var ctx = ContextManager.ObtenerOCrear(oForm.TypeCount.ToString());

                    switch (ctx.PrincipalStatus)
                    {
                        case CONSTANTS.STAGING_STATUS.COMPLETED:
                            ManejarCreacionProduccion(oForm, out BubbleEvent);
                            break;
                        case CONSTANTS.STAGING_STATUS.PENDING:
                            break;
                    }

                    CambiarTransformProductionStatus(Convert.ToInt32(docEntry), ctx.PrincipalStatus);

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



            if (boi.FormTypeEx == FormType && boi.EventType == BoEventTypes.et_FORM_DATA_LOAD && !boi.BeforeAction)
            {
                SAPbouiCOM.Form oForm = null;
                try
                {
                    oForm = ConnectionSDK.UIAPI.Forms.Item(boi.FormUID);
                    string status = LeereEstadoCabecera(oForm);

                    oForm.Freeze(true);
                    FormularioEnCualquierEstado(oForm);

                    switch (status){
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