using Addon_TransformProduction.Common;
using Addon_TransformProduction.Models;
using Addon_TransformProduction.Services;
using Addon_TransformProduction.Tools;
using SAPbobsCOM;
using System;

namespace Addon_TransformProduction.Forms.WindowBatches
{
    public partial class WindowBatchesFrm
    {
        /// <summary>
        /// Orquesta la aceptación de los lotes seleccionados: valida las cantidades asignadas
        /// (Functionality), persiste la selección en el contexto (XML + lista) y refresca la
        /// etiqueta de la cabecera del formulario de producción (FormReader).
        /// </summary>
        public bool ManejarSeleccionLotes(SAPbouiCOM.Form oForm, SAPbouiCOM.DataTable oDataTable, TransformProductionContext ctx)
        {
            if (!ValidarCantidadesAsignadas(oDataTable, ctx))
                return false;

            PersistirSeleccionLotes(oDataTable, ctx);
            CerrarFormulario(oForm);
            ActualizarEtiquetaLotesSeleccionados(ctx);

            return true;
        }

        /// <summary>
        /// Guarda los lotes seleccionados en el contexto de la producción: el XML crudo del
        /// DataTable (para restaurar la grilla al reabrir) y la lista de BatchModel.
        /// </summary>
        private void PersistirSeleccionLotes(SAPbouiCOM.DataTable oDataTable, TransformProductionContext ctx)
        {
            ctx.BatchHeadXml = oDataTable.SerializeAsXML(SAPbouiCOM.BoDataTableXmlSelect.dxs_DataOnly);
            ctx.InventoryGenExitsData = ObtenerLotesSeleccionados(oDataTable, ctx);
        }

        /// <summary>
        /// Muestra el formulario de lotes del TypeCount de producción indicado, reutilizando la
        /// instancia existente si ya está abierta, o construyéndola desde el .xml de B1 Studio
        /// (única por TypeCount) y cargando los lotes del contexto.
        /// </summary>
        public void MostrarFormulario(string typeCount)
        {
            if (!MostrarFormularioExistente(typeCount))
            {
                ConstruirFormulario(typeCount);
            }

            var oForm = ConnectionSDK.UIAPI.Forms.Item(CONSTANTS.FORM_UNIQUE_ID);
            CargarLotesContextoActual(oForm, typeCount);
        }

        private bool MostrarFormularioExistente(string typeCount)
        {
            try
            {
                var oExisting = ConnectionSDK.UIAPI.Forms.Item(CONSTANTS.ObtenerFormUID(typeCount));
                oExisting.Select();
                oExisting.Visible = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Puebla el formulario de lotes recién abierto con el estado del contexto: el artículo
        /// principal de la cabecera y la grilla de lotes (restaurada del XML o cargada de SAP).
        /// </summary>
        private void CargarLotesContextoActual(SAPbouiCOM.Form oForm, string typeCount)
        {
            var ctx = ContextManager.ObtenerOCrear(typeCount);
            if (ctx == null || string.IsNullOrWhiteSpace(ctx.PrincipalItemCode)) return;

            MostrarArticuloPrincipal(oForm, ctx.PrincipalItemCode);
            CargarGrillaLotes(oForm, ctx);
        }

        private void CargarGrillaLotes(SAPbouiCOM.Form oForm, TransformProductionContext ctx)
        {
            if (!string.IsNullOrEmpty(ctx.BatchHeadXml))
            {
                RestaurarGrillaDesdeXml(oForm, ctx.BatchHeadXml);
                return;
            }

            Recordset oRecordSet = null;
            try
            {
                oRecordSet = (Recordset)ConnectionSDK.DIAPI.GetBusinessObject(BoObjectTypes.BoRecordset);
                oRecordSet = ObtenerLotesPorArticulo(oRecordSet, ctx.PrincipalItemCode);

                var data = MapearLotesPorArticulo(ref oRecordSet);

                if (data != null && data.Count > 0)
                {
                    PoblarGrillaLotes(oForm, data);
                }
                else
                {
                    NotificationService.MostrarAlerta(
                        $"No se encontraron lotes para el artículo: {ctx.PrincipalItemCode}");
                }
            }
            finally
            {
                if (oRecordSet != null)
                {
                    MarshalGC.LiberarComObject(oRecordSet);
                    oRecordSet = null;
                }
            }
        }
    }
}