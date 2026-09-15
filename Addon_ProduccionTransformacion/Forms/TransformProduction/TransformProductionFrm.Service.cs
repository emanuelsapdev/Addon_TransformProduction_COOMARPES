using Addon_TransformProduction.Common;
using Addon_TransformProduction.Services;
using Addon_TransformProduction.Tools;
using SAPbobsCOM;
using System;

namespace Addon_TransformProduction.Forms.TransformProduction
{
    public partial class TransformProductionFrm
    {
        /// <summary>
        /// Orquesta el flujo al perder el foco el campo de código de artículo de la cabecera:
        /// lee el código, lo guarda en el contexto, consulta el BOM (Repository), lo mapea
        /// (Mapper) y pinta la grilla (FormReader).
        /// </summary>
        public void ManejarCodigoArticuloPerdidaFoco(SAPbouiCOM.Form oForm)
        {
            string itemCode = LeerCodigoArticulo(oForm);
            if (string.IsNullOrWhiteSpace(itemCode)) return;

            var context = ContextManager.ObtenerOCrear(oForm.TypeCount.ToString());
            context.PrincipalItemCode = itemCode;

            Recordset oRecordSet = null;
            try
            {
                oRecordSet = (Recordset)ConnectionSDK.DIAPI.GetBusinessObject(BoObjectTypes.BoRecordset);
                oRecordSet = ObtenerListaMateriales(oRecordSet, itemCode);

                var data = MapearListaMateriales(ref oRecordSet);

                if (data != null && data.Count > 0)
                {
                    PoblarGrillaMateriales(oForm, data);
                }
                else
                {
                    LimpiarGrillaMateriales(oForm);
                    NotificationService.MostrarAlerta(
                        $"No se encontraron datos para el código de artículo: {itemCode}");
                }
            }
            finally
            {
                if (oRecordSet != null) MarshalGC.LiberarComObject(oRecordSet);
            }
        }

        /// <summary>
        /// Orquesta el flujo al perder el foco el campo de cantidad consumida de la cabecera:
        /// lee la cantidad, la valida y actualiza el contexto.
        /// </summary>
        public void ManejarCantidadConsumidaPerdidaFoco(SAPbouiCOM.Form oForm)
        {
            string qtyConsumed = LeerCantidadConsumida(oForm);

            if (!EsCantidadConsumidaValida(qtyConsumed, out double qty)) return;

            var context = ContextManager.ObtenerOCrear(oForm.TypeCount.ToString());
            context.PrincipalQuantityConsumed = qty;
        }
    }
}