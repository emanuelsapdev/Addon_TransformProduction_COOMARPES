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
                    MnfDate = FechaOpcional(fields.Item("MnfDate").Value),
                    AutoExpDate = FechaOpcional(fields.Item("AutoExpDate").Value),
                    InDate = FechaOpcional(fields.Item("InDate").Value),
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

        /// <summary>
        /// Mapea la cabecera persistida del UDO (ver <see cref="ObtenerCabeceraUdoPersistida"/>)
        /// a un <see cref="TransformProductionUdoModel"/>, o null si el Recordset no tiene filas.
        /// </summary>
        public TransformProductionUdoModel MapearCabeceraUdo(Recordset recordset)
        {
            if (recordset == null || recordset.EoF) return null;

            var fields = recordset.Fields;
            return new TransformProductionUdoModel
            {
                Status = Convert.ToString(fields.Item(CONSTANTS.TABLES.FIELDS_HEAD_DB.STATUS).Value) ?? string.Empty,
                ItemCode = Convert.ToString(fields.Item(CONSTANTS.TABLES.FIELDS_HEAD_DB.ITEMCODE).Value) ?? string.Empty,
                Quantity = Convert.ToDouble(fields.Item(CONSTANTS.TABLES.FIELDS_HEAD_DB.QUANTITY).Value),
                EntryDocEntry = Convert.ToInt32(fields.Item(CONSTANTS.TABLES.FIELDS_HEAD_DB.ENTRY_DOC_ENTRY).Value),
                ExitDocEntry = Convert.ToInt32(fields.Item(CONSTANTS.TABLES.FIELDS_HEAD_DB.EXIT_DOC_ENTRY).Value),
                EntryRevDocEntry = Convert.ToInt32(fields.Item(CONSTANTS.TABLES.FIELDS_HEAD_DB.ENTRY_REV_DOC_ENTRY).Value),
                ExitRevDocEntry = Convert.ToInt32(fields.Item(CONSTANTS.TABLES.FIELDS_HEAD_DB.EXIT_REV_DOC_ENTRY).Value)
            };
        }

        /// <summary>
        /// Convierte el Recordset de almacenes (ver <see cref="ObtenerAlmacenesExistentes"/>) en
        /// un conjunto de códigos. El Recordset se posiciona en EoF al terminar.
        /// </summary>
        public HashSet<string> MapearCodigosAlmacen(Recordset recordset)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (recordset == null) return result;

            while (!recordset.EoF)
            {
                result.Add(Convert.ToString(recordset.Fields.Item("WhsCode").Value));
                recordset.MoveNext();
            }
            return result;
        }

        /// <summary>
        /// Convierte el Recordset de costos (ver <see cref="ObtenerCostosLineasSalida"/>) en un
        /// diccionario VisOrder → costo unitario. El Recordset se posiciona en EoF al terminar.
        /// </summary>
        public Dictionary<int, decimal> MapearCostosPorLinea(Recordset recordset)
        {
            var result = new Dictionary<int, decimal>();
            if (recordset == null) return result;

            while (!recordset.EoF)
            {
                int visOrder = Convert.ToInt32(recordset.Fields.Item("VisOrder").Value);
                result[visOrder] = Convert.ToDecimal(recordset.Fields.Item("StockPrice").Value);
                recordset.MoveNext();
            }
            return result;
        }

        /// <summary>
        /// Fecha de un campo del Recordset, o null si viene vacía: la DI API devuelve 30/12/1899
        /// cuando la vista trae NULL (subproducto que no se maneja por lotes).
        /// </summary>
        private static DateTime? FechaOpcional(object value)
        {
            if (value == null || value is DBNull) return null;
            DateTime fecha = Convert.ToDateTime(value);
            return fecha.Year <= 1900 ? (DateTime?)null : fecha;
        }

        /// <summary>
        /// Convierte el Recordset de artículos (ver <see cref="ObtenerArticulosConLote"/>) en un
        /// conjunto de códigos. El Recordset se posiciona en EoF al terminar.
        /// </summary>
        public HashSet<string> MapearCodigosArticulo(Recordset recordset)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (recordset == null) return result;

            while (!recordset.EoF)
            {
                result.Add(Convert.ToString(recordset.Fields.Item("ItemCode").Value));
                recordset.MoveNext();
            }
            return result;
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