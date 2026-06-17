# Diseño: Factura base CFDI 4.0 (sin complemento) — Ingreso/Egreso

**Fecha:** 2026-06-17
**Versión objetivo:** 3.1.0 (minor, sin breaking changes)
**Estado:** aprobado para escribir plan de implementación

## Objetivo

Permitir que la librería genere el PDF (representación impresa) de **facturas CFDI 4.0 base** — comprobantes de tipo **Ingreso (`I`)** y **Egreso (`E`)** que **no** llevan complemento Carta Porte ni Nómina. Hoy esos XML lanzan `CfdiComplementoNoSoportadoException`.

XML de referencia: `CFDI.BuildPdf.ConsoleDemo/test/Directas/` (FOAP-7968, FOAP-8008, FOAP-8021 — todos `TipoDeComprobante="I"`).

## Alcance

**Dentro:**
- `TipoDeComprobante` = `I` (Ingreso) y `E` (Egreso / nota de crédito). Mismo layout; solo cambia la etiqueta de tipo.
- Conceptos con impuestos a nivel concepto (Traslados y Retenciones) y desglose/resumen de impuestos a nivel comprobante.
- CFDI Relacionados (nodo opcional, ya soportado por el mapeo base).

**Fuera (lanzan `CfdiComplementoNoSoportadoException` con mensaje claro):**
- `TipoDeComprobante` = `T` (Traslado) y `P` (Pago/REP). El REP requiere su propio layout (complemento `pago20`) y sería una feature aparte. Decisión del usuario: no aplicar un formato genérico a tipos que no encajan.
- No se modifican Carta Porte 3.1 ni Nómina 1.2 (su salida debe quedar idéntica).

## Principio rector

Una factura base = el **cuerpo del comprobante** (encabezado fiscal + cliente/emisión + forma de pago + conceptos + totales + footer fiscal) **sin** secciones de complemento. Ese cuerpo **ya existe** dentro de `CartaPorteDocumentBuilder` y `CartaPorteMapper`, y es genérico de CFDI 4.0. Por tanto la feature es ~80% **extraer y reutilizar** lo genérico (decisión del usuario: "Extraer y compartir") y ~20% código nuevo.

## Decisiones tomadas

1. **Alcance:** Ingreso + Egreso (no fallback universal).
2. **Reutilización:** extraer las secciones genéricas a un lugar compartido, usadas por factura base **y** Carta Porte (DRY). Mitigación de riesgo: verificar que el PDF de Carta Porte queda **idéntico** tras la extracción (comparación de texto/bytes, como en v3).
3. **Despacho:** Enfoque A — predicado `CanHandle(XDocument)`.
4. **Superficie pública:** sin cambios (sigue siendo XML→`byte[]` vía `CfdiPdf` / `AddCfdiPdfServices`). Release **3.1.0** (minor).

---

## Arquitectura

### 1. Despacho: de namespace a predicado `CanHandle`

Hoy `CfdiPdfGenerator.ResolveHandler` selecciona el handler cuyo namespace de complemento aparece en el XML. Se evoluciona el contrato para que cada handler decida con un predicado.

**`ICfdiComplementHandler` (interno):**
- Se reemplaza `IReadOnlyCollection<string> ComplementNamespaces` por `bool CanHandle(XDocument xdoc)`.
- Se conservan `int Priority` y `byte[] Generate(XDocument, CfdiPdfOptions)`.

**Jerarquía de bases de handler** (para no duplicar el flujo mapper→builder→logo ni la lógica de namespace):
- `CfdiHandlerBase<TModel>` (nuevo, abstracto): implementa `Generate` (mapper.Map → aplicar `LogoBase64` → builder.Build), `virtual Priority`, y declara `abstract CanHandle`. Es el extracto del actual `ComplementHandlerBase`.
- `ComplementHandlerBase<TModel> : CfdiHandlerBase<TModel>`: mantiene `abstract ComplementNamespaces` e implementa `CanHandle(xdoc)` = "algún namespace declarado está presente en los descendientes del root". **Carta Porte y Nómina siguen heredando de aquí, sin cambios en sus archivos de handler.**
- `FacturaComplementHandler : CfdiHandlerBase<CfdiFacturaViewModel>` (nuevo): `CanHandle(xdoc)` = `TipoDeComprobante ∈ {I, E}`; `Priority` = el más bajo (constante `int.MinValue`, documentada). Como Carta Porte/Nómina usan `Priority = 0`, el handler de factura solo gana cuando ningún complemento aplica.

**`CfdiPdfGenerator`:**
- `ResolveHandler`: `_handlers.FirstOrDefault(h => h.CanHandle(xdoc))` sobre la lista ya ordenada por `Priority` descendente. (Un CFDI Carta Porte es `TipoDeComprobante="I"`, así que tanto el handler de Carta Porte como el de factura devuelven `true`; gana Carta Porte por prioridad — el orden lo garantiza.)
- Guard de unicidad de namespaces: hoy itera `handler.ComplementNamespaces`. Como la interfaz ya no lo expone, el guard se valida sobre los handlers basados en namespace (vía `OfType<ComplementHandlerBase<...>>` con una propiedad interna, o un pequeño contrato `IComplementNamespacesProvider`). Alternativa aceptable: eliminar el guard, dado que el composition root es interno y cerrado. **Se decide en el plan**; no afecta el comportamiento observable.
- Mensaje de `CfdiComplementoNoSoportadoException` actualizado (ver §6).

### 2. Capa de modelo

- **Mover** (sin cambio de comportamiento) los modelos genéricos `ConceptoViewModel`, `ImpuestoConceptoViewModel`, `RetencionImpuestoViewModel` desde `Models/CfdiCartaPorteViewModel.cs` a un archivo propio `Models/ConceptoViewModel.cs`. Hoy ya viven en el namespace `CFDI.BuildPdf.Models`; solo se reorganizan.
- **Nuevo** `Models/CfdiFacturaViewModel.cs`:
  ```csharp
  internal class CfdiFacturaViewModel : CfdiViewModelBase
  {
      public string CondicionesPago { get; set; }
      public List<ConceptoViewModel> Conceptos { get; set; } = new();
      public decimal TotalImpuestosTrasladados { get; set; }
      public decimal TotalImpuestosRetenidos { get; set; }
      public List<ImpuestoConceptoViewModel> TrasladosResumen { get; set; } = new();
      public List<RetencionImpuestoViewModel> RetencionesResumen { get; set; } = new();
  }
  ```
- **Base intermedia opcional** (`CfdiComprobanteViewModel`) de la que hereden Factura **y** Carta Porte para no repetir esos campos: se adopta **solo si** no altera el golden snapshot (JSON) del VM de Carta Porte. Como el snapshot serializa las mismas propiedades con los mismos valores, mover la declaración a una base no cambia el JSON. Decisión final en el plan; el default es introducirla (más DRY).

### 3. Capa de mapeo

- **Extraer** de `CartaPorteMapper.MapComplemento` el mapeo genérico de **Conceptos** (con Traslados/Retenciones a nivel concepto) y el **resumen de impuestos a nivel comprobante** (`TotalImpuestosTrasladados/Retenidos`, `TrasladosResumen`, `RetencionesResumen`, incluyendo el *fallback* que agrega desde los conceptos cuando no hay nodo global) a un punto compartido:
  - Opción: métodos `protected` en `BaseCfdiMapper<TModel>` (p.ej. `MapConceptos(comprobante)` → `List<ConceptoViewModel>` y `MapResumenImpuestos(comprobante, conceptos, …)`), o un helper estático `ConceptosImpuestosMapper`. Default: métodos `protected` reutilizables en `BaseCfdiMapper`.
  - `CartaPorteMapper` pasa a invocarlos (resultado idéntico, validado por su golden snapshot).
- **Nuevo** `Mappers/Factura/FacturaMapper.cs : BaseCfdiMapper<CfdiFacturaViewModel>`:
  - `CreateModel()` → `new CfdiFacturaViewModel()`.
  - `MapComplemento(xdoc, model)` → mapea `CondicionesPago` + Conceptos + resumen de impuestos (vía los helpers compartidos). No hay nodo de complemento que mapear.

### 4. Capa de render (extraer y compartir)

- **Nuevo** `PdfBuilders/Common/ComprobanteSections.cs` (estático, junto a `CfdiPdfSections`): se **mueven verbatim** desde `CartaPorteDocumentBuilder`, parametrizadas por `CfdiViewModelBase` + los modelos genéricos (no por el VM de Carta Porte):
  - `ComposeClienteYEmision` (cliente + datos de emisión + CFDI relacionados).
  - `ComposeFormaPago` (forma/método de pago, tipo de comprobante, condiciones de pago).
  - `ComposeConceptos` (tabla de conceptos con impuestos trasladados/retenidos por concepto).
  - `ComposeTotales` / panel de totales (subtotal, desglose trasladados/retenidos, total).
  - Los datos que estas secciones consumen (`ReceptorNombre`, `Conceptos`, `TrasladosResumen`, etc.) provienen de `CfdiViewModelBase` y de los modelos genéricos, por lo que las firmas tomarán esos tipos (no `CfdiCartaPorteViewModel`).
- **Refactor** `CartaPorteDocumentBuilder`: sustituye sus métodos privados por llamadas a `ComprobanteSections`. El resto (secciones de Carta Porte, condiciones de contrato) no cambia.
- **Nuevo** `PdfBuilders/Factura/FacturaDocumentBuilder.cs : IPdfDocumentBuilder<CfdiFacturaViewModel>`:
  - Página Letter (respeta `CfdiPdfOptions.Orientacion`), márgenes y estilo iguales a los builders existentes.
  - Orden de secciones: `CfdiPdfSections.ComposeEncabezado` → `ComprobanteSections.ComposeClienteYEmision` → `ComposeFormaPago` → `ComposeConceptos` → `ComposeTotales` → `CfdiPdfSections.ComposeFooterFiscal`.
  - Footer de página igual al de los otros builders ("REPRESENTACIÓN IMPRESA… Página X de Y").

### 5. Composition root / DI

- `CfdiPdfFactory.CreateGenerator`: agregar al arreglo de handlers
  `new FacturaComplementHandler(new FacturaMapper(qrGenerator, loggerFactory?.CreateLogger<FacturaMapper>()), new FacturaDocumentBuilder())`.
- La ruta de DI (`AddCfdiPdfServices`) ya construye el orquestador vía `CfdiPdfFactory`, así que **no requiere cambios adicionales**.

### 6. Manejo de errores

- `CfdiComplementoNoSoportadoException`: mensaje actualizado a algo como
  *"Tipo de CFDI no soportado. La librería soporta factura base de Ingreso (I) y Egreso (E), Carta Porte 3.1 y Nómina 1.2."*
- Traslado (`T`) y Pago (`P`) sin complemento reconocido → siguen lanzando esa excepción (no los cubre `CanHandle` de factura).
- Validaciones de entrada (XML nulo/ inválido, archivo inexistente) sin cambios: ya las maneja `CfdiPdfGenerator`.

### 7. Pruebas

- **Golden (ViewModel snapshot, JSON):** un fixture de factura **Ingreso** y uno de **Egreso** → `Factura.viewmodel.json`, `FacturaEgreso.viewmodel.json`. Fixtures embebidos (patrón `EmbeddedResource` existente). Datos sintéticos o derivados de los XML de `Directas` (sin datos confidenciales en el repo si aplica).
- **PDF smoke:** generar el PDF de factura base y verificar header `%PDF`, ≥1 página, longitud y texto extraído (PdfPig) > umbral.
- **Despacho:** test de que un CFDI `I`/`E` sin complemento resuelve `FacturaComplementHandler`; que Carta Porte/Nómina siguen resolviendo su handler; que `T`/`P` lanzan `CfdiComplementoNoSoportadoException`.
- **Regresión Carta Porte (clave):** confirmar que el PDF de Carta Porte queda **idéntico** tras la extracción de secciones — comparación de texto extraído/bytes contra el baseline previo a la extracción. El golden snapshot del VM de Carta Porte **no debe cambiar**.
- **Validación manual:** ejecutar los 3 XML de `Directas` en ConsoleDemo y que el usuario revise los PDFs (igual que en v3) antes de publicar.

## Preservación de comportamiento

- Carta Porte y Nómina: salida byte-idéntica (extracción sin cambio de lógica; golden snapshots intactos; comparación de PDF de Carta Porte).
- Superficie pública: sin cambios → consumidores actuales no se ven afectados. Es estrictamente aditivo.

## Versionado

- **3.1.0** (minor): feature aditiva, sin breaking changes. Entrada en `CHANGELOG.md`; nota breve en `README.md` (lista de tipos soportados). `MIGRATION.md` no aplica.

## Estructura de archivos (resumen)

| Acción | Archivo |
|---|---|
| Modificar | `Complements/ICfdiComplementHandler.cs` (CanHandle) |
| Crear | `Complements/CfdiHandlerBase.cs` (flujo Generate compartido) |
| Modificar | `Complements/ComplementHandlerBase.cs` (hereda de CfdiHandlerBase + CanHandle por namespace) |
| Crear | `Complements/FacturaComplementHandler.cs` |
| Crear | `Models/ConceptoViewModel.cs` (mover modelos genéricos) |
| Modificar | `Models/CfdiCartaPorteViewModel.cs` (quitar modelos movidos; opcional: heredar de base intermedia) |
| Crear | `Models/CfdiFacturaViewModel.cs` |
| Modificar | `Mappers/Common/BaseCfdiMapper.cs` (helpers de conceptos/impuestos compartidos) |
| Modificar | `Mappers/CartaPorte/CartaPorteMapper.cs` (usar helpers) |
| Crear | `Mappers/Factura/FacturaMapper.cs` |
| Crear | `PdfBuilders/Common/ComprobanteSections.cs` (mover secciones genéricas) |
| Modificar | `PdfBuilders/CartaPorte/CartaPorteDocumentBuilder.cs` (usar ComprobanteSections) |
| Crear | `PdfBuilders/Factura/FacturaDocumentBuilder.cs` |
| Modificar | `Configuration/CfdiPdfFactory.cs` (registrar handler de factura) |
| Modificar | `Services/CfdiPdfGenerator.cs` (despacho por CanHandle + mensaje) |
| Crear | Tests: golden I/E, smoke, despacho, regresión Carta Porte |
| Modificar | `CHANGELOG.md`, `README.md`, `CFDI.BuildPdf.csproj` (Version 3.1.0) |

## Fuera de alcance / futuro

- REP / complemento de Pagos `pago20` (P): feature separada con layout propio.
- Traslado (T): podría añadirse después si surge la necesidad (el cuerpo ya existiría).
- Otros complementos (INE, IEDU, etc.).
