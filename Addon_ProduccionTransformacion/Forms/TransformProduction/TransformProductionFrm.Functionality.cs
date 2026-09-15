using Addon_TransformProduction.Models;
using Addon_TransformProduction.Services;
using System;
using System.Globalization;
using System.Linq;

namespace Addon_TransformProduction.Forms.TransformProduction
{
    public partial class TransformProductionFrm
    {
        /// <summary>
        /// Valida que la cantidad consumida cargada en la cabecera sea un número
        /// mayor que cero. Devuelve false si el valor está vacío o no es numérico.
        /// </summary>
        public bool EsCantidadConsumidaValida(string valor, out double cantidad)
        {
            cantidad = 0;

            if (string.IsNullOrWhiteSpace(valor)) return false;

            bool esNumerico = double.TryParse(valor, NumberStyles.Any, CultureInfo.CurrentCulture, out cantidad)
                || double.TryParse(valor, NumberStyles.Any, CultureInfo.InvariantCulture, out cantidad);

            return esNumerico && cantidad > 0;
        }

        public bool ValidarFormulario(SAPbouiCOM.Form oForm, TransformProductionContext ctx)
        {
            if (ctx == null)
            {
                NotificationService.MostrarError("No se pudo obtener el contexto de la producción.");
                return false;
            }
            string itemCode = LeerCodigoArticulo(oForm);
            string qtyConsumed = LeerCantidadConsumida(oForm);
            if (string.IsNullOrWhiteSpace(itemCode))
            {
                NotificationService.MostrarError("Debe seleccionar un artículo para producir.");
                return false;
            }

            if (!EsCantidadConsumidaValida(qtyConsumed, out double cantidad))
            {
                NotificationService.MostrarError("La cantidad consumida debe ser un número mayor que cero.");
                return false;
            }

            if(ctx.BatchHead.Select(i => i.QuantityAssigned).Sum() != cantidad)
            {
                NotificationService.MostrarError("La cantidad total asignada no coincide con la Cantidad Consumida.");
                return false;
            }

            ctx.PrincipalItemCode = itemCode;
            ctx.PrincipalQuantityConsumed = cantidad;
            return true;
        }
    }
}