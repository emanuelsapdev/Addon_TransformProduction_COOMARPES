using Addon_TransformProduction.Common;
using SAPbouiCOM;
using System;

namespace Addon_TransformProduction.Services
{
    /// <summary>
    /// Notificaciones al usuario sobre la UI API de SAP B1 (barra de estado y MessageBox).
    /// </summary>
    public class NotificationService
    {
        /// <summary>Mensaje de error en la barra de estado.</summary>
        public static void MostrarError(string mensaje) => ConnectionSDK.UIAPI.StatusBar.SetText(mensaje, BoMessageTime.bmt_Short, BoStatusBarMessageType.smt_Error);

        /// <summary>Mensaje de advertencia en la barra de estado.</summary>
        public static void MostrarAdvertencia(string mensaje) => ConnectionSDK.UIAPI.StatusBar.SetText(mensaje, BoMessageTime.bmt_Short, BoStatusBarMessageType.smt_Warning);

        /// <summary>Mensaje de éxito en la barra de estado.</summary>
        public static void MostrarExito(string mensaje) => ConnectionSDK.UIAPI.StatusBar.SetText(mensaje, BoMessageTime.bmt_Short, BoStatusBarMessageType.smt_Success);

        /// <summary>
        /// Mensaje modal bloqueante (MessageBox de la UI API). Usar para impedir que el usuario
        /// continúe mientras haya errores pendientes (ej: filas inválidas en la grilla).
        /// </summary>
        public static void MostrarAlerta(string mensaje) => ConnectionSDK.UIAPI.MessageBox(mensaje);
    }
}