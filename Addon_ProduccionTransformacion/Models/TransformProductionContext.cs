using System.Collections.Generic;

namespace Addon_TransformProduction.Models
{
    /// <summary>
    /// Estado en memoria del formulario de Producción/Transformación: contiene el
    /// código del artículo principal, su cantidad consumida, y la información de lotes
    /// seleccionados en el formulario de lotes (WindowBatches).
    /// </summary>
    public class TransformProductionContext
    {
        public string PrincipalItemCode { get; set; } = string.Empty;
        public double PrincipalQuantityConsumed { get; set; }
        public string PrincipalStatus { get; set; } = string.Empty;

        public string BatchHeadXml { get; set; } = string.Empty;

        /// <summary>Referencia al formulario UDO de Producción/Transformación (abierto desde SAP).</summary>
        public SAPbouiCOM.Form FormTransfProd { get; set; }

        /// <summary>Referencia al formulario WindowBatches (selección de lotes), si está abierto.</summary>
        public SAPbouiCOM.Form FormBatches { get; set; }
        
        public InventoryGenModel InventoryGenExitsData { get; set; } = new InventoryGenModel();
        public InventoryGenModel InventoryGenEntriesData { get; set; } = new InventoryGenModel();

        public int InventoryGenExitsDocEntry { get; set; }
        public int InventoryGenEntriesDocEntry { get; set; }
    }
}