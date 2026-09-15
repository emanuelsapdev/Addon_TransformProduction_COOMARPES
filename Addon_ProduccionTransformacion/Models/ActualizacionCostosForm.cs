using System;
using System.Collections.Generic;

namespace Addon_TransformProduction.Models
{
    /// <summary>
    /// Estado en memoria del formulario de Actualización de Costos:
    /// filas importadas del Excel + almacén/fecha de programación seleccionados.
    /// </summary>
    public class ActualizacionCostosForm
    {
        public DateTime? ScheduledDate { get; set; }

        /// <summary>Hora programada en formato "HH:mm".</summary>
        public string ScheduledTime { get; set; }

        public string ExcelFilePath { get; set; }

        /// <summary>Almacén único seleccionado en el combobox de cabecera para procesar esta tanda.</summary>
        public string SelectedWarehouse { get; set; }

        public List<ActualizacionCostosFormRow> Rows { get; set; } = new List<ActualizacionCostosFormRow>();
    }

    /// <summary>
    /// Una fila del Excel importado, con el resultado de su validación.
    /// </summary>
    public class ActualizacionCostosFormRow
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string WhsCode { get; set; }
        public string WhsName { get; set; }

        /// <summary>Costo actual capturado al momento de importar, a modo de referencia histórica.</summary>
        public decimal CurrentCost { get; set; }

        public decimal NewCost { get; set; }

        /// <summary>Pasó las validaciones de artículo/almacén/lotes-series/costo.</summary>
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }

        /// <summary>La fila corresponde al almacén elegido en el combobox de cabecera.
        /// Filas de otros almacenes no bloquean la confirmación: simplemente no se programan.</summary>
        public bool MatchesSelectedWarehouse { get; set; }
    }
}
