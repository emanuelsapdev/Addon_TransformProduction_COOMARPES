# Plan: Persistencia del contexto y DocEntry de Entrada/Salida/Reversión

**Proyecto:** Addon COOMARPES — Producción / Transformación (`ITPS_TRANSFPROD`)
**Fecha:** 2026-09-22
**Stack:** .NET Framework 4.7.2 / C# 7.3 / SAP B1 v10 (UI API + DI API, HANA)

---

## 1. Problema

Los datos del contexto (`InventoryGenExitsData`, `InventoryGenEntriesData` y los
`DocEntry` de entrada/salida) viven **solo en memoria** dentro de
`TransformProductionContext` (diccionario estático `ContextManager`).

Eso provoca que:

1. Al **reabrir** un documento **Pendiente** (`PENDING`), el contexto se recrea vacío:
   - La salida (qué lotes del artículo principal se consumieron) **se pierde** porque
     nunca se persistió.
   - `ValidarFormulario` falla o no valida correctamente al no tener
     `ctx.InventoryGenExitsData`.
2. Al querer **crear los documentos definitivos** (entrada/salida) desde un documento
   Pendiente (botón `CONFIRM_PROD`), no hay datos de dónde reconstruir la salida.
3. Los **`DocEntry`** de entrada/salida solo se usan de forma efímera en
   `AbrirDocumentosRelacionados`; al reabrir, los botones `SHOW_DOCUMENTS` y
   `REVERT` no tienen de dónde leerlos.
4. Los botones `CONFIRM_PROD` (`Item_2`), `SHOW_DOCUMENTS` (`Item_5`) y `REVERT`
   (`Item_3`) existen en la UI y se habilitan/deshabilitan por estado, pero **no
   tienen handler** en `OnItemEvent`.

---

## 2. Estado actual del flujo de contexto

| Dato | Dónde se setea hoy | Dónde persiste | Reconstruible? |
|---|---|---|---|
| `PrincipalItemCode` | `ManejarCodigoArticuloPerdidaFoco` (Service) | Cabecera `U_ITPS_ItemCode` | ✅ Sí |
| `PrincipalQuantityConsumed` | `ManejarCantidadConsumidaPerdidaFoco` (Service) | Cabecera `U_ITPS_Qty` | ✅ Sí |
| Lotes del **principal** (salida) | `PersistirSeleccionLotes` → `ctx.InventoryGenExitsData` + `ctx.BatchHeadXml` | **NADA** | ❌ No |
| Subproductos (entrada) | `ManejarCreacionProduccion` → `ctx.InventoryGenEntriesData = ObtenerInfoLineas(...)` | Líneas UDO `@ITPS_TRANSFPROD_LIN` | ✅ Sí (regenerable con `ObtenerInfoLineas`) |
| `DocEntry` entrada/salida | `ManejarCreacionProduccion` → `ctx.InventoryGenExitsDocEntry/EntriesDocEntry` | **NADA** (solo memoria) | ❌ No |
| Estado | `CambiarTransformProductionStatus` (Repository) | Cabecera `U_ITPS_Status` | ✅ Sí |

Referencias clave del código actual:

- `Models/TransformProductionContext.cs` — contexto en memoria (item, qty, modelo de
  salida/entrada, DocEntry, referencias a forms).
- `ContextManager.cs` — diccionario estático `TypeCount → contexto`.
- `TransformProductionFrm.Handlers.cs` — `ManejarCreacionProduccion` arma la entrada
  desde el grid y llama a `CrearProduccion`.
- `TransformProductionFrm.Service.cs` — `CrearProduccion` crea IGN + IGO en transacción
  y las referencia.
- `TransformProductionFrm.Repository.cs` — `CrearEntradaMercancia`, `CrearSalidaMercancia`,
  `CrearDocumentoInventario`, `ReferenciarDocs`, `CambiarTransformProductionStatus`.
- `TransformProductionFrm.Functionality.cs` — `ObtenerInfoLineas` (grid → modelo de
  entrada), `AbrirDocumentosRelacionados`, `ValidarFormulario`, estados de la UI.
- `WindowBatchesFrm.Functionality.cs` — `ObtenerLotesSeleccionados` (DataTable → modelo
  de salida), `ValidarCantidadesAsignadas`.
- `WindowBatchesFrm.Service.cs` / `FormReader.cs` — persiste en memoria
  `ctx.BatchHeadXml` (XML del DataTable) y actualiza la etiqueta de la cabecera.

---

## 3. Diseño propuesto

### 3.1. Decisiones tomadas

1. **Los documentos definitivos los crea el addon** (flujo actual `CrearProduccion`),
   no un servicio externo.
2. La **entrada** se **reconstruye siempre desde las líneas del UDO**
   (`ObtenerInfoLineas`). Es la fuente única de verdad → no se persiste aparte
   (evita drift si el usuario edita cantidades/precios).
3. La **salida** (lotes del principal consumidos) **se persiste como DTO serializado**
   con `XmlSerializer` (sin dependencias NuGet nuevas).
4. **Reversión cruzada** (semántica del negocio):
   - Revertir la **entrada (IGN de subproductos)** ⇒ se genera una **salida (IGO)**.
   - Revertir la **salida (IGO del principal)** ⇒ se genera una **entrada (IGN)**.
5. Los 4 `DocEntry` se persisten en la cabecera del UDO como campos numéricos
   (restart-safe y consistente con el patrón staging).

### 3.2. Nuevos campos UDF en la cabecera `@ITPS_TRANSFPROD_CAB`

| UDF | Tipo | Descripción |
|---|---|---|
| `U_ITPS_EntryDocEntry` | `db_Numeric` (size 11) | DocEntry de la Entrada (Goods Receipt / IGN) creada |
| `U_ITPS_ExitDocEntry` | `db_Numeric` (size 11) | DocEntry de la Salida (Goods Issue / IGO) creada |
| `U_ITPS_EntryRevDocEntry` | `db_Numeric` (size 11) | DocEntry de la Reversión de la Entrada (IMO → IGO) |
| `U_ITPS_ExitRevDocEntry` | `db_Numeric` (size 11) | DocEntry de la Reversión de la Salida (IMO → IGN) |
| `U_ITPS_ExitBatchesXml` | `db_Memo` | XML del `InventoryGenExitsData` (lotes del principal) |

**Semántica de reversión** (mapeo de documentos):

| Campo UDF | Documento original | Reversión = documento nuevo |
|---|---|---|
| `U_ITPS_EntryDocEntry` | Entrada **IGN** (subproductos producidos) | `U_ITPS_EntryRevDocEntry` → **IGO** (salida que retira los subproductos) |
| `U_ITPS_ExitDocEntry` | Salida **IGO** (principal consumido) | `U_ITPS_ExitRevDocEntry` → **IGN** (entrada que devuelve el principal) |

> En la UI del UDO (B1 Studio) estos campos deben quedar **read-only / ocultos**:
> son gestionados por el addon, no por el usuario.

---

## 4. Implementación (archivo por archivo)

### 4.1. `Configuration/InfraConfig.cs`

En `TransformProductionInfra.CrearTablaCabecera()` agregar (idempotente, vía
`InfraDataService.CrearCampoUsuario`):

```csharp
InfraDataService.CrearCampoUsuario(
    tableName: CONSTANTS.TABLES.TRANSFORM_PRODUCTION_HEAD_WITH_AT,
    fieldName: CONSTANTS.TABLES.FIELDS_HEAD.ENTRY_DOC_ENTRY,
    desc:      CONSTANTS.TABLES.FIELDS_HEAD_DESC.ENTRY_DOC_ENTRY,
    type: BoFieldTypes.db_Numeric, size: 11);

InfraDataService.CrearCampoUsuario(
    tableName: CONSTANTS.TABLES.TRANSFORM_PRODUCTION_HEAD_WITH_AT,
    fieldName: CONSTANTS.TABLES.FIELDS_HEAD.EXIT_DOC_ENTRY,
    desc:      CONSTANTS.TABLES.FIELDS_HEAD_DESC.EXIT_DOC_ENTRY,
    type: BoFieldTypes.db_Numeric, size: 11);

InfraDataService.CrearCampoUsuario(
    tableName: CONSTANTS.TABLES.TRANSFORM_PRODUCTION_HEAD_WITH_AT,
    fieldName: CONSTANTS.TABLES.FIELDS_HEAD.ENTRY_REV_DOC_ENTRY,
    desc:      CONSTANTS.TABLES.FIELDS_HEAD_DESC.ENTRY_REV_DOC_ENTRY,
    type: BoFieldTypes.db_Numeric, size: 11);

InfraDataService.CrearCampoUsuario(
    tableName: CONSTANTS.TABLES.TRANSFORM_PRODUCTION_HEAD_WITH_AT,
    fieldName: CONSTANTS.TABLES.FIELDS_HEAD.EXIT_REV_DOC_ENTRY,
    desc:      CONSTANTS.TABLES.FIELDS_HEAD_DESC.EXIT_REV_DOC_ENTRY,
    type: BoFieldTypes.db_Numeric, size: 11);

InfraDataService.CrearCampoUsuario(
    tableName: CONSTANTS.TABLES.TRANSFORM_PRODUCTION_HEAD_WITH_AT,
    fieldName: CONSTANTS.TABLES.FIELDS_HEAD.EXIT_BATCHES_XML,
    desc:      CONSTANTS.TABLES.FIELDS_HEAD_DESC.EXIT_BATCHES_XML,
    type: BoFieldTypes.db_Memo);
```

### 4.2. `Forms/TransformProduction/TransformProductionFrm.Constants.cs`

Agregar a `FIELDS_HEAD`:

```csharp
public const string ENTRY_DOC_ENTRY    = "ITPS_EntryDocEntry";
public const string EXIT_DOC_ENTRY     = "ITPS_ExitDocEntry";
public const string ENTRY_REV_DOC_ENTRY = "ITPS_EntryRevDocEntry";
public const string EXIT_REV_DOC_ENTRY  = "ITPS_ExitRevDocEntry";
public const string EXIT_BATCHES_XML    = "ITPS_ExitBatchesXml";
```

Agregar a `FIELDS_HEAD_DB` (con prefijo `U_`):

```csharp
public const string ENTRY_DOC_ENTRY     = "U_" + FIELDS_HEAD.ENTRY_DOC_ENTRY;
public const string EXIT_DOC_ENTRY      = "U_" + FIELDS_HEAD.EXIT_DOC_ENTRY;
public const string ENTRY_REV_DOC_ENTRY = "U_" + FIELDS_HEAD.ENTRY_REV_DOC_ENTRY;
public const string EXIT_REV_DOC_ENTRY  = "U_" + FIELDS_HEAD.EXIT_REV_DOC_ENTRY;
public const string EXIT_BATCHES_XML    = "U_" + FIELDS_HEAD.EXIT_BATCHES_XML;
```

Agregar a `FIELDS_HEAD_DESC`:

```csharp
public const string ENTRY_DOC_ENTRY      = "Entrada DocEntry";
public const string EXIT_DOC_ENTRY       = "Salida DocEntry";
public const string ENTRY_REV_DOC_ENTRY  = "Reversión Entrada DocEntry";
public const string EXIT_REV_DOC_ENTRY   = "Reversión Salida DocEntry";
public const string EXIT_BATCHES_XML     = "Lotes de Salida (contexto)";
```

### 4.3. Nuevo Modelo: `Models/TransformProductionUdoModel.cs`

DTO de la cabecera del UDO (hidrata el contexto al reabrir):

```csharp
namespace Addon_TransformProduction.Models
{
    public class TransformProductionUdoModel
    {
        public string Status { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
        public double Quantity { get; set; }
        public int EntryDocEntry { get; set; }
        public int ExitDocEntry { get; set; }
        public int EntryRevDocEntry { get; set; }
        public int ExitRevDocEntry { get; set; }
        public string ExitBatchesXml { get; set; } = string.Empty;
    }
}
```

### 4.4. `Forms/TransformProduction/TransformProductionFrm.Mapper.cs`

Agregar (XML DTO de la salida + mapeo de cabecera):

```csharp
public string SerializarExitData(InventoryGenModel model)
{
    if (model == null || model.Items.Count == 0) return string.Empty;
    var serializer = new XmlSerializer(typeof(InventoryGenModel));
    using (var writer = new StringWriter())
    {
        serializer.Serialize(writer, model);
        return writer.ToString();
    }
}

public InventoryGenModel DeserializarExitData(string xml)
{
    if (string.IsNullOrWhiteSpace(xml)) return new InventoryGenModel();
    try
    {
        var serializer = new XmlSerializer(typeof(InventoryGenModel));
        using (var reader = new StringReader(xml))
            return (InventoryGenModel)serializer.Deserialize(reader);
    }
    catch
    {
        return new InventoryGenModel();
    }
}

public TransformProductionUdoModel MapearCabeceraUdo(SAPbouiCOM.DBDataSource oDbDataSource, int row = -1)
{
    int r = row < 0 ? oDbDataSource.Offset : row;
    return new TransformProductionUdoModel
    {
        Status          = oDbDataSource.GetValue(CONSTANTS.TABLES.FIELDS_HEAD_DB.STATUS, r) ?? string.Empty,
        ItemCode        = oDbDataSource.GetValue(CONSTANTS.TABLES.FIELDS_HEAD_DB.ITEMCODE, r) ?? string.Empty,
        Quantity        = ParseDouble(oDbDataSource.GetValue(CONSTANTS.TABLES.FIELDS_HEAD_DB.QUANTITY, r)),
        EntryDocEntry   = ParseInt(oDbDataSource.GetValue(CONSTANTS.TABLES.FIELDS_HEAD_DB.ENTRY_DOC_ENTRY, r)),
        ExitDocEntry    = ParseInt(oDbDataSource.GetValue(CONSTANTS.TABLES.FIELDS_HEAD_DB.EXIT_DOC_ENTRY, r)),
        EntryRevDocEntry= ParseInt(oDbDataSource.GetValue(CONSTANTS.TABLES.FIELDS_HEAD_DB.ENTRY_REV_DOC_ENTRY, r)),
        ExitRevDocEntry = ParseInt(oDbDataSource.GetValue(CONSTANTS.TABLES.FIELDS_HEAD_DB.EXIT_REV_DOC_ENTRY, r)),
        ExitBatchesXml  = oDbDataSource.GetValue(CONSTANTS.TABLES.FIELDS_HEAD_DB.EXIT_BATCHES_XML, r) ?? string.Empty
    };
}
```

> `InventoryGenModel` (y sus clases anidadas `Item`/`Batch`) son POCO públicas con
> constructor público → compatibles con `XmlSerializer`.

### 4.5. `Forms/TransformProduction/TransformProductionFrm.FormReader.cs`

```csharp
public TransformProductionUdoModel LeerCabeceraUdo(SAPbouiCOM.Form oForm)
{
    var oDbDataSource = oForm.DataSources.DBDataSources
        .Item(CONSTANTS.TABLES.TRANSFORM_PRODUCTION_HEAD_WITH_AT);
    return MapearCabeceraUdo(oDbDataSource);
}

public void ReconstruirContextoDesdeForm(SAPbouiCOM.Form oForm, TransformProductionContext ctx)
{
    var udo = LeerCabeceraUdo(oForm);
    ctx.PrincipalItemCode           = udo.ItemCode;
    ctx.PrincipalQuantityConsumed   = udo.Quantity;
    ctx.PrincipalStatus             = udo.Status;
    ctx.InventoryGenEntriesDocEntry = udo.EntryDocEntry;
    ctx.InventoryGenExitsDocEntry   = udo.ExitDocEntry;
    ctx.InventoryGenExitsData       = DeserializarExitData(udo.ExitBatchesXml);
}
```

### 4.6. `Forms/TransformProduction/TransformProductionFrm.Repository.cs`

Reemplazar `CambiarTransformProductionStatus` por un update unificado (mismo patrón
`CompanyService/GeneralService`, COM release en `finally`, **sin** `catch` mudo):

```csharp
public bool ActualizarResultadoTransformacion(
    int docEntry,
    string status,
    int? entryDocEntry = null,
    int? exitDocEntry = null,
    int? entryRevDocEntry = null,
    int? exitRevDocEntry = null,
    string exitBatchesXml = null)
{
    if (docEntry == 0) throw new ArgumentNullException(nameof(docEntry));

    CompanyService oCompanyService = null;
    GeneralService oGeneralService = null;
    GeneralDataParams oParams = null;

    try
    {
        oCompanyService = ConnectionSDK.DIAPI.GetCompanyService();
        oGeneralService = (GeneralService)oCompanyService.GetGeneralService(CONSTANTS.UDO.OBJECT_CODE);
        oParams = oGeneralService.GetDataInterface(GeneralServiceDataInterfaces.gsGeneralDataParams);
        oParams.SetProperty("DocEntry", docEntry);

        GeneralData oGeneralData = oGeneralService.GetByParams(oParams);

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
        if (exitBatchesXml != null)
            oGeneralData.SetProperty(CONSTANTS.TABLES.FIELDS_HEAD_DB.EXIT_BATCHES_XML, exitBatchesXml);

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
    }
}
```

### 4.7. `Forms/TransformProduction/TransformProductionFrm.cs` — Shell / `FORM_DATA_ADD` / `FORM_DATA_LOAD`

**`OnItemEvent`** — agregar ruteo (mismo patrón `BeforeAction` + `try/catch` +
release COM) para:

```csharp
// CONFIRM_PROD (Item_2): solo estado Pendiente.
if (pVal.BeforeAction && pVal.EventType == BoEventTypes.et_ITEM_PRESSED
    && pVal.ItemUID == CONSTANTS.UID.BUTTONS.CONFIRM_PROD)
    ManejarConfirmarProduccion(oForm, out BubbleEvent);

// SHOW_DOCUMENTS (Item_5): solo estado Completado.
if (pVal.BeforeAction && pVal.EventType == BoEventTypes.et_ITEM_PRESSED
    && pVal.ItemUID == CONSTANTS.UID.BUTTONS.SHOW_DOCUMENTS)
    ManejarVerDocumentos(FormUID);

// REVERT (Item_3): solo estado Completado.
if (pVal.BeforeAction && pVal.EventType == BoEventTypes.et_ITEM_PRESSED
    && pVal.ItemUID == CONSTANTS.UID.BUTTONS.REVERT)
    ManejarReversionTransformacion(oForm, out BubbleEvent);
```

**`FORM_DATA_ADD`** (después del add) — sustituir la lógica de persistencia por el
update unificado, antes de reabrir el form:

```csharp
string docEntry = ObtenerDocEntry(oForm);
var ctx = ContextManager.ObtenerOCrear(oForm.TypeCount.ToString());

switch (ctx.PrincipalStatus)
{
    case CONSTANTS.STAGING_STATUS.COMPLETED:
        ManejarCreacionProduccion(oForm, out BubbleEvent);   // crea IGN+IGO y setea ctx.DocEntries
        break;
    case CONSTANTS.STAGING_STATUS.PENDING:
        break;
}

string exitXml = SerializarExitData(ctx.InventoryGenExitsData); // siempre (auditoría)
ActualizarResultadoTransformacion(
    Convert.ToInt32(docEntry),
    ctx.PrincipalStatus,
    ctx.InventoryGenEntriesDocEntry > 0 ? ctx.InventoryGenEntriesDocEntry : (int?)null,
    ctx.InventoryGenExitsDocEntry > 0   ? ctx.InventoryGenExitsDocEntry   : (int?)null,
    exitBatchesXml: exitXml);

AbrirDocumentosRelacionados(ctx); // con guard de DocEntry > 0 (ya persistidos en ctx)

ConnectionSDK.UIAPI.OpenForm(BoFormObjectEnum.fo_UserDefinedObject, CONSTANTS.UDO.OBJECT_CODE, docEntry);
```

**`FORM_DATA_LOAD`** — reconstruir el contexto desde el DBDataSource bound:

```csharp
ReconstruirContextoDesdeForm(oForm, ContextManager.ObtenerOCrear(oForm.TypeCount.ToString()));
```

### 4.8. `Forms/TransformProduction/TransformProductionFrm.Handlers.cs`

Nuevos métodos:

```csharp
/// Crea los documentos definitivos (IGN + IGO) desde un documento Pendiente reabierto.
public void ManejarConfirmarProduccion(SAPbouiCOM.Form oForm, out bool BubbleEvent)
{
    BubbleEvent = true;
    var ctx = ContextManager.ObtenerOCrear(oForm.TypeCount.ToString());

    if (!ValidarFormulario(oForm, ctx)) { BubbleEvent = false; return; }

    var oMatrix = (Matrix)oForm.Items.Item(CONSTANTS.UID.GRID).Specific;
    oMatrix.FlushToDataSource();
    var oDbDataSource = oForm.DataSources.DBDataSources
        .Item(CONSTANTS.TABLES.TRANSFORM_PRODUCTION_LINE_WITH_AT);
    ctx.InventoryGenEntriesData = ObtenerInfoLineas(oDbDataSource); // entrada desde líneas UDO
    // ctx.InventoryGenExitsData ya viene reconstruido desde U_ITPS_ExitBatchesXml.

    if (!CrearProduccion(ctx, out int entryDocEntry, out int exitDocEntry))
    { BubbleEvent = false; return; }

    ctx.InventoryGenEntriesDocEntry = entryDocEntry;
    ctx.InventoryGenExitsDocEntry   = exitDocEntry;

    ActualizarResultadoTransformacion(
        Convert.ToInt32(ObtenerDocEntry(oForm)),
        CONSTANTS.STAGING_STATUS.COMPLETED,
        entryDocEntry, exitDocEntry,
        exitBatchesXml: SerializarExitData(ctx.InventoryGenExitsData));

    EscribirEstadoCabecera(oForm, ctx.PrincipalStatus);
    AbrirDocumentosRelacionados(ctx);
}

/// Abre los documentos de mercancía desde los DocEntry persistidos (no del ctx).
public void ManejarVerDocumentos(string formUid)
{
    var oForm = ConnectionSDK.UIAPI.Forms.Item(formUid);
    ReconstruirContextoDesdeForm(oForm, ContextManager.ObtenerOCrear(oForm.TypeCount.ToString()));
    var ctx = ContextManager.Obtener(oForm.TypeCount.ToString());
    AbrirDocumentosRelacionados(ctx);
}

/// Reversión cruzada: la entrada se revierte con una salida; la salida con una entrada.
public void ManejarReversionTransformacion(SAPbouiCOM.Form oForm, out bool BubbleEvent)
{
    BubbleEvent = true;
    var ctx = ContextManager.Obtener(oForm.TypeCount.ToString());

    ReconstruirContextoDesdeForm(oForm, ctx);
    int docEntryUnit = Convert.ToInt32(ObtenerDocEntry(oForm));

    int respuesta = ConnectionSDK.UIAPI.MessageBox(
        "¿Confirma la reversión de la producción/transformación? Se generarán los documentos de reversión de entrada y salida.",
        1, "Cancelar", "Reversión");

    if (respuesta != 2) return; // 1 = botón "Cancelar"

    int entryRevDocEntry = 0, exitRevDocEntry = 0;

    try
    {
        ConnectionSDK.DIAPI.StartTransaction();

        // Revertir la SALIDA (IGO del principal) ⇒ generar una ENTRADA (IGN).
        if (ctx.InventoryGenExitsDocEntry > 0 && ctx.InventoryGenExitsData.Items.Count > 0)
        {
            exitRevDocEntry = CrearEntradaMercancia(ctx.InventoryGenExitsData);
            ReferenciarDocs(exitRevDocEntry, BoObjectTypes.oInventoryGenEntry,
                            ctx.InventoryGenExitsDocEntry, ReferencedObjectTypeEnum.rot_GoodsIssue);
        }

        // Revertir la ENTRADA (IGN de subproductos) ⇒ generar una SALIDA (IGO).
        if (ctx.InventoryGenEntriesDocEntry > 0)
        {
            var oDbDataSource = oForm.DataSources.DBDataSources
                .Item(CONSTANTS.TABLES.TRANSFORM_PRODUCTION_LINE_WITH_AT);
            var entriesData = ObtenerInfoLineas(oDbDataSource); // subproductos de la línea UDO
            if (entriesData.Items.Count > 0)
            {
                entryRevDocEntry = CrearSalidaMercancia(entriesData);
                ReferenciarDocs(entryRevDocEntry, BoObjectTypes.oInventoryGenExit,
                                ctx.InventoryGenEntriesDocEntry, ReferencedObjectTypeEnum.rot_GoodsReceipt);
            }
        }

        ConnectionSDK.DIAPI.EndTransaction(BoWfTransOpt.wf_Commit);
    }
    catch (Exception ex)
    {
        ConnectionSDK.DIAPI.EndTransaction(BoWfTransOpt.wf_RollBack);
        NotificationService.MostrarError($"Error revirtiendo la producción: {ex.Message}");
        return;
    }

    ctx.PrincipalStatus = CONSTANTS.STAGING_STATUS.REVERT;
    ActualizarResultadoTransformacion(docEntryUnit, CONSTANTS.STAGING_STATUS.REVERT,
        entryRevDocEntry: entryRevDocEntry > 0 ? entryRevDocEntry : (int?)null,
        exitRevDocEntry:  exitRevDocEntry  > 0 ? exitRevDocEntry  : (int?)null);

    EscribirEstadoCabecera(oForm, ctx.PrincipalStatus);
}
```

### 4.9. `Forms/TransformProduction/TransformProductionFrm.Functionality.cs`

`AbrirDocumentosRelacionados` con guard y lectura de valores persistidos:

```csharp
public static void AbrirDocumentosRelacionados(TransformProductionContext ctx)
{
    if (ctx == null) return;

    if (ctx.InventoryGenExitsDocEntry > 0)
        ConnectionSDK.UIAPI.OpenForm(BoFormObjectEnum.fo_GoodsIssue, null, ctx.InventoryGenExitsDocEntry.ToString());

    if (ctx.InventoryGenEntriesDocEntry > 0)
        ConnectionSDK.UIAPI.OpenForm(BoFormObjectEnum.fo_GoodsReceipt, null, ctx.InventoryGenEntriesDocEntry.ToString());
}
```

### 4.10. `Forms/WindowBatches/WindowBatchesFrm.*` — restaurar selección desde el DTO

Al abrir el form de lotes sobre un documento Pendiente reabierto, `ctx.BatchHeadXml`
estará vacío pero `ctx.InventoryGenExitsData` traerá los lotes persistidos. En
`CargarGrillaLotes` (Service) o en `PoblarGrillaLotes` (FormReader), mergear:

```csharp
private void MarcarLotesSeleccionados(SAPbouiCOM.Form oForm, TransformProductionContext ctx)
{
    if (oForm == null || ctx?.InventoryGenExitsData?.Items == null) return;

    var seleccion = ctx.InventoryGenExitsData.Items
        .SelectMany(i => i.Batches.Select(b => new
        {
            Batch = b.BatchNumber,
            Whs = i.Warehouse,
            Qty = b.Quantity
        }))
        .ToDictionary(x => x.Batch, x => x, StringComparer.OrdinalIgnoreCase);

    var oDataTable = oForm.DataSources.DataTables.Item(CONSTANTS.DATATABLE.UID);
    for (int i = 0; i < oDataTable.Rows.Count; i++)
    {
        string batch = oDataTable.GetValue(CONSTANTS.DATATABLE.COLUMNS.BATCH, i);
        if (seleccion.TryGetValue(batch, out var sel) && sel.Qty > 0)
        {
            oDataTable.SetValue(CONSTANTS.DATATABLE.COLUMNS.CHECK, i, "Y");
            oDataTable.SetValue(CONSTANTS.DATATABLE.COLUMNS.QTY_ASSIGNED, i,
                sel.Qty.ToString(CultureInfo.InvariantCulture));
        }
    }
}
```

Al persistir nueva selección (`PersistirSeleccionLotes`) se sigue guardando
`ctx.BatchHeadXml` para la sesión + DTO para la base.

---

## 5. Ciclo de vida resultante

```
[Crear (ADD)]
  1. Usuario carga artículo, cantidad y lotes (WindowBatches) → ctx.InventoryGenExitsData
  2. Presiona Crear → PENDING o COMPLETED
  3. FORM_DATA_ADD:
       - COMPLETED → ManejarCreacionProduccion() crea IGN + IGO, setea DocEntry en ctx
       - (siempre)   ActualizarResultadoTransformacion(status, docEntry?, batchesXml)
       - reabre el UDO → FORM_DATA_LOAD reconstruye ctx desde DB
       - AbrirDocumentosRelacionados() con guard de DocEntry > 0

[Reabrir Pendiente]
  4. FORM_DATA_LOAD → ReconstruirContextoDesdeForm:
       item, qty, estado, DocEntry (0), InventoryGenExitsData desde U_ITPS_ExitBatchesXml
  5. CONFIRM_PROD (Item_2):
       entrada = ObtenerInfoLineas(grid) | salida = ctx.InventoryGenExitsData
       CrearProduccion → IGN + IGO → persistir COMPLETED + EntryDocEntry + ExitDocEntry
  6. SHOW_DOCUMENTS (Item_5): abre IGN/IGO desde DocEntry persistidos

[Reabrir Completado → Revertir]
  7. REVERT (Item_3):
       - Reversión de salida (IGO) → CrearEntradaMercancia(exitData) → ExitRevDocEntry (IGN)
       - Reversión de entrada (IGN) → CrearSalidaMercancia(entriesData) → EntryRevDocEntry (IGO)
       - ReferenciarDocs a los originales, Commit/RollBack
       - ActualizarResultadoTransformacion(REVERT, entryRev?, exitRev?)
```

---

## 6. Consideraciones críticas y advertencias

- **Restart-safe (invariante del repo):** todo el estado se reconstruye desde la base en
  `FORM_DATA_LOAD` → sobrevive a `aet_LanguageChanged` / `aet_FontChanged`
  (`Application.Restart()`). No romper la idempotencia de `InfraDataService`.
- **Un solo `GeneralService.Update`** por evento (evita carreras y estados parciales).
  El `CambiarTransformProductionStatus` actual **traga errores** (`catch { return false; }`);
  el nuevo método notifica el error real.
- **Guard de `DocEntry = 0`:** hoy `AbrirDocumentosRelacionados` abre forms con `"0"`
  (bug latente) → corregir con el guard de `> 0`.
- **`XmlSerializer` sin dependencias nuevas** (`System.Xml.Serialization`); no instalar
  ClosedXML para esto.
- **Reversión sin método nativo:** `Documents` de `oInventoryGenEntry/Exit` **no expone**
  `Reversal()`. La reversión se implementa como documento cruzado (entrada↔salida) con
  `ReferenciarDocs`, conforme a la decisión tomada. Verificar si se desea en el futuro el
  "Anulado" nativo de SAP (Service Layer) para estos objetos.
- **Campos solo-lectura en B1 Studio:** los 5 UDF nuevos se muestran automáticamente en el
  form UDO → declararlos read-only/ocultos (los gestiona el addon).
- **Liberación COM disciplinada** en `finally` (patrón `MarshalGC.LiberarComObject`) en
  todos los objetos nuevos (`GeneralData`, `Recordset`, `Documents`, etc.).
- **Código en español** (mensajes de usuario, UDs, doc comments) y **SQL HANA** con
  identificadores en comillas dobles si se agregaran queries (aquí se lee vía
  DBDataSource del form, no con queries).

---

## 7. Tareas / checklist

- [ ] `InfraConfig.cs`: 5 UDFs en cabecera (4 numéricos + 1 memo).
- [ ] `Constants.cs`: constantes de los 5 campos (`FIELDS_HEAD`, `FIELDS_HEAD_DB`, `FIELDS_HEAD_DESC`).
- [ ] `Models/TransformProductionUdoModel.cs`: DTO de cabecera.
- [ ] `Mapper.cs`: `SerializarExitData`, `DeserializarExitData`, `MapearCabeceraUdo`.
- [ ] `FormReader.cs`: `LeerCabeceraUdo`, `ReconstruirContextoDesdeForm`.
- [ ] `Repository.cs`: `ActualizarResultadoTransformacion` (reemplaza `CambiarTransformProductionStatus`).
- [ ] Shell `.cs`: ruteo de `CONFIRM_PROD`, `SHOW_DOCUMENTS`, `REVERT`; update unificado en `FORM_DATA_ADD`.
- [ ] `FORM_DATA_LOAD`: reconstrucción del contexto.
- [ ] `Handlers.cs`: `ManejarConfirmarProduccion`, `ManejarVerDocumentos`, `ManejarReversionTransformacion`.
- [ ] `Functionality.cs`: guard de `AbrirDocumentosRelacionados`.
- [ ] `WindowBatches`: merge de selección persistida al abrir el grid.
- [ ] B1 Studio: campos nuevos read-only/hidden en el diseño del form.
- [ ] Compilar con MSBuild (`.sln`), no hay tests/CI.