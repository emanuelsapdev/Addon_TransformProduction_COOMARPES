using Addon_TransformProduction.Common;
using Addon_TransformProduction.Services;
using SAPbouiCOM;
using System;

namespace Addon_TransformProduction.Forms.WindowBatches
{
    public partial class WindowBatchesFrm : IFormEventHandler
    {
        public const string FormType = CONSTANTS.FORM_TYPE;
        public const string FormUniqueID = CONSTANTS.FORM_UNIQUE_ID;

        /// <summary>
        /// Shell de eventos: determina qué item/evento dispara cada evento y delega a
        /// un único método por evento (vive en Handlers/Service).
        /// </summary>
        public void OnMenuEvent(ref MenuEvent pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;
        }

        public void OnItemEvent(string FormUID, ref ItemEvent pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;

            try
            {
                // Al abrir el formulario se crea un contexto para almacenar la información de los lotes.
                if (!pVal.BeforeAction && pVal.EventType == BoEventTypes.et_FORM_LOAD)
                {
                    ManejarCargaFormularioLotes(pVal.FormUID);
                    return;
                }

                // Al presionar "Aceptar" se validan y guardan los lotes seleccionados.
                if (pVal.BeforeAction && pVal.EventType == BoEventTypes.et_ITEM_PRESSED && pVal.ItemUID == CONSTANTS.UID.BUTTONS.OK)
                {
                    BubbleEvent = ManejarAceptarLotesSeleccionados(pVal.FormUID);
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