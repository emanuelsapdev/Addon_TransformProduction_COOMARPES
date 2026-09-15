using Addon_TransformProduction;
using Addon_TransformProduction.Common;
using Addon_TransformProduction.Models;
using SAPbouiCOM;
using System;

namespace Addon_TransformProduction.Forms.WindowBatches
{
    public partial class WindowBatchesFrm
    {
        /// <summary>
        /// Al abrir el formulario (et_FORM_LOAD) se guarda la referencia al formulario en
        /// el contexto de la producción (identificado por el TypeCount embebido en el FormUID)
        /// para poder cerrarlo desde TransformProductionFrm.
        /// </summary>
        public void ManejarCargaFormularioLotes(string formUid)
        {
            var oForm = ConnectionSDK.UIAPI.Forms.Item(formUid);
            string typeCount = CONSTANTS.ObtenerTypeCount(formUid) ?? oForm.TypeCount.ToString();
            var ctx = ContextManager.ObtenerOCrear(typeCount);
            ctx.FormBatches = oForm;
        }

        /// <summary>
        /// Al presionar "Aceptar" sincroniza la matriz con el DataSource y delega en
        /// Service.ManejarSeleccionLotes. Devuelve false si la selección no es válida
        /// (para que el evento no haga bubble y cierre el formulario).
        /// </summary>
        public bool ManejarAceptarLotesSeleccionados(string formUid)
        {
            var oForm = ConnectionSDK.UIAPI.Forms.Item(formUid);

            string typeCount = CONSTANTS.ObtenerTypeCount(formUid);
            if (string.IsNullOrEmpty(typeCount)) typeCount = oForm.TypeCount.ToString();

            var ctx = ContextManager.Obtener(typeCount);
            if (ctx == null) return true;

            var oDataTable = oForm.DataSources.DataTables.Item(CONSTANTS.DATATABLE.UID);
            SAPbouiCOM.Matrix mtx = oForm.Items.Item(CONSTANTS.UID.MATRIX).Specific;
            mtx.FlushToDataSource();

            return ManejarSeleccionLotes(oForm, oDataTable, ctx);
        }
    }
}