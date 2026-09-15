using System;
using System.Text;

namespace Addon_TransformProduction.Tools
{
    /// <summary>
    /// Utilidad de debug para inspeccionar columnas y valores de un Matrix de UI API
    /// durante el desarrollo (por ejemplo, para descubrir UIDs de columnas de una grilla).
    /// </summary>
    public class IUDataTest
    {
        /// <summary>Exporta a un archivo .txt el listado de columnas de la matriz (UID + caption).</summary>
        public static void ExportarColumnasMatrizATxt(SAPbouiCOM.Matrix oMtx, string filePath)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Matrix UID: {oMtx.Item.UniqueID}");
            sb.AppendLine($"Total Columns: {oMtx.Columns.Count}");
            sb.AppendLine(new string('-', 50));

            for (int i = 0; i < oMtx.Columns.Count; i++)
            {
                var column = oMtx.Columns.Item(i);

                sb.AppendLine($"Column {column.TitleObject.Caption}: UID = {column.UniqueID}");
            }

            System.IO.File.WriteAllText(filePath, sb.ToString());
        }

        /// <summary>Exporta a un archivo .txt el detalle de columnas y valores de cada fila de la matriz.</summary>
        public static void ExportarDetalleColumnasMatrizATxt(SAPbouiCOM.Matrix oMtx, string filePath)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Matrix UID: {oMtx.Item.UniqueID}");
            sb.AppendLine($"Total Columns: {oMtx.Columns.Count}");
            sb.AppendLine($"Total Rows: {oMtx.RowCount}");
            sb.AppendLine(new string('=', 80));

            for (int c = 0; c < oMtx.Columns.Count; c++)
            {
                var column = oMtx.Columns.Item(c);
                string colType = column.Type.ToString();
                string colUid = column.UniqueID;
                string colCaption = column.TitleObject.Caption;

                sb.AppendLine($"[Col {c}] Caption: \"{colCaption}\" | UID: {colUid} | Type: {colType}");

                for (int r = 1; r <= oMtx.RowCount; r++)
                {
                    string cellValue = string.Empty;
                    try
                    {
                        var cell = oMtx.GetCellSpecific(colUid, r);

                        if (cell is SAPbouiCOM.EditText et)
                            cellValue = et.Value;
                        else if (cell is SAPbouiCOM.ComboBox cb)
                            cellValue = cb.Value;
                        else if (cell is SAPbouiCOM.CheckBox chk)
                            cellValue = chk.Checked ? "Y" : "N";
                        else if (cell is SAPbouiCOM.ButtonCombo bc)
                            cellValue = bc.Selected?.Value ?? string.Empty;
                        else
                            cellValue = "(tipo no soportado)";
                    }
                    catch
                    {
                        cellValue = "(error al leer)";
                    }

                    sb.AppendLine($"    Row {r}: {cellValue}");
                }

                sb.AppendLine(new string('-', 80));
            }

            System.IO.File.WriteAllText(filePath, sb.ToString());
        }
    }
}