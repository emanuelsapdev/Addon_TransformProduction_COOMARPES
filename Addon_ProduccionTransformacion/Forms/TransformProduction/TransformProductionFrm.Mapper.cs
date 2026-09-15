using Addon_TransformProduction.Models;
using SAPbobsCOM;
using System;
using System.Collections.Generic;

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
                    UoM = fields.Item("InvntryUom").Value
                });
                recordset.MoveNext();
            }
            return result;
        }
    }
}