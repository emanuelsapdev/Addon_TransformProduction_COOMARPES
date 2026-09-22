using Addon_TransformProduction.Common;
using Addon_TransformProduction.Models;
using Addon_TransformProduction.Services;
using Addon_TransformProduction.Tools;
using SAPbobsCOM;
using System;
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
                return CrearDocumentoInventario(oDoc, model);
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
                return CrearDocumentoInventario(oDoc, model);
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
        private int CrearDocumentoInventario(Documents oDoc, InventoryGenModel model)
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

                if (!string.IsNullOrWhiteSpace(item.AcctCode))
                    oDoc.Lines.AccountCode = item.AcctCode;

                for (int b = 0; b < item.Batches.Count; b++)
                {
                    var batch = item.Batches[b];

                    if (b > 0) oDoc.Lines.BatchNumbers.Add();

                    oDoc.Lines.BatchNumbers.BatchNumber = batch.BatchNumber;
                    oDoc.Lines.BatchNumbers.Quantity = batch.Quantity;
                    oDoc.Lines.BatchNumbers.ManufacturingDate = batch.MnfDate;
                    oDoc.Lines.BatchNumbers.ExpiryDate = batch.ExpDate;
                    oDoc.Lines.BatchNumbers.AddmisionDate = batch.InDate;
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

        public bool CambiarTransformProductionStatus(int docEntry, string newStatus)
        {
            if (docEntry == 0) throw new ArgumentNullException(nameof(docEntry));
            if (string.IsNullOrWhiteSpace(newStatus)) throw new ArgumentNullException(nameof(newStatus));

            CompanyService oCompanyService = null;
            GeneralService oGeneralService = null;
            GeneralDataParams oParams = null;

            try
            {
                oCompanyService = ConnectionSDK.DIAPI.GetCompanyService();
                oGeneralService = (GeneralService)oCompanyService.GetGeneralService(CONSTANTS.UDO.OBJECT_CODE);
                oParams = oGeneralService.GetDataInterface(GeneralServiceDataInterfaces.gsGeneralDataParams);

                // Traer el registro existente
                oParams.SetProperty("DocEntry", docEntry);

                GeneralData oGeneralData = oGeneralService.GetByParams(oParams);

                // Modificar campos
                oGeneralData.SetProperty(CONSTANTS.TABLES.FIELDS_HEAD_DB.STATUS, newStatus);

                // Guardar
                oGeneralService.Update(oGeneralData);
                
                return true;
            }
            catch { 
                return false;
            }
            finally
            {
                MarshalGC.LiberarComObject(oCompanyService);
                MarshalGC.LiberarComObject(oGeneralService);
                MarshalGC.LiberarComObject(oParams);
            }
        }
    }
}
