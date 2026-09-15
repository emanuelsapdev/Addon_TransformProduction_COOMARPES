using Addon_TransformProduction.Models;
using Addon_TransformProduction.Services;
using SAPbouiCOM;
using System;
using System.Collections.Generic;

namespace Addon_TransformProduction.Forms.WindowBatches
{
    public partial class WindowBatchesFrm
    {
        /// <summary>
        /// Valida que ninguna línea asigne más cantidad que la disponible y que el total
        /// asignado coincida exactamente con la Cantidad Consumida de la cabecera de
        /// producción. Devuelve false y notifica si hay algún incumplimiento.
        /// </summary>
        public bool ValidarCantidadesAsignadas(SAPbouiCOM.DataTable oDataTable, TransformProductionContext ctx)
        {
            double totalQtyAsignada = 0;

            for (int i = 0; i < oDataTable.Rows.Count; i++)
            {
                double qty = Convert.ToDouble(oDataTable.GetValue(CONSTANTS.DATATABLE.COLUMNS.QTY, i));
                double qtyAssigned = Convert.ToDouble(oDataTable.GetValue(CONSTANTS.DATATABLE.COLUMNS.QTY_ASSIGNED, i));

                if (qtyAssigned > qty)
                {
                    NotificationService.MostrarAlerta($"La cantidad asignada de la línea {i + 1} no puede superar la cantidad disponible.");
                    return false;
                }

                totalQtyAsignada += qtyAssigned;
            }

            if (totalQtyAsignada > ctx.PrincipalQuantityConsumed)
            {
                NotificationService.MostrarAlerta("La cantidad total asignada no puede superar la Cantidad Consumida.");
                return false;
            }

            if (totalQtyAsignada != ctx.PrincipalQuantityConsumed)
            {
                NotificationService.MostrarAlerta("La cantidad elegida no coincide con la Cantidad Consumida.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Extrae los lotes marcados (check = "Y") con cantidad asignada mayor a cero
        /// del DataTable del formulario de lotes.
        /// </summary>
        public List<BatchModel> ObtenerLotesSeleccionados(SAPbouiCOM.DataTable oDataTable)
        {
            var resultado = new List<BatchModel>();

            for (int i = 0; i < oDataTable.Rows.Count; i++)
            {
                string batch = oDataTable.GetValue(CONSTANTS.DATATABLE.COLUMNS.BATCH, i);
                double qtyAssigned = Convert.ToDouble(oDataTable.GetValue(CONSTANTS.DATATABLE.COLUMNS.QTY_ASSIGNED, i));
                string check = oDataTable.GetValue(CONSTANTS.DATATABLE.COLUMNS.CHECK, i);

                if (qtyAssigned > 0 && check == "Y")
                {
                    resultado.Add(new BatchModel
                    {
                        BatchNumber = batch,
                        QuantityAssigned = qtyAssigned
                    });
                }
            }

            return resultado;
        }
    }
}