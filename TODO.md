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
- [ ] **TP-05 — `et_FORM_DATA_LOAD` en BeforeAction.** En ese momento el registro todavía no se
  cargó: el contexto y la habilitación de botones se arman con datos del registro anterior.
  Usar `ActionSuccess`. Además el `finally` hace `oForm.Freeze(false)` antes de chequear null.
- [ ] **TP-06 — Precio nuevo ignorado / fechas de lote vacías.** `CrearDocumentoInventario` no
  setea `Lines.UnitPrice` ni moneda: "Precio (nuevo)" no tiene efecto y el reingreso en la
  reversión se valúa al costo actual. La salida manda lotes con fechas `DateTime.MinValue`
  (no hace falta mandar fechas para lotes existentes).

## 🟠 Altos

- [ ] **TP-07 — `et_FORM_ACTIVATE` pisa el estado.** Reescribe `ctx.PrincipalStatus` con el combo
  (Pendiente por defecto); si el form se activa entre el MessageBox de Crear y el grabado,
  "Crear" termina como Pendiente. *Propuesta:* escribir la elección en el combo
  (`EscribirEstadoCabecera`) en el mismo click, así queda persistida y es consistente.
- [ ] **TP-08 — Cambio de artículo con lotes ya elegidos.** `ManejarCodigoArticuloPerdidaFoco` no
  limpia `BatchHeadXml` / `InventoryGenExitsData`: la salida sale con lotes del artículo
  anterior. Además cada pérdida de foco recarga la grilla y pisa cantidades/precios editados.
- [ ] **TP-09 — Confirmar continúa tras un fallo.** Después de `ManejarConfirmarProduccion` se
  cierra el contexto y se reabre el documento aunque `BubbleEvent` haya quedado en false.
- [ ] **TP-10 — Revertir deja el form en modo Update.** `EscribirEstadoCabecera` tras revertir deja
  cambios pendientes; si el usuario los graba al cerrar, el update del UDO puede vaciar los
  DocEntry de reversión guardados por DI API. Recargar el registro en vez de escribir el combo.
- [ ] **TP-11 — Habilitación persistente.** `FormularioEnCualquierEstado` deshabilita Producto y
  nada lo rehabilita al pasar a "Nuevo"; los botones quedan como en el registro anterior.
  Retomar la "red de seguridad" OK→ADD comentada (o `SetAutoManagedAttribute` por modo).
- [ ] **TP-12 — Contexto borrado tras Agregar.** `ManejarCierreFormulario(boi.FormUID)` borra el
  contexto de un formulario que sigue abierto: se pierde `FormTransfProd` y la etiqueta de
  lotes deja de actualizarse. Usar `ResetearContexto()` en su lugar.

## 🟡 Medios / menores

- [ ] **TP-13 — Tipo de cambio.** Crear chequea `RecordCount == 0`, Confirmar chequea `Rate == 0`
  (una cotización en 0 pasa en Crear). Los `Recordset` no se liberan. La lógica está inline en
  el shell de eventos (va en Service/Repository). Evaluar si aplica también a Pendiente.
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
