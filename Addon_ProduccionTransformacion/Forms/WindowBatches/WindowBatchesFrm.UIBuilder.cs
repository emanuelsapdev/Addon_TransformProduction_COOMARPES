using Addon_TransformProduction.Common;
using SAPbouiCOM;
using System;
using System.IO;
using System.Xml;

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
            string formUid = CONSTANTS.ObtenerFormUID(typeCount);
            string xml = PrepararXmlFormulario(File.ReadAllText(RutaXmlFormulario), formUid);

            ConnectionSDK.UIAPI.LoadBatchActions(ref xml);

            var oForm = ConnectionSDK.UIAPI.Forms.Item(formUid);
            oForm.Visible = true;

            return oForm;
        }

        /// <summary>
        /// Reemplaza en el XML de B1 Studio el uid del form (WINDOW_BATCHES) por
        /// <paramref name="formUid"/> y el FormType (-1) por <see cref="CONSTANTS.FORM_TYPE"/>.
        /// </summary>
        private static string PrepararXmlFormulario(string xml, string formUid)
        {
            var oXmlDoc = new XmlDocument();
            oXmlDoc.LoadXml(xml);

            var oFormNode = (XmlElement)oXmlDoc.SelectSingleNode("/Application/forms/action/form");
            if (oFormNode == null)
                throw new InvalidOperationException($"El XML del formulario de lotes no tiene el nodo form: {RutaXmlFormulario}");

            oFormNode.SetAttribute("uid", formUid);
            oFormNode.SetAttribute("FormType", CONSTANTS.FORM_TYPE);

            return oXmlDoc.OuterXml;
        }
    }
}