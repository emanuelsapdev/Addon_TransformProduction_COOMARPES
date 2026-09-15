using Addon_TransformProduction.Common;
using Addon_TransformProduction.Tools;
using SAPbobsCOM;
using System;
using System.Runtime.InteropServices;

namespace Addon_TransformProduction.Repositories
{
    /// <summary>
    /// Repositorio de consultas contra el maestro de artículos (OITM) y stock por almacén (OITW).
    /// </summary>
    public static class ItemMasterRepository
    {
        /// <summary>Indica si el código de artículo existe en OITM.</summary>
        public static bool Existe(string itemCode)
        {
            if (string.IsNullOrWhiteSpace(itemCode)) return false;

            Recordset oRec = null;
            try
            {
                oRec = (Recordset)ConnectionSDK.DIAPI.GetBusinessObject(BoObjectTypes.BoRecordset);
                oRec.DoQuery($@"SELECT 1 FROM OITM WHERE ""ItemCode"" = '{SqlEscapeHelper.EscapeSql(itemCode)}'");
                return !oRec.EoF;
            }
            finally
            {
                if (oRec != null) Marshal.ReleaseComObject(oRec);
            }
        }

        /// <summary>Devuelve el nombre del artículo, o null si no existe.</summary>
        public static string ObtenerNombreArticulo(string itemCode)
        {
            if (string.IsNullOrWhiteSpace(itemCode)) return null;

            Recordset oRec = null;
            try
            {
                oRec = (Recordset)ConnectionSDK.DIAPI.GetBusinessObject(BoObjectTypes.BoRecordset);
                oRec.DoQuery($@"SELECT ""ItemName"" FROM OITM WHERE ""ItemCode"" = '{SqlEscapeHelper.EscapeSql(itemCode)}'");
                if (oRec.EoF) return null;
                return oRec.Fields.Item(0).Value?.ToString();
            }
            finally
            {
                if (oRec != null) Marshal.ReleaseComObject(oRec);
            }
        }

        /// <summary>
        /// Indica si el artículo maneja números de lote y/o series
        /// (OITM."ManBtchNum" / "ManSerNum" = 'Y'). Por el momento el addon no soporta
        /// artículos con lotes/series: estas filas se rechazan en la validación.
        /// </summary>
        public static bool ManejaLotesOSeries(string itemCode)
        {
            if (string.IsNullOrWhiteSpace(itemCode)) return false;

            Recordset oRec = null;
            try
            {
                oRec = (Recordset)ConnectionSDK.DIAPI.GetBusinessObject(BoObjectTypes.BoRecordset);
                oRec.DoQuery($@"SELECT ""ManBtchNum"", ""ManSerNum"" FROM OITM WHERE ""ItemCode"" = '{SqlEscapeHelper.EscapeSql(itemCode)}'");
                if (oRec.EoF) return false;

                string manBatch = oRec.Fields.Item(0).Value?.ToString();
                string manSerial = oRec.Fields.Item(1).Value?.ToString();
                return manBatch == CONSTANTS_GLOBALS.FixedValues.Yes || manSerial == CONSTANTS_GLOBALS.FixedValues.Yes;
            }
            finally
            {
                if (oRec != null) Marshal.ReleaseComObject(oRec);
            }
        }

        /// <summary>
        /// Costo actual del artículo en el almacén indicado (OITW."AvgPrice").
        /// ⚠ Este valor depende del método de valuación configurado (Estándar/Promedio Móvil/FIFO).
        /// Se guarda únicamente como referencia histórica en el staging: el costo real que se usa
        /// para la actualización lo vuelve a calcular el Windows Service al momento de ejecutar.
        /// </summary>
        public static decimal ObtenerCostoActual(string itemCode, string whsCode)
        {
            if (string.IsNullOrWhiteSpace(itemCode) || string.IsNullOrWhiteSpace(whsCode)) return 0m;

            Recordset oRec = null;
            try
            {
                oRec = (Recordset)ConnectionSDK.DIAPI.GetBusinessObject(BoObjectTypes.BoRecordset);
                oRec.DoQuery($@"SELECT ""AvgPrice"" FROM OITW WHERE ""ItemCode"" = '{SqlEscapeHelper.EscapeSql(itemCode)}' AND ""WhsCode"" = '{SqlEscapeHelper.EscapeSql(whsCode)}'");
                if (oRec.EoF) return 0m;

                var val = oRec.Fields.Item(0).Value;
                return val == null ? 0m : Convert.ToDecimal(val);
            }
            finally
            {
                if (oRec != null) Marshal.ReleaseComObject(oRec);
            }
        }
    }
}