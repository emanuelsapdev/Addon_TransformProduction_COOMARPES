using Addon_TransformProduction.Services;
using SAPbouiCOM;
using System;

namespace Addon_TransformProduction.Common
{
    public partial class EventRouter
    {
        /// <summary>
        /// Delegación de ItemEvents al handler del FormTypeEx correspondiente.
        /// Siempre deja BubbleEvent = true para no romper la cadena de eventos de SAP.
        /// </summary>
        private void ManejarEventoItem(string FormUID, ref ItemEvent pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;
            try
            {
                var handler = ObtenerHandler(pVal.FormTypeEx, FormUID);
                if (handler != null)
                    handler.OnItemEvent(FormUID, ref pVal, out BubbleEvent);
            }
            catch (Exception ex)
            {
                NotificationService.MostrarError(CONSTANTS_GLOBALS.Messages.ItemEventErrorPrefix + ex.Message);
            }
        }

        /// <summary>
        /// Delegación de FormDataEvents al handler del FormTypeEx correspondiente.
        /// </summary>
        private void ManejarEventoFormData(ref BusinessObjectInfo boi, out bool BubbleEvent)
        {
            BubbleEvent = true;
            try
            {
                var handler = ObtenerHandler(boi.FormTypeEx, boi.FormUID);
                if (handler != null)
                    handler.OnFormDataEvent(ref boi, out BubbleEvent);
            }
            catch (Exception ex)
            {
                NotificationService.MostrarError(CONSTANTS_GLOBALS.Messages.FormDataEventErrorPrefix + ex.Message);
            }
        }

        /// <summary>
        /// Los eventos de menú se propagan a todos los handlers registrados.
        /// </summary>
        private void ManejarEventoMenu(ref MenuEvent pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;
            try
            {
                foreach (var handler in _handlers.Values)
                    handler.OnMenuEvent(ref pVal, out BubbleEvent);
            }
            catch (Exception ex)
            {
                NotificationService.MostrarError(CONSTANTS_GLOBALS.Messages.MenuEventErrorPrefix + ex.Message);
            }
        }

        /// <summary>
        /// Eventos a nivel aplicación: shutdown/company change cierran el addon;
        /// cambio de idioma/fuente lo relanzan vía Application.Restart().
        /// </summary>
        private void ManejarEventoApp(BoAppEventTypes EventType)
        {
            try
            {
                switch (EventType)
                {
                    // Cierre de la aplicación SAP
                    case BoAppEventTypes.aet_ShutDown:
                    case BoAppEventTypes.aet_ServerTerminition:
                    case BoAppEventTypes.aet_CompanyChanged:
                        ConnectionSDK.UIAPI?.StatusBar.SetText(CONSTANTS_GLOBALS.Messages.FinalizingAddon);

                        System.Windows.Forms.Application.Exit();
                        break;

                    // Cambio de fuente o idioma: reiniciar addon
                    case BoAppEventTypes.aet_FontChanged:
                    case BoAppEventTypes.aet_LanguageChanged:
                        ConnectionSDK.UIAPI?.StatusBar.SetText(CONSTANTS_GLOBALS.Messages.RestartingAddon);

                        System.Windows.Forms.Application.Restart();
                        break;
                }
            }
            catch (Exception ex)
            {
                NotificationService.MostrarError(CONSTANTS_GLOBALS.Messages.AppEventErrorPrefix + ex.Message);
            }
        }
    }
}