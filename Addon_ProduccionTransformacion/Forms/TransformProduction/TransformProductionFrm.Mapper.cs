using Addon_TransformProduction.Models;
using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Addon_TransformProduction.Forms.TransformProduction
{
    public partial class TransformProductionFrm
    {
        /// <summary>
        /// Convierte el Recordset de BOM (ver <see cref="ObtenerListaMateriales"/>) en la lista
        /// de modelos que consume la grilla. El Recordset se posiciona en EoF al terminar.
        /// </summary>
        public List<GetBillOfMaterialsModel> MapearListaMateriales(ref Recordset recordset)
        {
            var result = new List<GetBillOfMaterialsModel>();
            if (recordset == null) return result;

            var fields = recordset.Fields;
            while (!recordset.EoF)
            {
                result.Add(new GetBillOfMaterialsModel
                {
                    SubItemCode = fields.Item("ItemCode").Value,
                    SubItemName = fields.Item("ItemName").Value,
                    Quantity = Convert.ToDecimal(fields.Item("Quantity").Value),
                    Warehouse = fields.Item("Warehouse").Value,
                    UoM = fields.Item("InvntryUom").Value,
                    LastPurPrc = Convert.ToDecimal(fields.Item("LastPurPrc").Value),
                    LastPurCur = fields.Item("LastPurCur").Value,
                    MnfDate = Convert.ToDateTime(fields.Item("MnfDate").Value),
                    AutoExpDate = Convert.ToDateTime(fields.Item("AutoExpDate").Value),
                    InDate = Convert.ToDateTime(fields.Item("InDate").Value),
                    AutoBatchNumber = fields.Item("AutoBatchNumber").Value
                });
                recordset.MoveNext();
            }
            return result;
        }

        /// <summary>
        /// Mapea los campos de la cabecera del UDO desde el DBDataSource bound del formulario
        /// (offset activo por defecto) a un <see cref="TransformProductionUdoModel"/>.
        /// </summary>
        public TransformProductionUdoModel MapearCabeceraUdo(SAPbouiCOM.DBDataSource oDbDataSource, int row = -1)
        {
            int r = row < 0 ? oDbDataSource.Offset : row;
            return new TransformProductionUdoModel
            {
                Status = oDbDataSource.GetValue(CONSTANTS.TABLES.FIELDS_HEAD_DB.STATUS, r) ?? string.Empty,
                ItemCode = oDbDataSource.GetValue(CONSTANTS.TABLES.FIELDS_HEAD_DB.ITEMCODE, r) ?? string.Empty,
                Quantity = ParseDouble(oDbDataSource.GetValue(CONSTANTS.TABLES.FIELDS_HEAD_DB.QUANTITY, r)),
                EntryDocEntry = ParseInt(oDbDataSource.GetValue(CONSTANTS.TABLES.FIELDS_HEAD_DB.ENTRY_DOC_ENTRY, r)),
                ExitDocEntry = ParseInt(oDbDataSource.GetValue(CONSTANTS.TABLES.FIELDS_HEAD_DB.EXIT_DOC_ENTRY, r)),
                EntryRevDocEntry = ParseInt(oDbDataSource.GetValue(CONSTANTS.TABLES.FIELDS_HEAD_DB.ENTRY_REV_DOC_ENTRY, r)),
                ExitRevDocEntry = ParseInt(oDbDataSource.GetValue(CONSTANTS.TABLES.FIELDS_HEAD_DB.EXIT_REV_DOC_ENTRY, r))
            };
        }

        private static int ParseInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            if (int.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out var result)) return result;
            if (int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result)) return result;
            return 0;
        }
    }
}