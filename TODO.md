# TODO — Auditoría de funcionalidad (Producción / Transformación)

Pendientes surgidos de la auditoría sobre `master` (commit `66125c0`). Cada ítem tiene un
comentario `// TODO(TP-xx)` en el código, en el lugar donde hay que trabajar.

Prioridad: 🔴 crítico (puede duplicar/romper stock o dejar datos inconsistentes) ·
🟠 alto · 🟡 medio/menor.

## 🔴 Críticos

- [x] **TP-01 — Doble reversión.** `ManejarReversionTransformacion` (Handlers) no deshabilita
  el botón Revertir ni recarga el formulario: el form sigue con los DocEntry originales y un
  segundo click genera otra vez los documentos de reversión.
  *Propuesta:* validar que el estado persistido sea Completado y que no haya DocEntry de
  reversión; al terminar, recargar el registro (y con eso `FormularioEnEstadoRevertido`).
  *Hecho:* se valida contra la cabecera leída de la base (`PuedeRevertirse`), el botón se
  deshabilita al confirmar, el update del UDO va dentro de la transacción de reversión y al
  terminar se recarga el registro (`RecargarRegistro`, modo OK). Pendiente de probar en SAP.
- [x] **TP-02 — Formulario de lotes con UID fijo.** `WindowBatchesFrm.ConstruirFormulario` no
  modifica el XML: el form queda `WINDOW_BATCHES` en vez de `WINDOW_BATCHES_{typeCount}`.
  - `ObtenerTypeCount` devuelve null → se usa el TypeCount del form de lotes (1) → los lotes
    se guardan en el contexto de la producción 1, no en la que los pidió.
  - Como el flujo reabre el documento en otra ventana (`OpenForm` tras Agregar/Confirmar),
    al confirmar un Pendiente falla "La cantidad total asignada no coincide".
  - Reabrir lotes con el form ya abierto da error (LoadBatchActions con UID repetido).
  *Propuesta:* reemplazar `uid="WINDOW_BATCHES"` por `ObtenerFormUID(typeCount)` en el XML
  antes de `LoadBatchActions`, y en `MostrarFormulario` buscar el form por ese UID.
  *Hecho:* `PrepararXmlFormulario` fija `uid` = `WINDOW_BATCHES_{typeCount}` y `FormType` =
  `CONSTANTS.FORM_TYPE` en el XML antes de `LoadBatchActions`; `MostrarFormulario` busca el
  form por ese UID. Pendiente de probar en SAP.
- [x] **TP-03 — Validación después de grabar / Completado sin documentos.** La creación de la
  Entrada/Salida corre en `et_FORM_DATA_ADD` con `ActionSuccess`: el UDO ya está grabado y
  `BubbleEvent = false` no lo deshace. Si `ManejarCreacionProduccion` falla, igual se llama a
  `ActualizarResultadoTransformacion` con estado Completado.
  *Propuesta:* validar (`ValidarFormulario`) en el BeforeAction del botón Crear y cortar ahí;
  en el after-add, persistir Completado solo si `CrearProduccion` devolvió true.
  *Hecho:* con "Crear" se valida en el BeforeAction (`PrepararCreacionCompletada`: artículo,
  cantidad, lotes y líneas de subproductos) y si falla no se graba el UDO. El registro se graba
  siempre Pendiente; en el after-add (`ManejarProduccionAgregada`) se crean IGN/IGO con los
  datos validados y solo si se crean pasa a Completado. El DocEntry se toma de `boi.ObjectKey`.
  Además, con Pendiente o Crear, `ValidarLineasDetalle` bloquea el grabado si no hay líneas de
  detalle o si alguna línea con subproducto no tiene: número de lote, cantidad obtenida > 0,
  almacén existente en OWHS, precio nuevo > 0, moneda y fechas de vencimiento, fabricación e
  ingreso del lote.
  Pendiente de probar en SAP.
- [x] **TP-04 — Resultado del update del UDO ignorado.** Si IGN/IGE se confirman pero
  `ActualizarResultadoTransformacion` falla, el documento queda Pendiente con el stock ya
  movido y se puede volver a confirmar (duplica movimientos). Revisar el `bool` devuelto y
  avisar/bloquear; idealmente actualizar el UDO dentro de la misma transacción.
  *Hecho:* `CrearProduccion(ctx, udoDocEntry, ...)` crea IGN/IGO y actualiza el UDO a Completado
  en la misma transacción; si el update falla se hace rollback de todo (Crear y Confirmar).
  Confirmar valida contra la base (`PuedeConfirmarse`): solo Pendiente y sin DocEntry de
  entrada/salida. Pendiente de probar en SAP.
- [x] **TP-05 — `et_FORM_DATA_LOAD` en BeforeAction.** En ese momento el registro todavía no se
  cargó: el contexto y la habilitación de botones se arman con datos del registro anterior.
  Usar `ActionSuccess`. Además el `finally` hace `oForm.Freeze(false)` antes de chequear null.
  *Hecho:* el bloque corre con `ActionSuccess` y se delega a `ManejarRegistroCargado` (Service);
  el `Freeze(false)` va dentro del chequeo de null. Pendiente de probar en SAP.
- [x] **TP-06 — Precio nuevo ignorado / fechas de lote vacías.** `CrearDocumentoInventario` no
  setea `Lines.UnitPrice` ni moneda: "Precio (nuevo)" no tiene efecto y el reingreso en la
  reversión se valúa al costo actual. La salida manda lotes con fechas `DateTime.MinValue`
  (no hace falta mandar fechas para lotes existentes).
  *Hecho:* en las Entradas se manda `Lines.UnitPrice` y `Lines.Currency` ("Precio/Moneda
  (nuevo)"); en las Salidas no se mandan precio ni fechas de lote. Las fechas vacías
  (`MinValue`) no se envían. La reversión reconstruye siempre la salida desde el OIGE original
  y reingresa al costo de esa salida (`IGE1."StockPrice"`). Pendiente de probar en SAP.

## 🟠 Altos

- [x] **TP-07 — `et_FORM_ACTIVATE` pisa el estado.** Reescribe `ctx.PrincipalStatus` con el combo
  (Pendiente por defecto); si el form se activa entre el MessageBox de Crear y el grabado,
  "Crear" termina como Pendiente. *Propuesta:* escribir la elección en el combo
  (`EscribirEstadoCabecera`) en el mismo click, así queda persistida y es consistente.
  *Hecho:* (base en TP-03) la elección "Crear" vive en `ctx.CompletarAlAgregar` y el combo se
  escribe Pendiente en el mismo click; `ctx.PrincipalStatus` también queda Pendiente hasta que
  se crean IGN/IGO. `et_FORM_ACTIVATE` delega a `ManejarActivacionContexto` y solo sincroniza
  artículo y cantidad (ya no pisa el estado ni la entrada). Pendiente de probar en SAP.
- [x] **TP-08 — Cambio de artículo con lotes ya elegidos.** `ManejarCodigoArticuloPerdidaFoco` no
  limpia `BatchHeadXml` / `InventoryGenExitsData`: la salida sale con lotes del artículo
  anterior. Además cada pérdida de foco recarga la grilla y pisa cantidades/precios editados.
  *Hecho:* `AplicarCambioProducto` descarta la selección de lotes (`DescartarSeleccionLotes`:
  contexto, etiqueta y form de lotes abierto) al cambiar de artículo; si se borra el artículo
  se limpia la grilla. El CFL ya no recarga la grilla si se elige el mismo artículo (la
  pérdida de foco ya comparaba). Pendiente de probar en SAP.
- [x] **TP-09 — Confirmar continúa tras un fallo.** Después de `ManejarConfirmarProduccion` se
  cierra el contexto y se reabre el documento aunque `BubbleEvent` haya quedado en false.
  *Hecho:* el shell corta con `if (BubbleEvent == false) return;` después de
  `ManejarConfirmarProduccion` (fallo o Cancelar): no se cierra el contexto ni se reabre.
- [x] **TP-10 — Revertir deja el form en modo Update.** `EscribirEstadoCabecera` tras revertir deja
  cambios pendientes; si el usuario los graba al cerrar, el update del UDO puede vaciar los
  DocEntry de reversión guardados por DI API. Recargar el registro en vez de escribir el combo.
  *Hecho:* Revertir (TP-01) y Confirmar recargan el registro (`RecargarRegistro`, modo OK) en
  vez de escribir el combo. Pendiente de probar en SAP.
- [x] **TP-11 — Habilitación persistente.** `FormularioEnCualquierEstado` deshabilita Producto y
  nada lo rehabilita al pasar a "Nuevo"; los botones quedan como en el registro anterior.
  Retomar la "red de seguridad" OK→ADD comentada (o `SetAutoManagedAttribute` por modo).
  *Hecho:* el menú Nuevo llama a `ManejarModoAgregar`: reinicia el contexto, descarta lotes y
  aplica `FormularioEnModoAgregar` (Confirmar/Documentos/Revertir inactivos, grilla y cantidad
  editables); el botón de lotes se habilita en modo agregar con artículo y cantidad válidos.
  Pendiente de probar en SAP.
- [x] **TP-12 — Contexto borrado tras Agregar.** `ManejarCierreFormulario(boi.FormUID)` borra el
  contexto de un formulario que sigue abierto: se pierde `FormTransfProd` y la etiqueta de
  lotes deja de actualizarse. Usar `ResetearContexto()` en su lugar.
  *Hecho:* tras Agregar se llama a `ReiniciarContextoFormulario` (descarta lotes y
  `ResetearContexto`) y tras Confirmar a `ManejarProduccionConfirmada` (descarta lotes y
  reconstruye el contexto desde el registro). `ManejarCierreFormulario` queda solo para el cierre
  real (et_FORM_CLOSE, que ahora delega en él). Pendiente de probar en SAP.

## 🟡 Medios / menores

- [ ] **TP-13 — Monedas y tipo de cambio de las líneas.**
  *Hoy:* solo se controla la cotización del **USD** del día, y distinto en cada botón: Crear
  chequea `RecordCount == 0` (una cotización cargada en 0 pasa) y Confirmar chequea `Rate == 0`.
  Los `Recordset` no se liberan y la lógica está duplicada inline en el shell de eventos (va en
  Repository/Service). No se mira qué monedas tienen realmente las líneas.
  *Caso:* las líneas pueden tener 3 o 4 monedas distintas en "Moneda (nuevo)" (`U_ITPS_Curr`),
  p.ej. ARS, USD, EUR y BRL (las definidas en OCRN: ARS, BRL, CNY, EUR, USD). Si una línea está
  en EUR y no hay cotización del EUR del día, la Entrada falla o se valúa mal aunque el USD esté
  cargado.
  *Propuesta:*
  - **Moneda permitida:** cada línea con subproducto debe tener una moneda que exista en
    **OCRN** (`"CurrCode"`). Una sola consulta para todas las monedas de las líneas (mismo
    patrón que almacenes/lotes: Repository → Mapper → Service), dentro de
    `ValidarLineasDetalle`. Mensaje: "La línea N (subproducto X): la moneda Y no existe".
  - **Tipo de cambio por moneda:** para cada moneda **distinta** usada en las líneas que no sea
    la moneda local (`OADM."MainCurncy"`), exigir cotización del día en **ORTT** con
    `"Rate" > 0`. Una sola consulta; el mensaje lista las monedas sin cotización
    ("Falta el tipo de cambio de hoy para: EUR, BRL") y abre la ventana de tipos de cambio
    (menú 3333), como hoy.
  - Reemplaza el control actual del USD (que se elimina del shell) y se aplica igual en Crear y
    en Confirmar. Liberar los `Recordset` en `finally`.
  *Dudas abiertas:*
  - ¿Con **Pendiente** se exige la cotización? (no se crean documentos hasta Confirmar, donde
    se volvería a validar).
  - ¿La cotización se toma de la **fecha del día** (hoy) o de otra fecha (p.ej. la del documento)?
  - ¿Hace falta controlar también la moneda de sistema (`OADM."SysCurrncy"`) si difiere de la local?
- [ ] **TP-14 — `ReferenciarDocs`.** Sin `DocumentReferences.Add()`, ignora `GetByKey` y el
  resultado, y en la creación se llama fuera de la transacción. Verificar que la referencia
  quede grabada.
- [ ] **TP-15 — Validaciones faltantes.** `ValidarFormulario` no verifica que haya líneas de
  entrada con lote ni que el Producto (si se tipea a mano) tenga lista de materiales.
  Comparaciones `==`/`!=` entre `double` (usar tolerancia).
- [ ] **TP-16 — Reversión de la entrada desde la grilla.** Se arma con `ObtenerInfoLineas(oForm)`,
  que sigue editable en Completado. Reconstruir desde el IGN original como ya se hace con la
  salida (`ObtenerExitDataDesdeDocumento`).
- [ ] **TP-17 — Limpieza.**
  - `AbrirDocumentosRelacionados` abre cada documento dos veces.
  - `using` sin uso: `System.Text.Json`, `System.Text.Json.Serialization`, `System.Xml`,
    `System.Threading.Tasks` (TransformProductionFrm.cs), `VisualStyleElement` (Functionality).
  - Código muerto: `FatherItemCode`, `AbrirFormularioNuevo`, `ManejarCreacionProduccionPendiente`,
    `InicializarFormulario`, `OnMenuEvent` comentado.

## Relacionado

- [ ] **TP-18 — Filtro del ChooseFromList de Producto** (solo artículos con lista de materiales,
  `OITM."TreeType" = 'P'`). Implementado en la rama `claude/practical-brahmagupta-vh57eb`;
  pendiente de probar en SAP.
