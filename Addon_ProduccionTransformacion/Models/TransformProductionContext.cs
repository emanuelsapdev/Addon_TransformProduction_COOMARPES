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
        public int InventoryGenExitsRevDocEntry { get; set; }
        public int InventoryGenEntriesRevDocEntry { get; set; }

        /// <summary>
        /// El usuario eligió "Crear" (Completado) y la validación pasó en el BeforeAction: al
        /// agregarse el UDO hay que generar la Entrada/Salida. No lo pisa et_FORM_ACTIVATE.
        /// </summary>
        public bool CompletarAlAgregar { get; set; }

        /// <summary>
        /// Entrada (subproductos) y salida (lotes del principal) validadas en el BeforeAction de
        /// Crear. Se guardan aparte porque después de agregar el formulario queda en un
        /// documento nuevo y et_FORM_ACTIVATE reescribe InventoryGenEntriesData desde la grilla.
        /// </summary>
        public InventoryGenModel EntradasAlAgregar { get; set; }
        public InventoryGenModel SalidasAlAgregar { get; set; }

        /// <summary>
        /// Último modo de formulario observado: permite detectar la transición fm_OK_MODE →
        /// fm_ADD_MODE (botón "Nuevo") sin depender de FormModeEx, para resetear el contexto
        /// justo al empezar un documento nuevo.
        /// </summary>
        public SAPbouiCOM.BoFormMode? UltimoModoFormulario { get; set; }

        /// <summary>
        /// Devuelve el contexto a sus valores iniciales (los del constructor): borra el estado
        /// del registro anterior (artículo, cantidad, estado, XML de lotes, modelos de salida y
        /// entrada y DocEntry). Conserva las referencias de formulario (FormTransfProd/FormBatches),
        /// que pertenecen a la ventana y no al registro.
        /// </summary>
        public void ResetearContexto()
        {
            PrincipalItemCode = string.Empty;
            PrincipalQuantityConsumed = 0;
            PrincipalStatus = string.Empty;

            BatchHeadXml = string.Empty;
            InventoryGenExitsData = new InventoryGenModel();
            InventoryGenEntriesData = new InventoryGenModel();

            InventoryGenExitsDocEntry = 0;
            InventoryGenEntriesDocEntry = 0;
            InventoryGenExitsRevDocEntry = 0;
            InventoryGenEntriesRevDocEntry = 0;

            CompletarAlAgregar = false;
            EntradasAlAgregar = null;
            SalidasAlAgregar = null;
        }
    }
}