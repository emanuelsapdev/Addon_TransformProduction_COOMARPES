using Addon_TransformProduction.Common;
using Addon_TransformProduction.Models;
using Addon_TransformProduction.Services;
using SAPbouiCOM;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

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

            if(ctx.InventoryGenExitsData.Items.SelectMany(item => item.Batches.Select(batch => batch.Quantity)).Sum() != cantidad)
            {
                NotificationService.MostrarError("La cantidad total asignada no coincide con la Cantidad Consumida.");
                return false;
            }

            ctx.PrincipalItemCode = itemCode;
            ctx.PrincipalQuantityConsumed = cantidad;
            return true;
        }


        /// <summary>
        /// Valida las líneas de detalle antes de grabar el UDO (Pendiente o Crear). Tiene que haber
        /// al menos una línea con subproducto, y cada una debe tener: número de lote, cantidad
        /// obtenida mayor que cero, almacén existente en SAP, precio nuevo mayor que cero, moneda
        /// y fechas de vencimiento, fabricación e ingreso del lote. Las filas sin subproducto
        /// (p.ej. la fila vacía final de la matriz) se ignoran. Corta en el primer error.
        /// </summary>
        public bool ValidarLineasDetalle(SAPbouiCOM.Form oForm)
        {
            var oMatrix = (SAPbouiCOM.Matrix)oForm.Items.Item(CONSTANTS.UID.GRID).Specific;
            oMatrix.FlushToDataSource();

            var oDbDataSource = oForm.DataSources.DBDataSources.Item(CONSTANTS.TABLES.TRANSFORM_PRODUCTION_LINE_WITH_AT);

            // Línea (1-based) y subproducto por almacén, para informar el error de existencia.
            var almacenesPorLinea = new List<Tuple<int, string, string>>();

            for (int i = 0; i < oDbDataSource.Size; i++)
            {
                string itemCode = LeerValorLinea(oDbDataSource, CONSTANTS.TABLES.FIELDS_LINE_DB.SUBPRODUCT, i);
                if (string.IsNullOrWhiteSpace(itemCode)) continue;

                int linea = i + 1;

                if (string.IsNullOrWhiteSpace(LeerValorLinea(oDbDataSource, CONSTANTS.TABLES.FIELDS_LINE_DB.BATCH_NUM, i)))
                    return ErrorLinea(CONSTANTS.MESSAGES.CREATE_LINE_WITHOUT_BATCH, linea, itemCode);

                if (ParseDouble(LeerValorLinea(oDbDataSource, CONSTANTS.TABLES.FIELDS_LINE_DB.QUANTITY_OBTAINED, i)) <= 0)
                    return ErrorLinea(CONSTANTS.MESSAGES.CREATE_LINE_INVALID_QTY, linea, itemCode);

                string whsCode = LeerValorLinea(oDbDataSource, CONSTANTS.TABLES.FIELDS_LINE_DB.WAREHOUSE, i);
                if (string.IsNullOrWhiteSpace(whsCode))
                    return ErrorLinea(CONSTANTS.MESSAGES.CREATE_LINE_WITHOUT_WHS, linea, itemCode);

                if (ParseDecimal(LeerValorLinea(oDbDataSource, CONSTANTS.TABLES.FIELDS_LINE_DB.PRICE, i)) <= 0)
                    return ErrorLinea(CONSTANTS.MESSAGES.CREATE_LINE_INVALID_PRICE, linea, itemCode);

                if (string.IsNullOrWhiteSpace(LeerValorLinea(oDbDataSource, CONSTANTS.TABLES.FIELDS_LINE_DB.CURRENT, i)))
                    return ErrorLinea(CONSTANTS.MESSAGES.CREATE_LINE_WITHOUT_CURRENCY, linea, itemCode);

                if (!EsFechaCargada(LeerValorLinea(oDbDataSource, CONSTANTS.TABLES.FIELDS_LINE_DB.EXTDATE_BATCH, i)))
                    return ErrorLinea(CONSTANTS.MESSAGES.CREATE_LINE_WITHOUT_DATE, linea, itemCode, "fecha de vencimiento del lote");

                if (!EsFechaCargada(LeerValorLinea(oDbDataSource, CONSTANTS.TABLES.FIELDS_LINE_DB.MNFDATE_BATCH, i)))
                    return ErrorLinea(CONSTANTS.MESSAGES.CREATE_LINE_WITHOUT_DATE, linea, itemCode, "fecha de fabricación del lote");

                if (!EsFechaCargada(LeerValorLinea(oDbDataSource, CONSTANTS.TABLES.FIELDS_LINE_DB.INDATE_BATCH, i)))
                    return ErrorLinea(CONSTANTS.MESSAGES.CREATE_LINE_WITHOUT_DATE, linea, itemCode, "fecha de ingreso del lote");

                almacenesPorLinea.Add(Tuple.Create(linea, itemCode, whsCode));
            }

            if (almacenesPorLinea.Count == 0)
            {
                NotificationService.MostrarError(CONSTANTS.MESSAGES.CREATE_NO_DETAIL_LINES);
                return false;
            }

            var codigos = almacenesPorLinea.Select(l => l.Item3).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var existentes = ObtenerAlmacenesExistentes(codigos);

            var sinAlmacen = almacenesPorLinea.FirstOrDefault(l => !existentes.Contains(l.Item3));
            if (sinAlmacen != null)
                return ErrorLinea(CONSTANTS.MESSAGES.CREATE_LINE_WHS_NOT_FOUND, sinAlmacen.Item1, sinAlmacen.Item2, sinAlmacen.Item3);

            return true;
        }

        private static string LeerValorLinea(SAPbouiCOM.DBDataSource oDbDataSource, string campo, int fila)
        {
            return (oDbDataSource.GetValue(campo, fila) ?? string.Empty).Trim();
        }

        private static bool ErrorLinea(string formato, params object[] args)
        {
            NotificationService.MostrarError(string.Format(formato, args));
            return false;
        }

        /// <summary>
        /// El DBDataSource devuelve las fechas como "yyyyMMdd" (vacío si no hay fecha).
        /// </summary>
        private static bool EsFechaCargada(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;

            return DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
                || DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out _);
        }

        /// <summary>
        /// 
        /// </summary>
        public InventoryGenModel ObtenerInfoLineas(SAPbouiCOM.Form oForm)
        {
            var invGen = new InventoryGenModel();
            var oDbDataSource = oForm.DataSources.DBDataSources.Item(CONSTANTS.TABLES.TRANSFORM_PRODUCTION_LINE_WITH_AT);

            if (oDbDataSource == null) return invGen;

            var itemsIndex = new Dictionary<string, InventoryGenModel.Item>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < oDbDataSource.Size; i++)
            {
                string itemCode = (oDbDataSource.GetValue(CONSTANTS.TABLES.FIELDS_LINE_DB.SUBPRODUCT, i) ?? string.Empty).Trim();
                string batchNum = (oDbDataSource.GetValue(CONSTANTS.TABLES.FIELDS_LINE_DB.BATCH_NUM, i) ?? string.Empty).Trim();
                string whs = (oDbDataSource.GetValue(CONSTANTS.TABLES.FIELDS_LINE_DB.WAREHOUSE, i) ?? string.Empty).Trim();
                string uom = (oDbDataSource.GetValue(CONSTANTS.TABLES.FIELDS_LINE_DB.UNIT_MEASUREMENT, i) ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(itemCode) || string.IsNullOrWhiteSpace(batchNum))
                    continue;

                double qty = ParseDouble(oDbDataSource.GetValue(CONSTANTS.TABLES.FIELDS_LINE_DB.QUANTITY_OBTAINED, i));
                if (qty <= 0) continue;

                decimal price = ParseDecimal(oDbDataSource.GetValue(CONSTANTS.TABLES.FIELDS_LINE_DB.PRICE, i));

                DateTime expDate = ParseDateOrToday(oDbDataSource.GetValue(CONSTANTS.TABLES.FIELDS_LINE_DB.EXTDATE_BATCH, i));
                DateTime inDate = ParseDateOrToday(oDbDataSource.GetValue(CONSTANTS.TABLES.FIELDS_LINE_DB.INDATE_BATCH, i));
                DateTime mnfDate = ParseDateOrToday(oDbDataSource.GetValue(CONSTANTS.TABLES.FIELDS_LINE_DB.MNFDATE_BATCH, i));

                string itemKey = string.Concat(itemCode, "|", whs, "|", price.ToString(CultureInfo.InvariantCulture));
                if (!itemsIndex.TryGetValue(itemKey, out var item))
                {
                    item = new InventoryGenModel.Item
                    {
                        ItemCode = itemCode,
                        Warehouse = whs,
                        UnitMeasurement = uom,
                        Price = price,
                        Quantity = 0d
                    };

                    itemsIndex.Add(itemKey, item);
                    invGen.Items.Add(item);
                }

                var existingBatch = item.Batches.FirstOrDefault(b => string.Equals(b.BatchNumber, batchNum, StringComparison.OrdinalIgnoreCase));
                if (existingBatch != null)
                {
                    existingBatch.Quantity += qty;
                }
                else
                {
                    var batch = new InventoryGenModel.Item.Batch
                    {
                        BatchNumber = batchNum,
                        Quantity = qty,
                        ExpDate = expDate,
                        InDate = inDate,
                        MnfDate = mnfDate
                    };

                    item.AddBatch(batch);
                }

                item.Quantity += qty;
            }

            return invGen;
        }

        private static double ParseDouble(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0d;
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out var result)) return result;
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result)) return result;
            return 0d;
        }

        private static decimal ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0m;
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out var result)) return result;
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result)) return result;
            return 0m;
        }

        private static DateTime ParseDateOrToday(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return DateTime.Today;

            if (DateTime.TryParseExact(value.Trim(), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt;

            if (DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out dt))
                return dt;

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                return dt;

            return DateTime.Today;
        }

        public static void FormularioEnCualquierEstado(SAPbouiCOM.Form oForm)
        {
            SAPbouiCOM.Item oItemItemCode = oForm.Items.Item(CONSTANTS.UID.HEADER.ITEM_CODE);
            oItemItemCode.Enabled = false;
        }

        public static void FormularioEnEstadoCompletado(SAPbouiCOM.Form oForm) 
        {
            SAPbouiCOM.Item oItemBtnBatch = oForm.Items.Item(CONSTANTS.UID.BUTTONS.LOTE_SELECT);
            oItemBtnBatch.Enabled = false;

            SAPbouiCOM.Item oItemBtnConfirProd = oForm.Items.Item(CONSTANTS.UID.BUTTONS.CONFIRM_PROD);
            oItemBtnConfirProd.Enabled = false;

            SAPbouiCOM.Item oItemBtnShowDocuments = oForm.Items.Item(CONSTANTS.UID.BUTTONS.SHOW_DOCUMENTS);
            oItemBtnShowDocuments.Enabled = true;

            SAPbouiCOM.Item oItemBtnRevert = oForm.Items.Item(CONSTANTS.UID.BUTTONS.REVERT);
            oItemBtnRevert.Enabled = true;

            SAPbouiCOM.Item oItemQuantity = oForm.Items.Item(CONSTANTS.UID.HEADER.QUANTITY);
            oItemQuantity.Enabled = false;
        }

        public static void FormularioEnEstadoPendiente(SAPbouiCOM.Form oForm) 
        {
            SAPbouiCOM.Item oItemBtnBatch = oForm.Items.Item(CONSTANTS.UID.BUTTONS.LOTE_SELECT);
            oItemBtnBatch.Enabled = true;

            SAPbouiCOM.Item oItemBtnConfirProd = oForm.Items.Item(CONSTANTS.UID.BUTTONS.CONFIRM_PROD);
            oItemBtnConfirProd.Enabled = true;

            SAPbouiCOM.Item oItemBtnShowDocuments = oForm.Items.Item(CONSTANTS.UID.BUTTONS.SHOW_DOCUMENTS);
            oItemBtnShowDocuments.Enabled = false;

            SAPbouiCOM.Item oItemBtnRevert = oForm.Items.Item(CONSTANTS.UID.BUTTONS.REVERT);
            oItemBtnRevert.Enabled = false;

            SAPbouiCOM.Item oItemQuantity = oForm.Items.Item(CONSTANTS.UID.HEADER.QUANTITY);
            oItemQuantity.Enabled = true;
        }

        public static void FormularioEnEstadoRevertido(SAPbouiCOM.Form oForm) 
        {
            SAPbouiCOM.Item oItemBtnBatch = oForm.Items.Item(CONSTANTS.UID.BUTTONS.LOTE_SELECT);
            oItemBtnBatch.Enabled = false;

            SAPbouiCOM.Item oItemBtnConfirProd = oForm.Items.Item(CONSTANTS.UID.BUTTONS.CONFIRM_PROD);
            oItemBtnConfirProd.Enabled = false;

            SAPbouiCOM.Item oItemBtnRevert = oForm.Items.Item(CONSTANTS.UID.BUTTONS.REVERT);
            oItemBtnRevert.Enabled = false;

            SAPbouiCOM.Item oItemQuantity = oForm.Items.Item(CONSTANTS.UID.HEADER.QUANTITY);
            oItemQuantity.Enabled = false;
        }

        /// <summary>
        /// Aplica la habilitación de campos y botones que corresponde al estado del registro.
        /// </summary>
        public static void AplicarHabilitacionPorEstado(SAPbouiCOM.Form oForm, string status)
        {
            FormularioEnCualquierEstado(oForm);

            switch (status)
            {
                case CONSTANTS.STAGING_STATUS.COMPLETED:
                    FormularioEnEstadoCompletado(oForm);
                    break;
                case CONSTANTS.STAGING_STATUS.PENDING:
                    FormularioEnEstadoPendiente(oForm);
                    break;
                case CONSTANTS.STAGING_STATUS.REVERT:
                    FormularioEnEstadoRevertido(oForm);
                    break;
                default:
                    break;
            }
        }

        public static void HabilitarBotonRevertir(SAPbouiCOM.Form oForm, bool enabled)
        {
            SAPbouiCOM.Item oItemBtnRevert = oForm.Items.Item(CONSTANTS.UID.BUTTONS.REVERT);
            oItemBtnRevert.Enabled = enabled;
        }

        /// <summary>
        /// Una producción/transformación se puede revertir una sola vez: el registro tiene que
        /// existir en la base, estar Completado y no tener documentos de reversión. Se valida
        /// contra la cabecera persistida (no contra el formulario, que puede estar desactualizado).
        /// </summary>
        public static bool PuedeRevertirse(TransformProductionUdoModel udo, out string motivo)
        {
            motivo = null;

            if (udo == null)
            {
                motivo = CONSTANTS.MESSAGES.REVERT_NOT_SAVED;
                return false;
            }

            if (udo.EntryRevDocEntry > 0 || udo.ExitRevDocEntry > 0
                || udo.Status == CONSTANTS.STAGING_STATUS.REVERT)
            {
                motivo = CONSTANTS.MESSAGES.REVERT_ALREADY_DONE;
                return false;
            }

            if (udo.Status != CONSTANTS.STAGING_STATUS.COMPLETED)
            {
                motivo = CONSTANTS.MESSAGES.REVERT_NOT_COMPLETED + udo.Status;
                return false;
            }

            return true;
        }

        /// <summary>
        /// DocEntry del registro recién agregado, desde el ObjectKey del FormDataEvent
        /// (p.ej. "&lt;DocumentParams&gt;&lt;DocEntry&gt;12&lt;/DocEntry&gt;&lt;/DocumentParams&gt;"). Devuelve 0 si no se
        /// puede leer. Se usa en vez del formulario porque tras agregar queda en un documento nuevo.
        /// </summary>
        public static int ObtenerDocEntryAgregado(string objectKey)
        {
            if (string.IsNullOrWhiteSpace(objectKey)) return 0;

            var match = Regex.Match(objectKey, @"<DocEntry>\s*(\d+)\s*</DocEntry>", RegexOptions.IgnoreCase);
            return match.Success && int.TryParse(match.Groups[1].Value, out int docEntry) ? docEntry : 0;
        }

        public static void AbrirDocumentosRelacionados(TransformProductionContext ctx)
        {
            if (ctx == null) return;

            if (ctx.InventoryGenExitsDocEntry > 0)
                ConnectionSDK.UIAPI.OpenForm(BoFormObjectEnum.fo_GoodsIssue, null, ctx.InventoryGenExitsDocEntry.ToString());

            if (ctx.InventoryGenEntriesDocEntry > 0)
                ConnectionSDK.UIAPI.OpenForm(BoFormObjectEnum.fo_GoodsReceipt, null, ctx.InventoryGenEntriesDocEntry.ToString());

            if (ctx.InventoryGenExitsDocEntry > 0)
                ConnectionSDK.UIAPI.OpenForm(BoFormObjectEnum.fo_GoodsIssue, null, ctx.InventoryGenExitsDocEntry.ToString());

            if (ctx.InventoryGenEntriesDocEntry > 0)
                ConnectionSDK.UIAPI.OpenForm(BoFormObjectEnum.fo_GoodsReceipt, null, ctx.InventoryGenEntriesDocEntry.ToString());
        }
    }
}