# AGENTS.md

SAP Business One v10 addon (cliente COOMARPES) en .NET Framework 4.7.2 / C# 7.3 / WinExe.
Windows Forms message loop; integra SAP UI API (`SAPbouiCOM`) y DI API (`SAPbobsCOM`), base HANA.

## Build / Run
- No es CLI-friendly: `.csproj` clásico (MSBuild `ToolsVersion=15.0`). Compilar con Visual Studio 2022 (`Ctrl+Shift+B`) o `msbuild Addon_ProduccionTransformacion.sln`. No hay test, lint, ni CI.
- **Debe ejecutarse desde SAP** (Add-On Administration): SAP pasa el connection string como primer argumento (`Environment.GetCommandLineArgs()[1]`). `launchSettings.json` inyecta un string para debug local. Correr el `.exe` suelto falla con `MissingSapConnectionString`.
- `packages.config` clásico (no PackageReference). Al agregar paquetes, editar `packages.config` + bloques `<Reference>` del `.csproj` a mano (el paquete ClosedXML **no está instalado aún** — ver abajo).
- Cambios de idioma/fuente en SAP (`aet_LanguageChanged`/`aet_FontChanged`) relanzan el addon vía `Application.Restart()` — todo el flujo de arranque debe ser idempotente (menús registrados con guard `oMenus.Exists`, tablas/campos con chequeo de existencia). No romper esta invariante.

## Arquitectura
- **Entrada**: `Program.Main` (`[STAThread]`) → `ConnectionSDK.Singlenton()` (singleton de UIAPI/DIAPI) → `ExecutionsApp` (infraestructura) → `EventRouter` → `Application.Run()`.
- **Enrutamiento de eventos**: `EventRouter` suscribe `ItemEvent`/`MenuEvent`/`FormDataEvent`/`AppEvent` en `ConnectionSDK.UIAPI` y delega a handlers registrados en un `Dictionary<string, IFormEventHandler>` claveado por `FormTypeEx`. **Cada formulario nuevo debe registrarse acá**, y usar `BubbleEvent = true` siempre.
- **Formularios** implementan `IFormEventHandler` (`OnItemEvent`/`OnFormDataEvent`/`OnMenuEvent`) y se dividen en clases parciales por responsabilidad fija:

  ### Convención de formularios: clase parcial por entidad

  Cada formulario (`TransformProductionFrm`, `WindowBatchesFrm`, ...) es **una sola clase parcial** repartida en archivos `Addon_TransformProduction\Forms\<Formo>\<Formo>Frm.*.cs`. El patrón de referencia completo del repo es `ActualizacionCostosFrm`.

  | Parcial | ÚNICA responsabilidad |
  |---|---|
  | `X.cs` (main) | **Shell de eventos**: solo `OnItemEvent`/`OnFormDataEvent`/`OnMenuEvent`. Decide las condiciones de ruteo del evento (qué item/evento/columna) y delega a **un método por evento** que vive en `Service` o `Functionality`. `BubbleEvent = true` siempre; `try/catch` con `NotificationService`; `finally` con release COM. **Sin lógica de negocio inline.** |
  | `X.Constants.cs` | UIDs de UI, tablas/UDFs, mensajes. Nada de código. |
  | `X.UIBuilder.cs` | `BuildForm`/`LoadBatchActions`, post-proceso de editabilidad de columnas, habilitar/deshabilitar botones. Registro de menús SOLO si no hay `*Menu.cs`. |
  | `X.FormReader.cs` | Leer la UI (campos/cabecera) y **poblar el estado visual** de grillas y labels vía `DBDataSource` + `LoadFromDataSource`. Nada de consultas SQL. |
  | `X.Service.cs` | **Orquestación de flujos del form**: coordina repository → mapper → pintado de UI (p.ej. `OnItemCodeLostFocus`). |
  | `X.Functionality.cs` | Validaciones y helpers (`ShowForm`, apertura de otros forms, helpers de matriz). |
  | `X.Repository.cs` | Queries HANA con `Recordset`; el caller libera en `finally`. Sin lógica de UI. |
  | `X.Mapper.cs` | Mapping Recordset→DTO, sin queries. |

  Reglas de cierre:
  - El main delega un evento → **un solo método**; toda la cadena (query/map/pintar) se lee como orquestación en `Service`.
  - Modelos/DTOs siempre en `Models\` (namespace `Addon_TransformProduction.Models`), **nunca** clases anidadas dentro del form.
  - Namespace unificado: `Addon_TransformProduction.Forms.<Formo>.*` (excepción heredada: `WindowBatches` ya unificado; no crear `Addon_ProduccionTransformacion.*` nuevo).
  - Archivos obsoletos se **borran** del disco (p.ej. `TransformProductionFrm.Menu.cs`); no dejar `.cs` colgados con comentarios.
- **Forms desde B1 Studio**: los diseños se crean en B1 Studio, se exportan a `.xml` y se cargan en runtime con `ConnectionSDK.UIAPI.LoadBatchActions(ref xml)` (path relativo `Forms\<Form>\B1 Studio\*.xml`, marcado `CopyToOutputDirectory`). El `.b1s` es solo fuente de diseño, no se despliega. Post-procesar en `UIBuilder` la editabilidad de columnas (B1 Studio deja todo editable por defecto).
- **Esquema como infraestructura**: en arranque, `ExecutionsApp` → `TransformProductionInfra.ProcessInfrastructure()` crea idempotentemente UDTs (`CreateUserTable`), UDFs (`CreateUserField`) y registra el UDO `ITPS_TRANSFPROD` (tipo Document, cab + línea, `CanCreateDefaultForm=tNO`) vía `InfraDataService`. Todo nombre de objeto SAP usa prefijo `ITPS_` / `itps`.
- **Patrón staging**: el UDO guarda documentos de producción/transformación en tablas SAP HANA (`ITPS_TRANSFPROD_CAB`/`_LIN`) con estado (`STAGING_STATUS`), que un **Windows Service externo** consume para ejecutar la operación real. El addon solo arma y valida.
- **Capas**: `Repositories/` = queries DI API contra SAP (OITM/OITW/OWHS); `Services/` = lógica reutilizable; `Models/` = estado en memoria.

## Gotchas críticos
- **Dos conjuntos de constantes duplicados**: el activo es `Forms\TransformProductionFrm\TransformProductionFrm.Constants.cs` (p.ej. tabla `ITPS_TRANSFPROD_CAB`, usada por `InfraConfig`). `Common\Constants.cs` contiene valores obsoletos/divergentes (p.ej. `ITPS_TRANSFPROD_HEAD`). Al tocar esquemas, usa el primero y concilia el segundo.
- **Estado actual**: `TransformProductionFrm` es el feature activo — sus parciales (`FormReader`/`Service`/`Functionality`) están pobladas con el flujo de BOM (leer artículo → query → pintar grilla) y `UIBuilder` aún sin `BuildForm`. `ActualizacionCostosFrm` es legacy (desconectado del `EventRouter`, init comentado en `Executions.cs`). No asumir que el código del form activo está completo.
- **ClosedXML NO instalado**: `ExcelImportService.ReadRows` y `ActualizacionCostosFrm.Service.ConfirmScheduling` son stubs con el código completo comentado, esperando el paquete ClosedXML. Parte de la funcionalidad de Excel depende de eso.
- **Root namespace vs assembly**: assembly/RootNamespace = `Addon_AutoUpdateCost` (legacy), namespace de código = `Addon_TransformProduction`. Mantener `Addon_TransformProduction.*`.
- **SQL es HANA**: identificadores entre comillas dobles (`"ItemCode"`), string interpolation con escape manual de comillas simples (`EscapeSql`), sin consultas parametrizadas. Seguir ese patrón.
- **COM release disciplinado**: todo `Recordset`/`UserTablesMD`/`UserFieldsMD`/`UserObjectsMD` se libera en `finally` con `Marshal.ReleaseComObject` (o `FinalReleaseComObject`). Ver `Tools/MarshalGC.cs`. Mantener esta disciplina.
- **Uso de WinForms**: mensajes/diálogos usan WinForms (MessageBox, OpenFileDialog). Los diálogos de archivo deben abrirse en un thread STA dedicado (`FileService`) para no bloquear el thread de eventos COM de SAP. `Program.Main` es `[STAThread]` — no quitarlo.
- **Código en español** (argentino): mensajes de usuario, comentarios y UIDs de UI en español. UI control UIDs máx. 10 caracteres y debajo del form (prefijo `itps`).
- **`.github/copilot-instructions.md` es sobre Azure y ajeno al repo** — ignorar; no es fuente válida de convenciones del proyecto.

## Docs
- No hay changelog ni TODO reales: el `.csproj` referencia `TODO.md` como `<None Include>`, pero ni `TODO.md` ni `HISTORIAL.md` existen en disco. No buscarlos.
