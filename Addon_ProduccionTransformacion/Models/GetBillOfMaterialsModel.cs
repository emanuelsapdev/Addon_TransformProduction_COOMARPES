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
    }
}