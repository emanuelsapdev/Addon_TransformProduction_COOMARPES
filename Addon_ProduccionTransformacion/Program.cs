using Addon_TransformProduction.Common;
using Addon_TransformProduction.Services;
using System;

namespace Addon_TransformProduction
{
    internal class Program
    {
        // Requerido por WinForms: System.Windows.Forms.Application.Run() (más abajo) y cualquier
        // control/diálogo de WinForms que uses en este thread (OpenFileDialog, MessageBox,
        // System.Windows.Forms.Timer en ActualizacionCostosFrm.cs → AbrirDialogoExcelDiferido, etc.)
        // requieren que el thread que los usa esté en apartamento STA. Sin este atributo, el estado
        // de apartamento del thread principal queda sin definir explícitamente, lo cual es
        // incorrecto para WinForms aunque en la práctica no siempre falle de entrada.
        [STAThread]
        private static void Main(string[] args)
        {
            try
            {
                // 1. Conectar a SAP Business One (UI API + DI API)
                ConnectionSDK.InicializarConexion();

                if (!ConnectionSDK.Connected)
                    return;

                NotificationService.MostrarExito(CONSTANTS_GLOBALS.Messages.ConnectedOk);

                // 2. Ejecutar inicializaciones (infraestructura + carga de configuraciones)
                var executions = new ExecutionsApp();

                // 3. Registrar enrutador de eventos de UI API
                var eventRouter = new EventRouter();

                // 4. Mantener referencias vivas para evitar recolección por GC
                GC.KeepAlive(eventRouter);
                GC.KeepAlive(executions);

                // 5. Iniciar bucle de mensajes (mantiene el addon activo)
                System.Windows.Forms.Application.Run();
            }
            catch (Exception ex)
            {
                ConnectionSDK.UIAPI?.MessageBox(CONSTANTS_GLOBALS.Messages.FatalErrorPrefix + ex.Message);
            }
        }
    }
}
