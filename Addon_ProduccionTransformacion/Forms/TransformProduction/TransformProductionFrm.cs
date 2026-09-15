using Addon_TransformProduction.Common;
using Addon_TransformProduction.Services;
using Addon_TransformProduction.Tools;
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

                if(pVal.BeforeAction && pVal.EventType == BoEventTypes.et_ITEM_PRESSED
                    && pVal.ItemUID == CONSTANTS.UID.BUTTONS.CREATE && pVal.FormMode == (int)BoFormMode.fm_ADD_MODE)
                {
                    SAPbouiCOM.Form oForm = null;
                    try
                    {
                        oForm = ConnectionSDK.UIAPI.Forms.Item(pVal.FormUID);
                        int respuesta = ConnectionSDK.UIAPI.MessageBox("¿Confirma la creación y continuación con las transacciones correspondientes? De lo contrario, quedará pendiente para su posterior gestión.", 1, "Pendiente", "Crear", "Cancelar");

                        if (respuesta == 2) // Crear
                        {
                            ManejarCreacionProduccion(oForm, out BubbleEvent);
                        }
                        else if (respuesta == 1) // Pendiente
                        {
                            ManejarCreacionProduccionPendiente(oForm, out BubbleEvent);
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
        }
    }
}