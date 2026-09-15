using System;
using System.Globalization;

namespace Addon_TransformProduction.Services
{
    /// <summary>
    /// Conversión de valores capturados desde SAP B1 (texto de la UI, SAP HANA) a tipos .NET.
    /// </summary>
    public class ConverterService
    {
        /// <summary>
        /// Convierte una cadena con formato numérico invariante (punto decimal, sin separador
        /// de miles) a decimal redondeado a 2 posiciones.
        /// </summary>
        public static decimal ConvertirDecimalDesdeCadenaSAP(string val)
        {
            if (string.IsNullOrEmpty(val)) return 0;
            decimal result = decimal.Parse(val.Trim(), CultureInfo.InvariantCulture);
            return decimal.Round(result, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Convierte una fecha en formato SAP "yyyyMMdd" a DateTime.
        /// </summary>
        public static DateTime ConvertirFechaHoraDesdeCadenaSAP(string value)
        {
            return DateTime.ParseExact(value.Trim(), "yyyyMMdd", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Convierte una cadena de moneda (con prefijo ARS/USD, separador de miles con . y
        /// decimales con ,) a decimal. Devuelve 0 si no se puede interpretar.
        /// </summary>
        public static decimal ConvertirDecimalMonedaDesdeCadenaSAP(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0m;

            string cleaned = value
                .Replace("ARS", "")
                .Replace("USD", "")
                .Replace("\"", "")
                .Replace(" ", "")
                .Replace(".", "")
                .Replace(",", ".");

            return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result)
                ? result
                : 0m;
        }
    }
}