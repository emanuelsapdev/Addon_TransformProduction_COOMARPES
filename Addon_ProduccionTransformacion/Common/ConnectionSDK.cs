using SAPbobsCOM;
using SAPbouiCOM;
using System;

namespace Addon_TransformProduction.Common
{
    public class ConnectionSDK
    {
        protected static SAPbouiCOM.Application _UIAPI;
        protected static SAPbobsCOM.Company _DIAPI;
        public static SAPbouiCOM.Application UIAPI => _UIAPI ?? throw new Exception(CONSTANTS_GLOBALS.Messages.UiApiNotDefined);
        public static SAPbobsCOM.Company DIAPI => _DIAPI ?? throw new Exception(CONSTANTS_GLOBALS.Messages.DiApiNotDefined);

        public static bool Connected => _UIAPI != null && _DIAPI.Connected;

        /// <summary>
        /// Inicializa la conexión con SAP Business One (UI API + DI API) y deja
        /// preparadas las instancias singleton que usa todo el addon.
        /// </summary>
        public static void InicializarConexion()
        {
            _UIAPI = ObtenerAplicacion();

            if (_UIAPI != null)
            {
                _DIAPI = _UIAPI.Company.GetDICompany();
            }
        }

        /// <summary>
        /// Se conecta a la UI API de SAP B1 usando el string de conexión que SAP pasa como
        /// primer argumento después del ejecutable (Add-On Administration), o el inyectado por
        /// launchSettings.json en debug local.
        /// </summary>
        public static SAPbouiCOM.Application ObtenerAplicacion()
        {
            SboGuiApi api = new SboGuiApi()
            {
                AddonIdentifier = CONSTANTS_GLOBALS.Addon.Identifier
            };

            string[] commands = Environment.GetCommandLineArgs();
            string strConnection = null;

            // SAP B1 passes the connection string as the first argument after the executable.
            // Environment.GetCommandLineArgs(): [0]=exe path, [1]=connection string (when started by SAP).
            if (commands != null && commands.Length > 1)
            {
                strConnection = commands[1];
            }

            if (string.IsNullOrWhiteSpace(strConnection))
            {
                throw new ArgumentException(CONSTANTS_GLOBALS.Messages.MissingSapConnectionString, nameof(strConnection));
            }

            try
            {
                api.Connect(strConnection);
            }
            catch (Exception ex)
            {
                throw new Exception(CONSTANTS_GLOBALS.Messages.UiApiConnectErrorPrefix + ex.Message, ex);
            }

            SAPbouiCOM.Application app;
            try
            {
                app = api.GetApplication();
            }
            catch (Exception ex)
            {
                throw new Exception(CONSTANTS_GLOBALS.Messages.UiApiGetApplicationErrorPrefix + ex.Message, ex);
            }

            if (app == null)
                throw new Exception(CONSTANTS_GLOBALS.Messages.UiApiApplicationNull);

            return app;
        }
    }
}