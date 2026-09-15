using System;

namespace Addon_TransformProduction.Tools
{
    /// <summary>
    /// Utilidades compartidas para construir queries HANA de forma segura.
    /// Centraliza el escape de comillas simples que antes estaba duplicado como
    /// método privado en cada repositorio (TransformProductionFrm.Repository,
    /// WindowBatchesFrm.Repository, ItemMasterRepository, WarehouseRepository).
    /// </summary>
    public static class SqlEscapeHelper
    {
        /// <summary>
        /// Escapa comillas simples para interpolación de strings en queries HANA.
        /// </summary>
        /// <param name="value">Valor a interpolar en el SQL.</param>
        /// <returns>Valor con las comillas simples duplicadas (''), o cadena vacía si es null.</returns>
        public static string EscapeSql(object value)
        {
            return (value?.ToString() ?? string.Empty).Replace("'", "''");
        }
    }
}