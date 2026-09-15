using Addon_TransformProduction.Tools;
using SAPbobsCOM;
using System;
using System.Runtime.InteropServices;

namespace Addon_TransformProduction.Forms.WindowBatches
{
    public partial class WindowBatchesFrm
    {
        /// <summary>
        /// Listado de lotes de un artículo (obtenidos de la tabla OBTN).
        /// </summary>
        /// <param name="oRec">Recordset a ejecutar (lo libera el caller en finally).</param>
        /// <param name="itemCode">Código del artículo.</param>
        /// <returns>
        /// Recordset con las columnas: "Batch", "Warehouse", "QtyTotal", "QtyAssigned", "ExpDate".
        /// </returns>
        public Recordset ObtenerLotesPorArticulo(Recordset oRec, string itemCode)
        {
            if (oRec == null) throw new ArgumentNullException(nameof(oRec));
            if (string.IsNullOrWhiteSpace(itemCode)) return null;

            try
            {
                string q = $@"
                                SELECT 
                                    T0.""DistNumber"" AS ""Batch"",
                                    T1.""WhsCode"" AS ""Warehouse"",
                                    IFNULL(T1.""Quantity"", 0) AS ""QtyTotal"",
                                    0 AS ""QtyAssigned"",
                                    T0.""ExpDate"" AS ""ExpDate""
                                FROM OBTN T0
                                INNER JOIN OBTQ T1
                                    ON T1.""SysNumber"" = T0.""SysNumber"" AND T1.""ItemCode"" = T0.""ItemCode"" 
                                WHERE T0.""ItemCode"" = '{SqlEscapeHelper.EscapeSql(itemCode)}' AND T1.""Quantity"" > 0
                                ORDER BY ""ExpDate"" ASC;
                                ";
                oRec.DoQuery(q);
                return oRec;
            }
            catch
            {
                if (oRec != null) Marshal.ReleaseComObject(oRec);
                throw;
            }
        }
    }
}