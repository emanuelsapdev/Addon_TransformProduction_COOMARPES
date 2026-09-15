using Addon_TransformProduction.Common;
using Addon_TransformProduction.Tools;
using SAPbobsCOM;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Addon_TransformProduction.Repositories
{
    /// <summary>
    /// Repositorio de consultas contra el maestro de almacenes (OWHS).
    /// </summary>
    public static class WarehouseRepository
    {
        /// <summary>Indica si el código de almacén existe en OWHS.</summary>
        public static bool Existe(string whsCode)
        {
            if (string.IsNullOrWhiteSpace(whsCode)) return false;

            Recordset oRec = null;
            try
            {
                oRec = (Recordset)ConnectionSDK.DIAPI.GetBusinessObject(BoObjectTypes.BoRecordset);
                oRec.DoQuery($@"SELECT 1 FROM OWHS WHERE ""WhsCode"" = '{SqlEscapeHelper.EscapeSql(whsCode)}'");
                return !oRec.EoF;
            }
            finally
            {
                if (oRec != null) Marshal.ReleaseComObject(oRec);
            }
        }

        /// <summary>Devuelve el nombre del almacén, o null si no existe.</summary>
        public static string ObtenerNombreAlmacen(string whsCode)
        {
            if (string.IsNullOrWhiteSpace(whsCode)) return null;

            Recordset oRec = null;
            try
            {
                oRec = (Recordset)ConnectionSDK.DIAPI.GetBusinessObject(BoObjectTypes.BoRecordset);
                oRec.DoQuery($@"SELECT ""WhsName"" FROM OWHS WHERE ""WhsCode"" = '{SqlEscapeHelper.EscapeSql(whsCode)}'");
                if (oRec.EoF) return null;
                return oRec.Fields.Item(0).Value?.ToString();
            }
            finally
            {
                if (oRec != null) Marshal.ReleaseComObject(oRec);
            }
        }

        /// <summary>
        /// Lista todos los almacenes (código + nombre).
        /// </summary>
        /// <remarks>
        /// Sin uso actual: se usaba para poblar por código el combobox de cabecera del formulario
        /// de Actualización de Costos. Ese campo pasó a ser un EditText con ChooseFromList propio,
        /// que SAP puebla y completa solo — no hace falta este método para eso. Se deja por si
        /// hace falta la lista completa de almacenes en otro lado más adelante.
        /// </remarks>
        public static List<(string WhsCode, string WhsName)> ObtenerTodos()
        {
            var result = new List<(string WhsCode, string WhsName)>();

            Recordset oRec = null;
            try
            {
                oRec = (Recordset)ConnectionSDK.DIAPI.GetBusinessObject(BoObjectTypes.BoRecordset);
                oRec.DoQuery(@"SELECT ""WhsCode"", ""WhsName"" FROM OWHS ORDER BY ""WhsCode""");

                while (!oRec.EoF)
                {
                    string code = oRec.Fields.Item(0).Value?.ToString();
                    string name = oRec.Fields.Item(1).Value?.ToString();
                    result.Add((code, name));
                    oRec.MoveNext();
                }
            }
            finally
            {
                if (oRec != null) Marshal.ReleaseComObject(oRec);
            }

            return result;
        }
    }
}