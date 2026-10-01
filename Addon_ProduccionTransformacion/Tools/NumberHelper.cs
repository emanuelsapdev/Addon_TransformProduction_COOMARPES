using System;

namespace Addon_TransformProduction.Tools
{
    /// <summary>
    /// Comparaciones de cantidades (double) con un margen mínimo. Comparar con ==/!= falla por
    /// redondeo binario: p.ej. 1,1 + 2,2 da 3,3000000000000003 y no es "igual" a 3,3.
    /// </summary>
    public static class NumberHelper
    {
        /// <summary>Margen de comparación de cantidades (muy por debajo de los decimales de SAP).</summary>
        public const double TOLERANCIA_CANTIDAD = 0.0001;

        /// <summary>True si <paramref name="a"/> y <paramref name="b"/> difieren a lo sumo en el margen.</summary>
        public static bool SonIguales(double a, double b, double tolerancia = TOLERANCIA_CANTIDAD)
        {
            return Math.Abs(a - b) <= tolerancia;
        }

        /// <summary>True si <paramref name="a"/> supera a <paramref name="b"/> en más que el margen.</summary>
        public static bool EsMayor(double a, double b, double tolerancia = TOLERANCIA_CANTIDAD)
        {
            return a - b > tolerancia;
        }
    }
}
