using Addon_TransformProduction.Common;
using Addon_TransformProduction.Models;
using Addon_TransformProduction.Services;
using Addon_TransformProduction.Tools;
using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Addon_TransformProduction.Forms.TransformProduction
{
    public partial class TransformProductionFrm
    {
        /// <summary>
        /// Listado de materiales (BOM) activo de un artículo padre.
        /// </summary>
        /// <param name="oRec">Recordset a ejecutar (lo libera el caller en finally).</param>
        /// <param name="itemCode">Código del artículo padre.</param>
        /// <returns>
        /// Recordset con las columnas: "Code" (árbol), "ItemCode", "ItemName", "Quantity",
        /// "Warehouse", "InvntryUom".
        /// </returns>
        public Recordset ObtenerListaMateriales(Recordset oRec, string itemCode)
        {
            if (oRec == null) throw new ArgumentNullException(nameof(oRec));
            if (string.IsNullOrWhiteSpace(itemCode)) return null;

            try
            {
                oRec.DoQuery($@"SELECT 
                                ""Code"", 
                                ""ItemCode"", 
                                ""ItemName"", 
                                ""Quantity"", 
                                ""Warehouse"", 
                                ""InvntryUom"",
                                ""LastPurPrc"",
                                ""LastPurCur"",
                                ""MnfDate"",
                                ""AutoExpDate"",
                                ""InDate"",
                                ""AutoBatchNumber""                                
                                FROM ITPS_VW_TRANSFPROD_LOTES WHERE ""Code"" = '{SqlEscapeHelper.EscapeSql(itemCode)}';");
                return oRec;
            }
            catch
            {
                if (oRec != null) Marshal.ReleaseComObject(oRec);
                throw;
            }
        }

        /// <summary>
        /// Cabecera persistida del UDO (estado y DocEntry de entrada/salida/reversión), leída
        /// de la base y no del formulario, que puede estar desactualizado.
        /// </summary>
        /// <param name="oRec">Recordset a ejecutar (lo libera el caller en finally).</param>
        /// <param name="docEntry">DocEntry del registro UDO.</param>
        /// <returns>
        /// Recordset con las columnas de <see cref="CONSTANTS.TABLES.FIELDS_HEAD_DB"/>
        /// (sin filas si el registro no existe).
        /// </returns>
        public Recordset ObtenerCabeceraUdoPersistida(Recordset oRec, int docEntry)
        {
            if (oRec == null) throw new ArgumentNullException(nameof(oRec));

            try
            {
                oRec.DoQuery($@"SELECT
                                ""{CONSTANTS.TABLES.FIELDS_HEAD_DB.STATUS}"",
                                ""{CONSTANTS.TABLES.FIELDS_HEAD_DB.ITEMCODE}"",
                                ""{CONSTANTS.TABLES.FIELDS_HEAD_DB.QUANTITY}"",
                                ""{CONSTANTS.TABLES.FIELDS_HEAD_DB.ENTRY_DOC_ENTRY}"",
                                ""{CONSTANTS.TABLES.FIELDS_HEAD_DB.EXIT_DOC_ENTRY}"",
                                ""{CONSTANTS.TABLES.FIELDS_HEAD_DB.ENTRY_REV_DOC_ENTRY}"",
                                ""{CONSTANTS.TABLES.FIELDS_HEAD_DB.EXIT_REV_DOC_ENTRY}""
                                FROM ""{CONSTANTS.TABLES.TRANSFORM_PRODUCTION_HEAD_WITH_AT}""
                                WHERE ""{CONSTANTS.TABLES.FIELDS_HEAD_DB.DOCENTRY}"" = {docEntry};");
                return oRec;
            }
            catch
            {
                if (oRec != null) Marshal.ReleaseComObject(oRec);
                throw;
            }
        }

        /// <summary>
        /// Almacenes (OWHS) que existen entre los códigos indicados.
        /// </summary>
        /// <param name="oRec">Recordset a ejecutar (lo libera el caller en finally).</param>
        /// <param name="whsCodes">Códigos de almacén a buscar (no vacío).</param>
        /// <returns>Recordset con la columna "WhsCode" de los almacenes encontrados.</returns>
        public Recordset ObtenerAlmacenesExistentes(Recordset oRec, IEnumerable<string> whsCodes)
        {
            if (oRec == null) throw new ArgumentNullException(nameof(oRec));

            string codigos = string.Join(", ", whsCodes.Select(c => $"'{SqlEscapeHelper.EscapeSql(c)}'"));

            try
            {
                oRec.DoQuery($@"SELECT ""WhsCode"" FROM OWHS WHERE ""WhsCode"" IN ({codigos});");
                return oRec;
            }
            catch
            {
                if (oRec != null) Marshal.ReleaseComObject(oRec);
                throw;
            }
        }

        /// <summary>
        /// Costo unitario (moneda local, IGE1."StockPrice") de cada línea de un Goods Issue.
        /// </summary>
        /// <param name="oRec">Recordset a ejecutar (lo libera el caller en finally).</param>
        /// <param name="exitDocEntry">DocEntry del Goods Issue (OIGE).</param>
        /// <returns>Recordset con las columnas "VisOrder" y "StockPrice", ordenado por VisOrder.</returns>
        public Recordset ObtenerCostosLineasSalida(Recordset oRec, int exitDocEntry)
        {
            if (oRec == null) throw new ArgumentNullException(nameof(oRec));

            try
            {
                oRec.DoQuery($@"SELECT ""VisOrder"", ""StockPrice"" FROM IGE1 WHERE ""DocEntry"" = {exitDocEntry} ORDER BY ""VisOrder"";");
                return oRec;
            }
            catch
            {
                if (oRec != null) Marshal.ReleaseComObject(oRec);
                throw;
            }
        }

        /// <summary>
        /// Crea la entrada de mercancía (Goods Receipt, artículos obtenidos de la transformación)
        /// a partir de <see cref="TransformProductionContext.InventoryGenEntriesData"/>.
        /// </summary>
        /// <param name="model">Líneas/lotes de la entrada a crear.</param>
        /// <param name="baseDocObjectType">
        /// Tipo de documento base a referenciar (documentos relacionados), o null si no aplica.
        /// </param>
        /// <param name="baseDocEntry">DocEntry del documento base a referenciar, si corresponde.</param>
        /// <returns>DocEntry del documento creado.</returns>
        public int CrearEntradaMercancia(InventoryGenModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var oDoc = (Documents)ConnectionSDK.DIAPI.GetBusinessObject(BoObjectTypes.oInventoryGenEntry);
            try
            {
                return CrearDocumentoInventario(oDoc, model, esEntrada: true);
            }
            finally
            {
                MarshalGC.LiberarComObject(oDoc);
            }
        }

        /// <summary>
        /// Crea la salida de mercancía (Goods Issue, artículo principal consumido)
        /// a partir de <see cref="TransformProductionContext.InventoryGenExitsData"/>.
        /// </summary>
        /// <param name="model">Líneas/lotes de la salida a crear.</param>
        /// <param name="baseDocObjectType">
        /// Tipo de documento base a referenciar (documentos relacionados), o null si no aplica.
        /// </param>
        /// <param name="baseDocEntry">DocEntry del documento base a referenciar, si corresponde.</param>
        /// <returns>DocEntry del documento creado.</returns>
        public int CrearSalidaMercancia(InventoryGenModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var oDoc = (Documents)ConnectionSDK.DIAPI.GetBusinessObject(BoObjectTypes.oInventoryGenExit);
            try
            {
                return CrearDocumentoInventario(oDoc, model, esEntrada: false);
            }
            finally
            {
                MarshalGC.LiberarComObject(oDoc);
            }
        }

        /// <summary>
        /// Reconstruye la salida (lotes del artículo principal consumidos) desde el Goods Issue
        /// original (OIGE) indicado por <paramref name="exitDocEntry"/>. Se usa en la reversión
        /// de un documento Completado reabierto, donde la selección de lotes ya no está en
        /// memoria (la salida no se persiste en el UDO).
        /// </summary>
        public InventoryGenModel ObtenerExitDataDesdeDocumento(int exitDocEntry)
        {
            var model = new InventoryGenModel();
            if (exitDocEntry <= 0) return model;

            var oDoc = (Documents)ConnectionSDK.DIAPI.GetBusinessObject(BoObjectTypes.oInventoryGenExit);
            try
            {
                if (!oDoc.GetByKey(exitDocEntry)) return model;

                model.DocDate = oDoc.DocDate;
                model.TaxDate = oDoc.TaxDate;

                for (int i = 0; i < oDoc.Lines.Count; i++)
                {
                    oDoc.Lines.SetCurrentLine(i);

                    // El costo con el que salió cada línea se completa aparte
                    // (ver Service.ObtenerSalidaParaReversion).
                    var item = new InventoryGenModel.Item
                    {
                        ItemCode = oDoc.Lines.ItemCode,
                        Warehouse = oDoc.Lines.WarehouseCode,
                        Quantity = oDoc.Lines.Quantity
                    };

                    for (int b = 0; b < oDoc.Lines.BatchNumbers.Count; b++)
                    {
                        oDoc.Lines.BatchNumbers.SetCurrentLine(b);

                        var batch = new InventoryGenModel.Item.Batch
                        {
                            BatchNumber = oDoc.Lines.BatchNumbers.BatchNumber,
                            Quantity = oDoc.Lines.BatchNumbers.Quantity,
                            MnfDate = oDoc.Lines.BatchNumbers.ManufacturingDate,
                            ExpDate = oDoc.Lines.BatchNumbers.ExpiryDate,
                            InDate = oDoc.Lines.BatchNumbers.AddmisionDate
                        };

                        item.AddBatch(batch);
                    }

                    model.Items.Add(item);
                }

                return model;
            }
            finally
            {
                MarshalGC.LiberarComObject(oDoc);
            }
        }

        /// <summary>
        /// Arma cabecera, líneas y lotes de un documento de entrada/salida de mercancía (DI API)
        /// y lo agrega. Si se indica <paramref name="baseDocObjectType"/>/<paramref name="baseDocEntry"/>,
        /// referencia todas las líneas a la primera línea de ese documento base para que quede
        /// relacionado en "Documentos Referenciados".
        /// </summary>
        private int CrearDocumentoInventario(Documents oDoc, InventoryGenModel model, bool esEntrada)
        {
            oDoc.DocDate = model.DocDate ?? DateTime.Today;
            oDoc.TaxDate = model.TaxDate ?? DateTime.Today;

            for (int i = 0; i < model.Items.Count; i++)
            {
                var item = model.Items[i];

                if (i > 0) oDoc.Lines.Add();

                oDoc.Lines.ItemCode = item.ItemCode;
                oDoc.Lines.WarehouseCode = item.Warehouse;
                oDoc.Lines.Quantity = item.Quantity;

                // En la entrada el precio valúa el stock que ingresa ("Precio (nuevo)"); en la
                // salida SAP valúa al costo, así que no se manda.
                if (esEntrada && item.Price > 0)
                {
                    if (!string.IsNullOrWhiteSpace(item.Currency))
                        oDoc.Lines.Currency = item.Currency;

                    oDoc.Lines.UnitPrice = Convert.ToDouble(item.Price);
                }

                if (!string.IsNullOrWhiteSpace(item.AcctCode))
                    oDoc.Lines.AccountCode = item.AcctCode;

                for (int b = 0; b < item.Batches.Count; b++)
                {
                    var batch = item.Batches[b];

                    if (b > 0) oDoc.Lines.BatchNumbers.Add();

                    oDoc.Lines.BatchNumbers.BatchNumber = batch.BatchNumber;
                    oDoc.Lines.BatchNumbers.Quantity = batch.Quantity;

                    // Las fechas solo aplican al lote que ingresa; en la salida el lote ya existe.
                    // No se mandan fechas vacías (DateTime.MinValue).
                    if (esEntrada)
                    {
                        if (batch.MnfDate != DateTime.MinValue) oDoc.Lines.BatchNumbers.ManufacturingDate = batch.MnfDate;
                        if (batch.ExpDate != DateTime.MinValue) oDoc.Lines.BatchNumbers.ExpiryDate = batch.ExpDate;
                        if (batch.InDate != DateTime.MinValue) oDoc.Lines.BatchNumbers.AddmisionDate = batch.InDate;
                    }
                }
            }

            int ret = oDoc.Add();
            if (ret != 0)
            {
                ConnectionSDK.DIAPI.GetLastError(out int errCode, out string errMsg);
                throw new Exception($"({errCode}) {errMsg}");
            }

            string newKey = ConnectionSDK.DIAPI.GetNewObjectKey();
            return int.Parse(newKey);
        }


        public bool ReferenciarDocs(int docEntry = 0, BoObjectTypes? docObj = null, int baseDocEntry = 0, ReferencedObjectTypeEnum? referencedObject = null)
        {
            if (docEntry == 0) throw new ArgumentNullException(nameof(docEntry));
            if (baseDocEntry == 0) throw new ArgumentNullException(nameof(baseDocEntry));
            if(!docObj.HasValue) throw new ArgumentNullException(nameof(docObj));

            var oDoc = (Documents)ConnectionSDK.DIAPI.GetBusinessObject(docObj.Value);
            try
            {
                oDoc.GetByKey(docEntry);

                if (referencedObject.HasValue)
                {
                    oDoc.DocumentReferences.ReferencedDocEntry = baseDocEntry;
                    oDoc.DocumentReferences.ReferencedObjectType = referencedObject.Value;
                }

                if(oDoc.Update() != 0)
                {
                    ConnectionSDK.DIAPI.GetLastError(out int err, out string msg);
                    NotificationService.MostrarError($"Error: {err} - {msg}");
                    return false;
                }
                return true;
            }
            finally
            {
                MarshalGC.LiberarComObject(oDoc);
            }


        }

        /// <summary>
        /// Actualiza de forma unificada la cabecera del UDO de Producción/Transformación:
        /// estado y/o DocEntry de entrada, salida y reversiones. Solo persiste los campos
        /// provistos (null = no tocar). La selección de lotes de la salida no se persiste.
        /// </summary>
        /// <param name="docEntry">DocEntry del registro UDO a actualizar (mayor que cero).</param>
        /// <param name="status">Nuevo estado, o null para no modificarlo.</param>
        public bool ActualizarResultadoTransformacion(
            int docEntry,
            string status,
            int? entryDocEntry = null,
            int? exitDocEntry = null,
            int? entryRevDocEntry = null,
            int? exitRevDocEntry = null)
        {
            if (docEntry == 0) throw new ArgumentNullException(nameof(docEntry));

            CompanyService oCompanyService = null;
            GeneralService oGeneralService = null;
            GeneralDataParams oParams = null;
            GeneralData oGeneralData = null;

            try
            {
                oCompanyService = ConnectionSDK.DIAPI.GetCompanyService();
                oGeneralService = (GeneralService)oCompanyService.GetGeneralService(CONSTANTS.UDO.OBJECT_CODE);
                oParams = oGeneralService.GetDataInterface(GeneralServiceDataInterfaces.gsGeneralDataParams);
                oParams.SetProperty("DocEntry", docEntry);

                oGeneralData = oGeneralService.GetByParams(oParams);

                if (status != null)
                    oGeneralData.SetProperty(CONSTANTS.TABLES.FIELDS_HEAD_DB.STATUS, status);
                if (entryDocEntry.HasValue)
                    oGeneralData.SetProperty(CONSTANTS.TABLES.FIELDS_HEAD_DB.ENTRY_DOC_ENTRY, entryDocEntry.Value);
                if (exitDocEntry.HasValue)
                    oGeneralData.SetProperty(CONSTANTS.TABLES.FIELDS_HEAD_DB.EXIT_DOC_ENTRY, exitDocEntry.Value);
                if (entryRevDocEntry.HasValue)
                    oGeneralData.SetProperty(CONSTANTS.TABLES.FIELDS_HEAD_DB.ENTRY_REV_DOC_ENTRY, entryRevDocEntry.Value);
                if (exitRevDocEntry.HasValue)
                    oGeneralData.SetProperty(CONSTANTS.TABLES.FIELDS_HEAD_DB.EXIT_REV_DOC_ENTRY, exitRevDocEntry.Value);

                oGeneralService.Update(oGeneralData);
                return true;
            }
            catch (Exception ex)
            {
                NotificationService.MostrarError($"Error actualizando el UDO {docEntry}: {ex.Message}");
                return false;
            }
            finally
            {
                MarshalGC.LiberarComObject(oCompanyService);
                MarshalGC.LiberarComObject(oGeneralService);
                MarshalGC.LiberarComObject(oParams);
                MarshalGC.LiberarComObject(oGeneralData);
            }
        }
    }
}
