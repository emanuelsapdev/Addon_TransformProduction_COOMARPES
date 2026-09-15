using Addon_TransformProduction.Forms.TransformProduction;
using Addon_TransformProduction.Forms.WindowBatches;
using Addon_TransformProduction.Services;
using SAPbouiCOM;
using System;
using System.Collections.Generic;

namespace Addon_TransformProduction.Common
{
    /// <summary>
    /// Registra las suscripciones de la UI API y delega cada evento a los handlers
    /// registrados por FormTypeEx (ver EventRouter.Handlers.cs para la delegación).
    /// </summary>
    public partial class EventRouter
    {
        // Registro de manejadores: clave = FormTypeEx, valor = handler singleton
        private readonly Dictionary<string, IFormEventHandler> _handlers = new Dictionary<string, IFormEventHandler>();

        public EventRouter()
        {
            // Registrar todos los formularios del addon
            _handlers.Add(TransformProductionFrm.FormType, new TransformProductionFrm());
            _handlers.Add(WindowBatchesFrm.FormType, new WindowBatchesFrm());

            SuscribirEventos();
        }

        /// <summary>Suscripción a los cuatro eventos de la UI API que maneja el addon.</summary>
        private void SuscribirEventos()
        {
            try
            {
                ConnectionSDK.UIAPI.AppEvent += ManejarEventoApp;
                ConnectionSDK.UIAPI.MenuEvent += ManejarEventoMenu;
                ConnectionSDK.UIAPI.ItemEvent += ManejarEventoItem;
                ConnectionSDK.UIAPI.FormDataEvent += ManejarEventoFormData;
            }
            catch (Exception ex)
            {
                NotificationService.MostrarError(CONSTANTS_GLOBALS.Messages.EventRouterInitErrorPrefix + ex.Message);
            }
        }

        /// <summary>Handler registrado para el FormTypeEx indicado, o null si ninguno lo maneja.</summary>
        public IFormEventHandler ObtenerHandler(string formTypeEx)
        {
            _handlers.TryGetValue(formTypeEx, out var handler);
            return handler;
        }
    }
}