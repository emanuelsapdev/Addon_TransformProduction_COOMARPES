using Addon_TransformProduction.Common;
using Addon_TransformProduction.Tools;
using SAPbobsCOM;
using System;
using System.Runtime.InteropServices;

namespace Addon_TransformProduction.Forms.TransformProduction
{
    public partial class TransformProductionFrm
    {
        /// <summary>
        /// Listado de materiales (BOM) activo de un artículo padre.
        /// </summary>
        /// <param name="oRec">Recordset a ejecutar (lo libera el caller en finally).</param>
        /// <param name="itemCode">Código del artículo padre.</param>
        /// <returns>
        /// Recordset con las columnas: "Code" (árbol), "ItemCode", "ItemName", "Quantity",
        /// "Warehouse", "InvntryUom".
        /// </returns>
        public Recordset ObtenerListaMateriales(Recordset oRec, string itemCode)
        {
            if (oRec == null) throw new ArgumentNullException(nameof(oRec));
            if (string.IsNullOrWhiteSpace(itemCode)) return null;

            try
            {
                oRec.DoQuery($@"SELECT t0.""Code"", t1.""Code"" AS ""ItemCode"", t1.""ItemName"", t1.""Quantity"", t1.""Warehouse"", t2.""InvntryUom""   
                                FROM OITT t0
                                INNER JOIN ITT1 t1 ON t1.""Father"" = t0.""Code""
                                INNER JOIN OITM t2 ON t2.""ItemCode"" = t1.""Code""
                                WHERE t0.""TreeType"" = 'P'
                                AND t1.""Type"" = '4'
                                AND t0.""Code"" = '{SqlEscapeHelper.EscapeSql(itemCode)}';");
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