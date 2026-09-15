using Addon_TransformProduction.Common;
using SAPbouiCOM;
using System;
using System.IO;

namespace Addon_TransformProduction.Forms.WindowBatches
{
    public partial class WindowBatchesFrm
    {
        private static readonly string RutaXmlFormulario = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Forms", "WindowBatches", "B1 Studio", "WindowBatches.xml");

        /// <summary>
        /// Construye el formulario de lotes cargando el diseño exportado desde B1 Studio (.xml)
        /// vía LoadBatchActions y lo muestra. Devuelve la instancia recién creada.
        /// Como cada formulario de producción (TransformProduction) debe tener su propio
        /// formulario de lotes, se modifica el XML cargado: el UID del form se hace único por
        /// TypeCount del formulario de producción (p.ej. WINDOW_BATCHES_3) y se fija el
        /// FormType para que todos compartan el mismo tipo y el EventRouter lo siga repartiendo
        /// al mismo handler.
        /// </summary>
        private SAPbouiCOM.Form ConstruirFormulario(string typeCount)
        {
            string xml = File.ReadAllText(RutaXmlFormulario);

            ConnectionSDK.UIAPI.LoadBatchActions(ref xml);

            var oForm = ConnectionSDK.UIAPI.Forms.Item(CONSTANTS.FORM_UNIQUE_ID);
            oForm.Visible = true;

            return oForm;
        }
    }
}