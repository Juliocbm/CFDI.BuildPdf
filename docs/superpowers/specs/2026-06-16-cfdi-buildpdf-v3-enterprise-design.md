# CFDI.BuildPdf v3.0.0 — Diseño de refactor a nivel enterprise

- **Fecha:** 2026-06-16
- **Estado:** Aprobado (pendiente de plan de implementación)
- **Versión objetivo:** 3.0.0 (breaking changes opt-in)
- **Target framework:** net8.0 (LTS)
- **Rama de trabajo:** `Refactor`

## 1. Contexto

`CFDI.BuildPdf` es una librería NuGet (.NET, actualmente net6.0, v2.0.8) que genera la representación impresa en PDF de un XML CFDI 4.0 del SAT (México), con complementos **Carta Porte 3.1** y **Nómina 1.2**. Usa QuestPDF como motor de render y QRCoder para el QR del timbre fiscal. Es cross-platform y sin dependencias nativas.

Una auditoría previa de la API encontró duplicados, superficie pública innecesaria y observaciones de diseño. Este documento define el rediseño v3 para resolverlos y elevar la librería a nivel enterprise: alta mantenibilidad, alta escalabilidad (añadir complementos nuevos), correcta reutilización de código y buenas prácticas.

## 2. Objetivos y decisiones tomadas

- **Versionado:** v3.0.0 con breaking changes **opt-in**. Los consumidores en v2.0.8 siguen funcionando (NuGet no actualiza versiones mayores solo); migran cuando lo decidan, guiados por `MIGRATION.md`. Se mantiene una rama `release/2.x` para parches críticos.
- **Target framework:** **net8.0** únicamente (LTS). Sube `Microsoft.Extensions.*` a 8.x.
- **Extensibilidad:** **caja cerrada.** El consumidor solo consume XML→PDF. Internamente, un registro de complementos hace trivial **añadir** tipos nuevos, pero mappers/builders/modelos/handlers son `internal`.
- **Pilares enterprise incluidos:** Calidad + CI/CD, Testing reforzado, Packaging & docs pro. (No se abre un workstream dedicado de observabilidad; los arreglos mínimos de robustez derivados de los hallazgos —async honesto, licencia idempotente, logging consistente— sí entran.)
- **Estrategia de ejecución:** Enfoque incremental in-place con **red de seguridad de golden tests primero** (Enfoque A). Nada de reescritura a ciegas ni greenfield.

## 3. Hallazgos de la auditoría que se deben resolver

**Duplicados:** D1 (la fachada estática duplica `ICfdiPdfGenerator` y replica el cableado DI); D2 (`ICfdiModelMapper.CanMap` duplica `ICfdiTypeDetector.Detect`; `CanMap` no se usa en producción); D3 (`QrGeneratorService` expone la lógica QR dos veces: `GenerateQr` estático + `GenerateBase64`); D4 (`TrasladoImpuestoViewModel` y `RetencionConceptoViewModel` son estructuralmente idénticos).

**Obsoletos / superficie muerta:** O1 (sin `[Obsolete]`; `GenerateQr` "backward-compatibility" carece de sentido en clase internal); O2 (`CanMap` público sin uso real); O3 (todo `Models` es público solo porque las interfaces genéricas lo filtran).

**Observaciones:** X1 (namespaces casi idénticos `CFDI.BuildPdf.Service` vs `CFDI.BuildPdf.Services`); X2 (DTOs públicos mutables sin docs XML por propiedad); X3 (async falso: `Task.FromResult` envolviendo trabajo síncrono); X4 (`ConfigureQuestPdfLicense` frágil: solo re-aplica si el `Lazy` ya se creó; la licencia es global de proceso).

## 4. Verificación adversarial (resumen)

Tres revisores independientes leyeron el código real y validaron la dirección, con veredicto **"cumple-con-cambios"**. Correcciones adoptadas (todas integradas en este diseño):

1. **El handler NO absorbe al mapper.** Si `Generate` solo devuelve `byte[]`, se rompen ~25 tests unitarios que afirman sobre el ViewModel y el `FakeQrGenerator`. El handler es un orquestador delgado que recibe mapper + builder por constructor; ambos siguen separados y testeables (`internal` + `InternalsVisibleTo`).
2. **`Type` + `CanHandle` reintroducen `CanMap`.** Una sola fuente de verdad: despacho por **namespace del complemento** en un `Dictionary<string, handler>`, con una sola pasada por los hijos de `cfdi:Complemento`.
3. **Versionado y múltiples complementos.** El namespace ya lleva la versión (Carta Porte 3.0/3.1, Nómina 1.1/1.2, Pagos 1.0/2.0) → la llave por namespace lo resuelve. Colisión de llave = error en arranque. Desempate por **prioridad explícita**, no por orden de registro DI.
4. **Sin `ServiceProvider` estático en la fachada.** Un único *composition root* interno compartido por la fachada y `AddCfdiPdfServices` elimina el doble cableado (D1) sin arrastrar el contenedor DI completo al escenario zero-DI ni los problemas de dispose/licencia.
5. **`CfdiPdfSections.cs` (≈1020 líneas) mezcla catálogos SAT (datos) con render QuestPDF.** Mayor pasivo de SRP del repo → se extrae `SatCatalogos`.
6. **Encabezado/logo/footer duplicados** entre los dos builders → subir a `CfdiPdfSections` antes de multiplicar handlers; el patch de `LogoBase64` va a una base de handler, no copiado N veces.
7. **D4 y X3** se añaden explícitamente al alcance (unificar ViewModels de impuesto; async honesto).

## 5. Arquitectura objetivo

```
ICfdiComplementHandler                         (internal)
   IReadOnlyCollection<string> Namespaces      // namespace(s) de complemento que maneja (incluye versión)
   int  Priority                               // desempate determinista si varios aplican
   byte[] Generate(XDocument xdoc, CfdiPdfOptions opts)

ComplementHandlerBase<TModel>                  (internal)  ← lógica común
   ctor(ICfdiModelMapper<TModel> mapper, IPdfDocumentBuilder<TModel> builder)
   Generate = mapper.Map(xdoc) → aplicar opts (LogoBase64, etc., EN UN SOLO LUGAR) → builder.Build(model, opts)

CartaPorteComplementHandler : ComplementHandlerBase<CfdiCartaPorteViewModel>   // Namespaces = { ".../CartaPorte31" }
NominaComplementHandler     : ComplementHandlerBase<CfdiNominaViewModel>       // Namespaces = { ".../nomina12" }

CfdiPdfGenerator (internal, ICfdiPdfGenerator)
   ctor(IEnumerable<ICfdiComplementHandler> handlers) → Dictionary<namespace, handler> (colisión = error de arranque)
   GenerarDesde*Async → await cargar XDocument (LoadAsync en I/O)
                      → inspeccionar hijos de cfdi:Complemento → match por namespace → (desempate por Priority)
                      → 0 matches → CfdiComplementoNoSoportadoException
                      → handler.Generate(xdoc, opts)

CfdiPdf (fachada estática) y AddCfdiPdfServices → ambos usan el MISMO composition root interno (CfdiPdfFactory).
   Licencia QuestPDF: idempotente (no pisa una ya configurada); + CfdiPdf.ConfigureLogging(ILoggerFactory).
```

**Mappers y builders** se mantienen separados (SRP, testeables); el handler solo los coordina. **Añadir un complemento nuevo** (p. ej. Pagos 2.0) = crear `PagosMapper` + `PagosDocumentBuilder` + `PagosComplementHandler` (declara su namespace) + 1 línea de registro. Orquestador, detección y fachada no se tocan.

**Decisiones por defecto:**
- `ICfdiTypeDetector` / `CfdiType` → **`internal`** (la detección es interna; el consumidor solo necesita XML→bytes). Reduce superficie pública y rompe el acoplamiento al enum cerrado.
- **Opciones por complemento** (hoy `CfdiPdfOptions` está sesgado a Carta Porte): se diseña la costura pero se deja **fuera del v3 inicial** (YAGNI); se añade cuando entre el primer complemento que lo requiera.

## 6. Superficie pública v3

Principio: lo público vive en `CFDI.BuildPdf`; la extensión DI en `Microsoft.Extensions.DependencyInjection`; todo lo demás `internal`. Pasa de ~45 tipos públicos a ~9.

| Tipo | Namespace | Miembros públicos |
|---|---|---|
| `CfdiPdf` (fachada) | `CFDI.BuildPdf` | `ConfigureQuestPdfLicense`, `ConfigureLogging(ILoggerFactory)` (nuevo), `DesdeRutaAsync`, `DesdeXmlStringAsync`, `DesdeXmlBytesAsync`, `DesdeStreamAsync`, `GuardarDesdeRutaAsync`, `EscribirEnStreamAsync` |
| `AddCfdiPdfServices` (ext.) | `Microsoft.Extensions.DependencyInjection` | `AddCfdiPdfServices(this IServiceCollection, Action<CfdiPdfOptions>?, CfdiPdfLicenseType)` |
| `ICfdiPdfGenerator` | `CFDI.BuildPdf` | los 4 `Generar*Async` |
| `CfdiPdfOptions` + `PdfOrientation` | `CFDI.BuildPdf` | opciones de generación |
| `CfdiPdfLicenseType` | `CFDI.BuildPdf` | enum de licencia |
| `CfdiPdfException` / `CfdiXmlInvalidoException` / `CfdiComplementoNoSoportadoException` | `CFDI.BuildPdf` | jerarquía de errores |

`internal`: modelos (`CfdiViewModelBase` + árboles de Carta Porte/Nómina), `ICfdiModelMapper<T>`, `IPdfDocumentBuilder<T>`, `IQrGenerator`, `ICfdiTypeDetector`, `CfdiType`, mappers, builders, handlers, `QrGeneratorService`, `CfdiTypeDetector`, `NumberToWordsConverter`, `QrUrlBuilder`, `PdfStyleConstants`, `CfdiPdfSections`, `SatCatalogos`.

## 7. Inventario de breaking changes (núcleo de `MIGRATION.md`)

1. **TargetFramework `net6.0` → `net8.0`.** Consumidores en net6/net7 actualizan o permanecen en `v2.x`.
2. **Namespace de la fachada `CFDI.BuildPdf.Service` → `CFDI.BuildPdf`.** Para el ~99% de usuarios (los que llaman `CfdiPdf.DesdeRutaAsync`), la migración es **una sola línea** (`using`).
3. **`public` → `internal`** (afecta solo a consumidores avanzados): `ICfdiModelMapper<T>`, `IPdfDocumentBuilder<T>`, `IQrGenerator`, `ICfdiTypeDetector`, `CfdiType`, todo `Models.*`. El único punto de extensión público que se pierde es `IQrGenerator`; se documenta explícitamente.
4. **`CanMap` eliminado** de `ICfdiModelMapper` (irrelevante para el consumidor; la interfaz pasa a internal).

**Política de deprecación:** al ser cambios de visibilidad, v3 hace un **corte limpio y documentado** (no `[Obsolete]` gradual). `MIGRATION.md` lista cada cambio con su equivalente v3 y ejemplos antes/después.

## 8. Plan de fases (cada fase verde antes de avanzar)

- **F0 — Red de seguridad + base:** migrar a net8.0 + paquetes 8.x; crear **golden tests** de los PDFs actuales (Carta Porte + Nómina) como baseline; montar `.editorconfig`, nullable estricto y analizadores Roslyn (sin romper el build aún). *(R6, baseline)*
- **F1 — Reúso/extracción sin cambiar comportamiento:** extraer `SatCatalogos` de `CfdiPdfSections`; subir encabezado/logo/footer común a `CfdiPdfSections`; unificar los dos ViewModels de impuesto; colapsar `GenerateQr` en `GenerateBase64`. *(#5, #6, D4, D3)*
- **F2 — Núcleo de arquitectura:** `ICfdiComplementHandler` + `ComplementHandlerBase<TModel>` + handlers Carta Porte/Nómina; reescribir `CfdiPdfGenerator` con despacho por namespace (sin `switch`); mover el patch de `LogoBase64` a la base; detección interna por namespace; migrar los 4 tests `CanMap` → detección/handler. *(#1, #2, #3, D2, O2)*
- **F3 — Composition root + fachada:** `CfdiPdfFactory` interno compartido por fachada y `AddCfdiPdfServices` (elimina el grafo `new` manual); licencia idempotente; `ConfigureLogging(ILoggerFactory)`; lifetimes (handler Transient) + `ValidateScopes`/`ValidateOnBuild`; I/O async honesto (`LoadAsync`). *(D1, #4, X3, X4, bug de logging)*
- **F4 — Cierre de superficie + v3:** aplicar `internal`; consolidar namespaces públicos a `CFDI.BuildPdf`; docs XML completas en los ~9 tipos públicos; activar `TreatWarningsAsErrors`. *(O3, O1, X1, X2)*
- **F5 — Packaging, CI/CD, docs:** SourceLink + build determinista + símbolos; GitHub Actions (build→test+coverage→pack→publish); `MIGRATION.md`, `CHANGELOG`, README v3, bump a 3.0.0, rama `release/2.x`. *(pilares Packaging y CI/CD)*

## 9. Testing & CI/CD

**Pirámide de pruebas:**
- **Unit:** mappers aislados con `FakeQrGenerator` (se preservan los ~25 actuales), `SatCatalogos`, `NumberToWordsConverter`, `QrUrlBuilder`, detección por namespace y selección de handler (incl. desempate por prioridad y caso "ninguno aplica").
- **Integración:** XML→`byte[]` de punta a punta por complemento + casos de error (`CfdiXmlInvalidoException`, `CfdiComplementoNoSoportadoException`).
- **Golden / anti-regresión** (pragmático, porque el PDF de QuestPDF no es byte-determinista): (a) snapshot del *ViewModel* mapeado (100% determinista, atrapa regresiones de mapeo) + (b) smoke del PDF (extracción de texto de campos clave + nº de páginas + que no lance).

**CI/CD (GitHub Actions):**
- **PR gate:** build net8 + tests + cobertura + analizadores deben pasar. Política de cobertura en **dos niveles**: piso ~80% global (para no bloquear por DTOs/guardas defensivas/render) y meta ~95% en la **lógica de dominio** (mappers, `SatCatalogos`, detección por namespace, `NumberToWordsConverter`, `QrUrlBuilder`). El número es un piso, no el objetivo: lo que importa es el comportamiento verificado.
- **Release:** tag `v3.0.0` → `pack` (con símbolos) → `publish` a NuGet con API key en secrets.

## 10. Packaging, docs & migración

- **`csproj` hardening:** `Deterministic=true`, `ContinuousIntegrationBuild` en CI, SourceLink (`Microsoft.SourceLink.GitHub`), `EmbedUntrackedSources`, `PublishRepositoryUrl`, `GenerateDocumentationFile=true`, `EnforceCodeStyleInBuild=true`, símbolos `snupkg` (ya activo).
- **Versionado:** `3.0.0`. Opcional: MinVer/Nerdbank.GitVersioning para SemVer automático desde tags (no obligatorio).
- **Docs:** README v3 (namespaces/ejemplos actualizados), `MIGRATION.md` (v2→v3 con antes/después), `CHANGELOG.md` (Keep a Changelog), docs XML en toda la API pública.
- **Release:** tag → CI publica; rama `release/2.x` para parches críticos.

## 11. Riesgos y mitigaciones

- **R1 — Fidelidad del PDF al reubicar el ensamblado del modelo (LogoBase64/opts) a los handlers.** Mitigación: golden tests en F0 antes de mover nada; centralizar el patch en la base del handler.
- **R2 — Detección de tipo al unificar en despacho por namespace.** Mitigación: misma semántica de namespaces; unicidad por llave; tests de detección con XMLs reales.
- **R3 — Licencia QuestPDF global de proceso (coexistencia fachada+DI puede degradar Enterprise→Community).** Mitigación: política idempotente (no pisar una licencia ya configurada); documentar orden canónico.
- **R4 — Breaking changes para consumidores v2.** Mitigación: `MIGRATION.md` exhaustivo; rama `release/2.x`.
- **R5 — Tests existentes que dependen de `CanMap`/visibilidad.** Mitigación: `InternalsVisibleTo` ya existe; migrar los 4 tests `CanMap` a detección/handler en F2.

## 12. Fuera de alcance (v3 inicial)

- Opciones tipadas por complemento (se diseña la costura, se implementa cuando un complemento lo requiera).
- Soporte de complementos adicionales (Pagos 2.0, Comercio Exterior): la arquitectura los habilita, pero su implementación es trabajo posterior.
- Composición de varios complementos en un mismo PDF como "secciones contribuyentes" (evolución futura; v3 elige un handler principal por documento de forma determinista).
- Workstream dedicado de observabilidad más allá de los arreglos derivados de los hallazgos.

## 13. Criterios de aceptación

- Superficie pública reducida a los ~9 tipos del §6, en `CFDI.BuildPdf` / `Microsoft.Extensions.DependencyInjection`.
- Todos los hallazgos D1–D4, O1–O3, X1–X4 resueltos o conscientemente documentados.
- Añadir un complemento nuevo no requiere tocar orquestador, detección ni fachada (solo crear 3 clases + 1 registro).
- Golden tests verdes en cada fase; cobertura ≥ ~80% global y ~95% en la lógica de dominio.
- Build net8.0 determinista, con SourceLink, símbolos y docs XML; CI verde; v3.0.0 publicable.
- `MIGRATION.md`, `CHANGELOG.md` y README v3 presentes y consistentes.
