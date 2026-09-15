using Addon_TransformProduction.Models;
using SAPbobsCOM;
using System;
using System.Collections.Generic;

namespace Addon_TransformProduction.Forms.WindowBatches
{
    public partial class WindowBatchesFrm
    {
        /// <summary>
        /// Convierte el Recordset de lotes (ver <see cref="ObtenerLotesPorArticulo"/>) en la
        /// lista de modelos que consume la grilla. El Recordset se posiciona en EoF al terminar.
        /// </summary>
        public List<BatchModel> MapearLotesPorArticulo(ref Recordset recordset)
        {
            var result = new List<BatchModel>();
            if (recordset == null) return result;

            var fields = recordset.Fields;
            while (!recordset.EoF)
            {
                result.Add(new BatchModel
                {
                    BatchNumber = fields.Item("Batch").Value,
                    Warehouse = fields.Item("Warehouse").Value,
                    QuantityAvailable = Convert.ToDouble(fields.Item("QtyTotal").Value),
                    QuantityAssigned = Convert.ToDouble(fields.Item("QtyAssigned").Value),
                    ExpDate = Convert.ToDateTime(fields.Item("ExpDate").Value)
                });
                recordset.MoveNext();
            }
            return result;
        }
    }
}