using Addon_TransformProduction.Models;
using Addon_TransformProduction.Services;
using Addon_TransformProduction.Tools;
using SAPbouiCOM;
using System;
using System.Collections.Generic;
using static Addon_TransformProduction.Models.InventoryGenModel;

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
                

                if (NumberHelper.EsMayor(qtyAssigned, qty))
                {
                    NotificationService.MostrarAlerta($"La cantidad asignada de la línea {i + 1} no puede superar la cantidad disponible.");
                    return false;
                }

                bool check = oDataTable.GetValue(CONSTANTS.DATATABLE.COLUMNS.CHECK, i) == "Y";
                if (check) totalQtyAsignada += qtyAssigned;
            }

            // Comparaciones con margen: la suma de decimales en double no es exacta (TP-15).
            if (NumberHelper.EsMayor(totalQtyAsignada, ctx.PrincipalQuantityConsumed))
            {
                NotificationService.MostrarAlerta("La cantidad total asignada no puede superar la Cantidad Consumida.");
                return false;
            }

            if (!NumberHelper.SonIguales(totalQtyAsignada, ctx.PrincipalQuantityConsumed))
            {
                NotificationService.MostrarAlerta("La cantidad elegida no coincide con la Cantidad Consumida.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Extrae los lotes marcados (check = "Y") con cantidad asignada mayor a cero
        /// del DataTable del formulario de lotes. Si hay lotes seleccionados en distintos
        /// almacenes, se genera una línea por almacén en el InventoryGenModel.
        /// </summary>
        public InventoryGenModel ObtenerLotesSeleccionados(SAPbouiCOM.DataTable oDataTable, TransformProductionContext ctx)
        {
            var invGen = new InventoryGenModel();

            // Agrupar Items por almacén
            var itemsPorAlmacen = new Dictionary<string, InventoryGenModel.Item>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < oDataTable.Rows.Count; i++)
            {
                string batchNum = oDataTable.GetValue(CONSTANTS.DATATABLE.COLUMNS.BATCH, i);
                double qty = Convert.ToDouble(oDataTable.GetValue(CONSTANTS.DATATABLE.COLUMNS.QTY_ASSIGNED, i));
                string check = oDataTable.GetValue(CONSTANTS.DATATABLE.COLUMNS.CHECK, i);
                string whs = oDataTable.GetValue(CONSTANTS.DATATABLE.COLUMNS.WAREHOUSE, i) ?? string.Empty;

                if (qty <= 0 || check != "Y")
                {
                    continue;
                }

                // Obtener o crear la línea para este almacén
                if (!itemsPorAlmacen.TryGetValue(whs, out var item))
                {
                    item = new InventoryGenModel.Item
                    {
                        ItemCode = ctx.PrincipalItemCode,
                        Warehouse = whs,
                        Quantity = 0d
                    };

                    itemsPorAlmacen.Add(whs, item);
                }

                // Crear el batch y añadirlo a la línea correspondiente
                var batchMdl = new InventoryGenModel.Item.Batch
                {
                    BatchNumber = batchNum,
                    Quantity = qty
                };

                item.AddBatch(batchMdl);

                // Sumar la cantidad del batch a la cantidad total de la línea
                item.Quantity += qty;
            }

            // Añadir todas las líneas agrupadas al modelo de inventario
            foreach (var kvp in itemsPorAlmacen)
            {
                invGen.Items.Add(kvp.Value);
            }

            return invGen;
        }
    }
}