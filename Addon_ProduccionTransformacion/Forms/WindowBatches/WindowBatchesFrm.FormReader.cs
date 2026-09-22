using Addon_TransformProduction.Forms.TransformProduction;
using Addon_TransformProduction.Models;
using SAPbouiCOM;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Addon_TransformProduction.Forms.WindowBatches
{
    public partial class WindowBatchesFrm
    {
        /// <summary>
        /// Puebla la grilla de lotes a partir de los lotes del artículo ya mapeados
        /// (ver Service.CargarGrillaLotes). Escribe vía DataTable + LoadFromDataSource;
        /// nada de queries SQL acá.
        /// </summary>
        private void PoblarGrillaLotes(SAPbouiCOM.Form oForm, List<BatchModel> data)
        {
            if (oForm == null) throw new ArgumentNullException(nameof(oForm));
            if (data == null) throw new ArgumentNullException(nameof(data));

            oForm.Freeze(true);

            var oMatrix = (Matrix)oForm.Items.Item(CONSTANTS.UID.MATRIX).Specific;
            var oDataTable = oForm.DataSources.DataTables.Item(CONSTANTS.DATATABLE.UID);

            oDataTable.Rows.Clear();
            oDataTable.Rows.Add(data.Count);

            for (int i = 0; i < data.Count; i++)
            {
                var row = data[i];

                oDataTable.SetValue(CONSTANTS.DATATABLE.COLUMNS.NUM_LINE, i, (i + 1).ToString(CultureInfo.InvariantCulture));
                oDataTable.SetValue(CONSTANTS.DATATABLE.COLUMNS.CHECK, i, "N");
                oDataTable.SetValue(CONSTANTS.DATATABLE.COLUMNS.BATCH, i, row.BatchNumber ?? string.Empty);
                oDataTable.SetValue(CONSTANTS.DATATABLE.COLUMNS.DUE_DATE, i, row.ExpDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
                oDataTable.SetValue(CONSTANTS.DATATABLE.COLUMNS.WAREHOUSE, i, row.Warehouse ?? string.Empty);
                oDataTable.SetValue(CONSTANTS.DATATABLE.COLUMNS.QTY, i, row.QuantityAvailable.ToString(CultureInfo.InvariantCulture));
                oDataTable.SetValue(CONSTANTS.DATATABLE.COLUMNS.QTY_ASSIGNED, i, row.QuantityAssigned.ToString(CultureInfo.InvariantCulture));
            }

            oMatrix.LoadFromDataSource();
            oMatrix.AutoResizeColumns();
            oForm.Freeze(false);
        }

        /// <summary>
        /// Restaura la grilla de lotes a partir del XML serializado que quedó guardado en el
        /// contexto (BatchHeadXml), para que el usuario conserve su selección al reabrir el
        /// formulario dentro de la misma producción.
        /// </summary>
        private void RestaurarGrillaDesdeXml(SAPbouiCOM.Form oForm, string xml)
        {
            if (oForm == null) throw new ArgumentNullException(nameof(oForm));

            oForm.Freeze(true);

            var oMatrix = (Matrix)oForm.Items.Item(CONSTANTS.UID.MATRIX).Specific;
            var oDataTable = oForm.DataSources.DataTables.Item(CONSTANTS.DATATABLE.UID);

            oDataTable.LoadSerializedXML(SAPbouiCOM.BoDataTableXmlSelect.dxs_DataOnly, xml);
            oMatrix.LoadFromDataSource();
            oMatrix.AutoResizeColumns();

            oForm.Freeze(false);
        }

        /// <summary>
        /// Muestra el código del artículo principal en el campo de cabecera del formulario de lotes.
        /// </summary>
        private static void MostrarArticuloPrincipal(SAPbouiCOM.Form oForm, string principalItemCode)
        {
            oForm.Items.Item(CONSTANTS.UID.TEXT_ART).Specific.Value = principalItemCode;
        }

        /// <summary>
        /// Cierra el formulario de lotes (con supresión de excepciones: puede ya estar
        /// invalidado por SAP en el momento del cierre).
        /// </summary>
        private void CerrarFormulario(SAPbouiCOM.Form oForm)
        {
            try
            {
                oForm.Close();
            }
            catch { }
        }

        /// <summary>
        /// Refresca la etiqueta de la cabecera del formulario de producción con los números de
        /// lote seleccionados (separados por coma).
        /// </summary>
        private static void ActualizarEtiquetaLotesSeleccionados(TransformProductionContext ctx)
        {
            if (ctx.FormTransfProd == null) return;

            ((SAPbouiCOM.StaticText)ctx.FormTransfProd.Items.Item(TransformProductionFrm.CONSTANTS.UID.HEADER.LOTE_LABEL).Specific)
                .Caption = string.Join(", ", ctx.InventoryGenExitsData.Items.SelectMany(item => item.Batches.Select(batch => batch.BatchNumber)).ToArray());
        }
    }
}