using Addon_TransformProduction.Models;
using Addon_TransformProduction.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Addon_TransformProduction.Forms.ActualizacionCostos
{
    public partial class ActualizacionCostosFrm
    {
        /// <summary>
        /// Códigos de almacén del Excel (columna "Almacén") que NO existen en OWHS.
        /// Se corre ANTES de cargar la grilla: si devuelve algo, se aborta la carga.
        /// </summary>
        private List<string> ObtenerCodigosAlmacenInvalidos(List<ActualizacionCostosFormRow> rows)
        {
            return rows
                .Select(r => r.WhsCode)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(c => !WarehouseRepository.Existe(c))
                .ToList();
        }

        /// <summary>
        /// Valida cada fila (artículo/almacén/lotes-series/costo) y marca si corresponde
        /// o no al almacén elegido en el edit-text de cabecera.
        /// </summary>
        private void ValidarTodasLasFilas(List<ActualizacionCostosFormRow> rows, string selectedWhsCode)
        {
            foreach (var row in rows)
            {
                //ValidationService.ValidateRow(row);
                row.MatchesSelectedWarehouse = string.Equals(row.WhsCode, selectedWhsCode, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}