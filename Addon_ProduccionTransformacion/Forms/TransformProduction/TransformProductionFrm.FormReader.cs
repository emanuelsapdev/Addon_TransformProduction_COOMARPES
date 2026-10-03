using Addon_TransformProduction.Common;
using Addon_TransformProduction.Models;
using Addon_TransformProduction.Tools;
using SAPbouiCOM;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Addon_TransformProduction.Forms.TransformProduction
{
    public partial class TransformProductionFrm
    {
        /// <summary>
        /// Código de artículo cargado en la cabecera del formulario (U_ITPS_ItemCode del
        /// DBDataSource), o null/vacío. Se lee del DBDataSource y no del EditText original
        /// porque ese campo queda oculto detrás del campo espejo con CFL filtrado.
        /// </summary>
        private string LeerCodigoArticulo(SAPbouiCOM.Form oForm)
        {
            var oDBDS = oForm.DataSources.DBDataSources.Item(CONSTANTS.TABLES.TRANSFORM_PRODUCTION_HEAD_WITH_AT);
            return oDBDS.GetValue(CONSTANTS.TABLES.FIELDS_HEAD_DB.ITEMCODE, oDBDS.Offset)?.Trim();
        }

        /// <summary>
        /// Código de artículo escrito/elegido en el campo espejo de "Producto" (UserDataSource).
        /// </summary>
        private string LeerCodigoArticuloEspejo(SAPbouiCOM.Form oForm)
        {
            var oUDS = oForm.DataSources.UserDataSources.Item(CONSTANTS.UID.CHOOSE_FROM_LIST.USER_DATASOURCE);
            return oUDS.ValueEx?.Trim();
        }

        /// <summary>
        /// Escribe el código de artículo en el UDF de la cabecera (DBDataSource, lo que se graba
        /// en el UDO) y en el campo espejo.
        /// </summary>
        private void EscribirCodigoArticulo(SAPbouiCOM.Form oForm, string itemCode)
        {
            var oDBDS = oForm.DataSources.DBDataSources.Item(CONSTANTS.TABLES.TRANSFORM_PRODUCTION_HEAD_WITH_AT);
            oDBDS.SetValue(CONSTANTS.TABLES.FIELDS_HEAD_DB.ITEMCODE, oDBDS.Offset, itemCode ?? string.Empty);

            var oUDS = oForm.DataSources.UserDataSources.Item(CONSTANTS.UID.CHOOSE_FROM_LIST.USER_DATASOURCE);
            oUDS.ValueEx = itemCode ?? string.Empty;
        }

        /// <summary>
        /// Vacía el campo espejo de "Producto" (después de agregar, el formulario queda en un
        /// documento nuevo).
        /// </summary>
        private void LimpiarCampoProductoEspejo(SAPbouiCOM.Form oForm)
        {
            try
            {
                oForm.DataSources.UserDataSources.Item(CONSTANTS.UID.CHOOSE_FROM_LIST.USER_DATASOURCE).ValueEx = string.Empty;
            }
            catch
            {
                // El formulario no tiene el campo espejo (falló su creación): nada que limpiar.
            }
        }

        /// <summary>
        /// Refleja en el campo espejo de "Producto" el valor del registro (DBDataSource) y lo deja
        /// editable solo en modo agregar (en un registro existente el artículo no se cambia).
        /// No hace nada si el formulario todavía no tiene el campo espejo.
        /// </summary>
        private void SincronizarCampoProducto(SAPbouiCOM.Form oForm)
        {
            UserDataSource oUDS;
            try
            {
                oUDS = oForm.DataSources.UserDataSources.Item(CONSTANTS.UID.CHOOSE_FROM_LIST.USER_DATASOURCE);
            }
            catch
            {
                return;
            }

            oUDS.ValueEx = LeerCodigoArticulo(oForm) ?? string.Empty;

            try
            {
                oForm.Items.Item(CONSTANTS.UID.CHOOSE_FROM_LIST.ITEM_CODE_MIRROR).Enabled = oForm.Mode == BoFormMode.fm_ADD_MODE;
            }
            catch
            {
                // SAP no deja deshabilitar el item que tiene el foco; se reintenta en la próxima sincronización.
            }
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
        /// Factor cargado en el campo Factor (UserDataSource, no se graba en el UDO), o 0 si está
        /// vacío o el formulario todavía no tiene el campo.
        /// </summary>
        private double LeerFactor(SAPbouiCOM.Form oForm)
        {
            string valor;
            try
            {
                valor = oForm.DataSources.UserDataSources.Item(CONSTANTS.UID.HEADER.FACTOR_DATASOURCE).ValueEx?.Trim();
            }
            catch
            {
                return 0;
            }

            if (double.TryParse(valor, NumberStyles.Any, CultureInfo.InvariantCulture, out double factor)) return factor;
            if (double.TryParse(valor, NumberStyles.Any, CultureInfo.CurrentCulture, out factor)) return factor;
            return 0;
        }

        /// <summary>
        /// Vacía el campo Factor (registro cargado, documento nuevo o factor inválido). No hace
        /// nada si el formulario todavía no tiene el campo.
        /// </summary>
        private void LimpiarFactor(SAPbouiCOM.Form oForm)
        {
            try
            {
                oForm.DataSources.UserDataSources.Item(CONSTANTS.UID.HEADER.FACTOR_DATASOURCE).ValueEx = "0";
            }
            catch
            {
                // El formulario todavía no tiene el campo Factor.
            }
        }

        /// <summary>
        /// Cantidad consumida de la cabecera leída del DBDataSource (0 si está vacía).
        /// </summary>
        private double LeerCantidadConsumidaDataSource(SAPbouiCOM.Form oForm)
        {
            var oDBDS = oForm.DataSources.DBDataSources.Item(CONSTANTS.TABLES.TRANSFORM_PRODUCTION_HEAD_WITH_AT);
            return LeerNumeroDataSource(oDBDS, CONSTANTS.TABLES.FIELDS_HEAD_DB.QUANTITY, oDBDS.Offset);
        }

        /// <summary>
        /// Multiplica la Cantidad consumida de la cabecera por <paramref name="factor"/>.
        /// </summary>
        private void MultiplicarCantidadCabecera(SAPbouiCOM.Form oForm, double factor)
        {
            var oDBDS = oForm.DataSources.DBDataSources.Item(CONSTANTS.TABLES.TRANSFORM_PRODUCTION_HEAD_WITH_AT);
            double cantidad = LeerCantidadConsumidaDataSource(oForm);
            oDBDS.SetValue(CONSTANTS.TABLES.FIELDS_HEAD_DB.QUANTITY, oDBDS.Offset, MultiplicarCantidad(cantidad, factor));
        }

        /// <summary>
        /// Multiplica la Cantidad obtenida de cada línea del detalle por <paramref name="factor"/>.
        /// Antes baja a la DBDataSource lo editado en la grilla, para no perderlo.
        /// </summary>
        private void MultiplicarCantidadesDetalle(SAPbouiCOM.Form oForm, double factor)
        {
            var oMatrix = (Matrix)oForm.Items.Item(CONSTANTS.UID.GRID).Specific;
            var oDBDS = oForm.DataSources.DBDataSources.Item(CONSTANTS.TABLES.TRANSFORM_PRODUCTION_LINE_WITH_AT);

            oMatrix.FlushToDataSource();

            for (int i = 0; i < oDBDS.Size; i++)
            {
                double cantidad = LeerNumeroDataSource(oDBDS, CONSTANTS.TABLES.FIELDS_LINE_DB.QUANTITY_OBTAINED, i);
                oDBDS.SetValue(CONSTANTS.TABLES.FIELDS_LINE_DB.QUANTITY_OBTAINED, i, MultiplicarCantidad(cantidad, factor));
            }

            oMatrix.LoadFromDataSource();
        }

        private static string MultiplicarCantidad(double cantidad, double factor)
        {
            return Math.Round(cantidad * factor, 6).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Valor numérico de un campo del DBDataSource (SAP lo devuelve con punto decimal, sin
        /// importar la configuración regional), o 0 si está vacío.
        /// </summary>
        private static double LeerNumeroDataSource(DBDataSource oDBDS, string campo, int fila)
        {
            string valor = oDBDS.GetValue(campo, fila)?.Trim();
            return double.TryParse(valor, NumberStyles.Any, CultureInfo.InvariantCulture, out double numero) ? numero : 0;
        }

        /// <summary>
        /// Escribe el estado (U_ITPS_Status) en el combo de cabecera del formulario, para que
        /// quede reflejado al agregar el registro del UDO.
        /// </summary>
        private void EscribirEstadoCabecera(SAPbouiCOM.Form oForm, string status)
        {
            var oStatus = (ComboBox)oForm.Items.Item(CONSTANTS.UID.HEADER.STATUS).Specific;
            oStatus.Select(status, BoSearchKey.psk_ByValue);
        }

        private string LeereEstadoCabecera(SAPbouiCOM.Form oForm)
        {
            SAPbouiCOM.DBDataSource oDBDS = oForm.DataSources.DBDataSources.Item(CONSTANTS.TABLES.TRANSFORM_PRODUCTION_HEAD_WITH_AT);
            string status = oDBDS.GetValue(CONSTANTS.TABLES.FIELDS_HEAD_DB.STATUS, oDBDS.Offset);
            return status;
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
                oDbDataSource.SetValue(CONSTANTS.TABLES.FIELDS_LINE_DB.WAREHOUSE, i, row.Warehouse ?? string.Empty);
                oDbDataSource.SetValue(CONSTANTS.TABLES.FIELDS_LINE_DB.LAST_PUR_PRICE, i, row.LastPurPrc.ToString(CultureInfo.InvariantCulture));
                oDbDataSource.SetValue(CONSTANTS.TABLES.FIELDS_LINE_DB.LAST_PUR_CUR, i, row.LastPurCur ?? string.Empty);
                oDbDataSource.SetValue(CONSTANTS.TABLES.FIELDS_LINE_DB.PRICE, i, row.LastPurPrc.ToString(CultureInfo.InvariantCulture));
                oDbDataSource.SetValue(CONSTANTS.TABLES.FIELDS_LINE_DB.CURRENT, i, !string.IsNullOrEmpty(row.LastPurCur) ? row.LastPurCur : "ARS");
                oDbDataSource.SetValue(CONSTANTS.TABLES.FIELDS_LINE_DB.BATCH_NUM, i, row.AutoBatchNumber ?? string.Empty);
                // Sin fechas si el subproducto no se maneja por lotes.
                oDbDataSource.SetValue(CONSTANTS.TABLES.FIELDS_LINE_DB.EXTDATE_BATCH, i, row.AutoExpDate?.ToString("yyyyMMdd") ?? string.Empty);
                oDbDataSource.SetValue(CONSTANTS.TABLES.FIELDS_LINE_DB.MNFDATE_BATCH, i, row.MnfDate?.ToString("yyyyMMdd") ?? string.Empty);
                oDbDataSource.SetValue(CONSTANTS.TABLES.FIELDS_LINE_DB.INDATE_BATCH, i, row.InDate?.ToString("yyyyMMdd") ?? string.Empty);


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


        private string ObtenerDocEntry(SAPbouiCOM.Form oForm)
        {
            SAPbouiCOM.DBDataSource oDBDS = oForm.DataSources.DBDataSources.Item(CONSTANTS.TABLES.TRANSFORM_PRODUCTION_HEAD_WITH_AT);
            string docEntry = oDBDS.GetValue("DocEntry", oDBDS.Offset);
            return docEntry;
        }

        /// <summary>
        /// Lee la cabecera del UDO desde el DBDataSource bound del formulario (incluye los
        /// DocEntry de entrada/salida/reversión). La selección de lotes de la salida no se
        /// persiste en la base: vive solo en memoria.
        /// </summary>
        public TransformProductionUdoModel LeerCabeceraUdo(SAPbouiCOM.Form oForm)
        {
            var oDbDataSource = oForm.DataSources.DBDataSources
                .Item(CONSTANTS.TABLES.TRANSFORM_PRODUCTION_HEAD_WITH_AT);
            return MapearCabeceraUdo(oDbDataSource);
        }

        /// <summary>
        /// Reconstruye el contexto en memoria desde el formulario recién cargado (FORM_DATA_LOAD):
        /// artículo, cantidad, estado y DocEntry persistidos. La salida (lotes del principal)
        /// no se restaura: queda vacía para que el usuario la re-elija, y el resto del estado
        /// sigue reconstruyéndose desde la base (restart-safe).
        /// </summary>
        public void ReconstruirContextoDesdeForm(SAPbouiCOM.Form oForm, TransformProductionContext ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            AplicarCabeceraUdoAContexto(LeerCabeceraUdo(oForm), ctx);
        }

        /// <summary>
        /// Copia al contexto en memoria artículo, cantidad, estado y DocEntry de la cabecera
        /// del UDO (leída del formulario o de la base).
        /// </summary>
        private static void AplicarCabeceraUdoAContexto(TransformProductionUdoModel udo, TransformProductionContext ctx)
        {
            ctx.PrincipalItemCode = udo.ItemCode;
            ctx.PrincipalQuantityConsumed = udo.Quantity;
            ctx.PrincipalStatus = udo.Status;
            ctx.InventoryGenEntriesDocEntry = udo.EntryDocEntry;
            ctx.InventoryGenEntriesRevDocEntry = udo.EntryRevDocEntry;
            ctx.InventoryGenExitsDocEntry = udo.ExitDocEntry;
            ctx.InventoryGenExitsRevDocEntry = udo.ExitRevDocEntry;
        }

        /// <summary>
        /// Vuelve a leer de la base la cabecera y las líneas del registro <paramref name="docEntry"/>
        /// (DBDataSource.Query) y deja el formulario en modo OK, sin cambios pendientes. Se usa
        /// después de actualizar el UDO por DI API (p.ej. al revertir) para que el formulario
        /// refleje el estado y los DocEntry persistidos.
        /// </summary>
        private void RecargarRegistro(SAPbouiCOM.Form oForm, int docEntry)
        {
            if (oForm == null) throw new ArgumentNullException(nameof(oForm));

            Conditions oConditions = null;
            oForm.Freeze(true);
            try
            {
                oConditions = (Conditions)ConnectionSDK.UIAPI.CreateObject(BoCreatableObjectType.cot_Conditions);
                Condition oCondition = oConditions.Add();
                oCondition.Alias = CONSTANTS.TABLES.FIELDS_HEAD_DB.DOCENTRY;
                oCondition.Operation = BoConditionOperation.co_EQUAL;
                oCondition.CondVal = docEntry.ToString(CultureInfo.InvariantCulture);

                oForm.DataSources.DBDataSources.Item(CONSTANTS.TABLES.TRANSFORM_PRODUCTION_HEAD_WITH_AT).Query(oConditions);
                oForm.DataSources.DBDataSources.Item(CONSTANTS.TABLES.TRANSFORM_PRODUCTION_LINE_WITH_AT).Query(oConditions);

                var oMatrix = (Matrix)oForm.Items.Item(CONSTANTS.UID.GRID).Specific;
                oMatrix.LoadFromDataSource();

                oForm.Mode = BoFormMode.fm_OK_MODE;
            }
            finally
            {
                oForm.Freeze(false);
                if (oConditions != null) MarshalGC.LiberarComObject(oConditions);
            }
        }

        /// <summary>
        /// Vacía el caption de la etiqueta de lotes seleccionados de la cabecera. Se usa al
        /// reiniciar el contexto (crear/actualizar/navegar) para que no quede información
        /// visual obsoleta del registro anterior.
        /// </summary>
        private void LimpiarEtiquetaLotes(SAPbouiCOM.Form oForm)
        {
            if (oForm == null) return;

            var oStaticText = oForm.Items.Item(CONSTANTS.UID.HEADER.LOTE_LABEL).Specific as SAPbouiCOM.StaticText;
            if (oStaticText != null)
                oStaticText.Caption = string.Empty;
        }
    }
}