using System;

namespace Addon_TransformProduction.Models
{
    /// <summary>
    /// Línea de material listada para un artículo padre del formulario de producción/transformación
    /// (resultado de GetBillOfMaterials en TransformProductionFrm.Repository.cs).
    /// </summary>
    public class GetBillOfMaterialsModel
    {
        public string SubItemCode { get; set; }
        public string SubItemName { get; set; }
        public decimal Quantity { get; set; }
        public string Warehouse { get; set; }
        public string UoM { get; set; }
        public decimal LastPurPrc { get; set; }
        public string LastPurCur { get; set; }
        /// <summary>Fechas del lote a crear; null si el subproducto no se maneja por lotes.</summary>
        public DateTime? MnfDate { get; set; }
        public DateTime? AutoExpDate { get; set; }
        public DateTime? InDate { get; set; }
        public string AutoBatchNumber { get; set; }
    }
}