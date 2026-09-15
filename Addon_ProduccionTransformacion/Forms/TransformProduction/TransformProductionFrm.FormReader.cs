using Addon_TransformProduction.Models;
using SAPbouiCOM;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Addon_TransformProduction.Forms.TransformProduction
{
    public partial class TransformProductionFrm
    {
        /// <summary>
        /// Código de artículo cargado en la cabecera del formulario, o null/vacío.
        /// </summary>
        private string LeerCodigoArticulo(SAPbouiCOM.Form oForm)
        {
            var oItemCode = (EditText)oForm.Items.Item(CONSTANTS.UID.HEADER.ITEM_CODE).Specific;
            return oItemCode.Value?.Trim();
        }

        /// <summary>
        /// Cantidad consumida cargada en la cabecera del formulario, o null/vacío.
        /// </summary>
        private string LeerCantidadConsumida(SAPbouiCOM.Form oForm)
        {
            var oQuantity = (EditText)oForm.Items.Item(CONSTANTS.UID.HEADER.QUANTITY).Specific;
            return oQuantity.Value?.Trim();
        }

        /// <summary>
        /// Puebla la grilla de materiales a partir de las líneas del BOM ya mapeadas
        /// (ver Service.ManejarCodigoArticuloPerdidaFoco). Escribe vía DBDataSource +
        /// LoadFromDataSource; nada de queries SQL acá.
        /// </summary>
        private void PoblarGrillaMateriales(SAPbouiCOM.Form oForm, List<GetBillOfMaterialsModel> data)
        {
            if (oForm == null) throw new ArgumentNullException(nameof(oForm));
            if (data == null) throw new ArgumentNullException(nameof(data));

            oForm.Freeze(true);

            var oMatrix = (Matrix)oForm.Items.Item(CONSTANTS.UID.GRID).Specific;
            var oDbDataSource = oForm.DataSources.DBDataSources.Item(CONSTANTS.TABLES.TRANSFORM_PRODUCTION_LINE_WITH_AT);

            RedimensionarDataSource(oDbDataSource, data.Count);

            for (int i = 0; i < data.Count; i++)
            {
                var row = data[i];

                oDbDataSource.SetValue(CONSTANTS.TABLES.FIELDS_LINE_DB.SUBPRODUCT, i, row.SubItemCode ?? string.Empty);
                oDbDataSource.SetValue(CONSTANTS.TABLES.FIELDS_LINE_DB.SUBPRODUCT_NAME, i, row.SubItemName ?? string.Empty);
                oDbDataSource.SetValue(CONSTANTS.TABLES.FIELDS_LINE_DB.QUANTITY_OBTAINED, i, row.Quantity.ToString(CultureInfo.InvariantCulture));
                oDbDataSource.SetValue(CONSTANTS.TABLES.FIELDS_LINE_DB.UNIT_MEASUREMENT, i, row.UoM ?? string.Empty);
                oDbDataSource.SetValue(CONSTANTS.TABLES.FIELDS_LINE_DB.LAST_PUR_PRICE, i, string.Empty);
                oDbDataSource.SetValue(CONSTANTS.TABLES.FIELDS_LINE_DB.PRICE, i, string.Empty);
            }

            oMatrix.LoadFromDataSource();
            oMatrix.AutoResizeColumns();
            oForm.Freeze(false);
        }

        /// <summary>
        /// Vacía la grilla de materiales (cuando el artículo no tiene BOM o se borró el código).
        /// </summary>
        private void LimpiarGrillaMateriales(SAPbouiCOM.Form oForm)
        {
            if (oForm == null) throw new ArgumentNullException(nameof(oForm));

            oForm.Freeze(true);

            var oMatrix = (Matrix)oForm.Items.Item(CONSTANTS.UID.GRID).Specific;
            var oDbDataSource = oForm.DataSources.DBDataSources.Item(CONSTANTS.TABLES.TRANSFORM_PRODUCTION_LINE_WITH_AT);

            RedimensionarDataSource(oDbDataSource, 0);

            oMatrix.LoadFromDataSource();
            oForm.Freeze(false);
        }

        /// <summary>
        /// Deja el DBDataSource con exactamente <paramref name="cantidadFilas"/> filas,
        /// removiendo las filas sobrantes de atrás hacia adelante (RemoveRecord reindexa).
        /// </summary>
        private static void RedimensionarDataSource(DBDataSource oDbDataSource, int cantidadFilas)
        {
            for (int i = oDbDataSource.Size - 1; i >= cantidadFilas; i--)
                oDbDataSource.RemoveRecord(i);

            for (int i = oDbDataSource.Size; i < cantidadFilas; i++)
                oDbDataSource.InsertRecord(i);
        }
    }
}