namespace Addon_TransformProduction.Models
{
    /// <summary>
    /// DTO de la cabecera del UDO de Producción/Transformación: hidrata el contexto
    /// (<see cref="TransformProductionContext"/>) al reabrir un documento, con los DocEntry
    /// de entrada/salida/reversión. Los lotes de la salida viven solo en memoria y no se
    /// persisten en la base.
    /// </summary>
    public class TransformProductionUdoModel
    {
        public string Status { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
        public double Quantity { get; set; }
        public int EntryDocEntry { get; set; }
        public int ExitDocEntry { get; set; }
        public int EntryRevDocEntry { get; set; }
        public int ExitRevDocEntry { get; set; }
    }
}