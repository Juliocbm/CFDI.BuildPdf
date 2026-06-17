# CFDI.BuildPdf v3 — Fase F1: reúso / extracción (sin cambiar comportamiento) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Mejorar la reutilización y la separación de responsabilidades **sin cambiar el comportamiento observable**: extraer los catálogos SAT (datos) del render, deduplicar el encabezado/logo/footer entre los dos builders, unificar los dos ViewModels de impuesto idénticos, y colapsar la lógica de QR duplicada. La red de golden tests de F0 es el oráculo: salvo donde se indique, **los baselines existentes NO deben cambiar**.

**Architecture:** Refactors mecánicos validados por golden tests. `SatCatalogos` (nueva clase `internal static`, datos puros sin QuestPDF) recibe los 27 `Nombre*` + `PacsConocidos` que hoy viven en `CfdiPdfSections`; `CfdiPdfSections` se queda solo con render + formato. El encabezado común sube a `CfdiPdfSections.ComposeEncabezado(CfdiViewModelBase)` (todas sus propiedades están en la clase base). Los dos `TrasladoImpuestoViewModel`/`RetencionConceptoViewModel` idénticos se unen en `ImpuestoConceptoViewModel`.

**Tech Stack:** .NET 8.0, xUnit, QuestPDF 2024.3.5, QRCoder 1.6.0, PdfPig 0.1.9 (tests).

**Spec:** `docs/superpowers/specs/2026-06-16-cfdi-buildpdf-v3-enterprise-design.md` (F1). **Base:** tag `f0-baseline`. **Rama:** `Refactor` (commits locales; NO push hasta autorización del usuario).

**Invariante de F1 (repetir en cada refactor):** tras cada cambio, correr `dotnet test CFDI.BuildPdf.sln` → **todo verde** y `git status --porcelain` NO debe mostrar ningún `*.viewmodel.json` modificado (salvo en la Task 2, que AÑADE baselines nuevos). Si un baseline cambia, es un cambio de comportamiento no intencionado → investigar antes de continuar.

---

## File Structure (F1)

- Create: `CFDI.BuildPdf.Tests/Golden/TestCulture.cs` — module initializer que fija cultura invariante en los tests.
- Create: `CFDI.BuildPdf.Tests/TestData/cfdi_cartaporte_retenciones.xml`, `cfdi_nomina_incapacidades.xml` — fixtures de casos límite.
- Modify: `CFDI.BuildPdf.Tests/Helpers/TestXmlLoader.cs` — loaders para los nuevos fixtures.
- Modify: `CFDI.BuildPdf.Tests/Golden/ViewModelSnapshotTests.cs` y `PdfSmokeTests.cs` — tests para los nuevos fixtures (+ baselines generados).
- Modify: `CFDI.BuildPdf/Helpers/QrGeneratorService.cs` — colapsar `GenerateQr` en `GenerateBase64`.
- Create: `CFDI.BuildPdf/Models/ImpuestoConceptoViewModel.cs` (o dentro de `CfdiCartaPorteViewModel.cs`) — tipo unificado.
- Modify: `CFDI.BuildPdf/Models/CfdiCartaPorteViewModel.cs`, `Mappers/CartaPorte/CartaPorteMapper.cs`, `PdfBuilders/CartaPorte/CartaPorteDocumentBuilder.cs` — repuntar al tipo unificado.
- Create: `CFDI.BuildPdf/Catalogs/SatCatalogos.cs` — catálogos SAT extraídos.
- Modify: `CFDI.BuildPdf/PdfBuilders/Common/CfdiPdfSections.cs` — quitar catálogos, añadir `ComposeEncabezado`/`FiscalRow`/`TryDecodeLogo`.
- Modify: `CFDI.BuildPdf/PdfBuilders/CartaPorte/CartaPorteDocumentBuilder.cs`, `PdfBuilders/Nomina/NominaDocumentBuilder.cs` — usar el encabezado compartido; repuntar catálogos a `SatCatalogos`.

---

## Task 1: Fijar la cultura en los tests (determinismo)

**Files:**
- Create: `CFDI.BuildPdf.Tests/Golden/TestCulture.cs`

- [ ] **Step 1: Crear el module initializer de cultura**

Crear `CFDI.BuildPdf.Tests/Golden/TestCulture.cs`:
```csharp
using System.Globalization;
using System.Runtime.CompilerServices;

namespace CFDI.BuildPdf.Tests.Golden
{
    /// <summary>
    /// Fija una cultura invariante para todos los tests del ensamblado, de modo que
    /// el formato de números/fechas sea reproducible entre máquinas de desarrollo y CI.
    /// </summary>
    internal static class TestCulture
    {
        [ModuleInitializer]
        internal static void Init()
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        }
    }
}
```

- [ ] **Step 2: Correr la suite completa y confirmar que NADA cambia**

Run: `dotnet test CFDI.BuildPdf.sln`
Expected: `Passed!` (66 tests). Luego:
Run: `git status --porcelain`
Expected: solo aparece el archivo nuevo `TestCulture.cs` (sin `??`/`M` en ningún `*.viewmodel.json`). Si algún baseline cambió, significa que había una dependencia oculta de cultura: PARAR y reportar el diff (no regenerar a ciegas).

- [ ] **Step 3: Commit**

```bash
git add CFDI.BuildPdf.Tests/Golden/TestCulture.cs
git commit -m "test: fijar cultura invariante en los tests para determinismo (F1)"
```

---

## Task 2: Fixtures de casos límite + baselines

**Files:**
- Create: `CFDI.BuildPdf.Tests/TestData/cfdi_cartaporte_retenciones.xml`
- Create: `CFDI.BuildPdf.Tests/TestData/cfdi_nomina_incapacidades.xml`
- Modify: `CFDI.BuildPdf.Tests/Helpers/TestXmlLoader.cs`
- Modify: `CFDI.BuildPdf.Tests/Golden/ViewModelSnapshotTests.cs`, `PdfSmokeTests.cs`

Objetivo: cubrir, ANTES de refactorizar, ramas que hoy no se ejercitan y que F1 toca al renderizar/mapear: en Carta Porte **Retenciones a nivel concepto** + **CfdiRelacionados**; en Nómina **Incapacidades** + **HorasExtra**. (Se DEFIEREN `RegimenesAduaneros`/`DocumentosAduaneros` porque hoy ni siquiera se mapean — añadirían XML que mapea a listas vacías; son trabajo de una fase posterior con mapeo nuevo.)

- [ ] **Step 1: Crear el fixture de Carta Porte con Retenciones + CfdiRelacionados**

Partir de `CFDI.BuildPdf.Tests/TestData/cfdi_cartaporte.xml` (léelo). Para saber EXACTAMENTE qué nodos/atributos leen los mappers, lee `CFDI.BuildPdf/Mappers/CartaPorte/CartaPorteMapper.cs` (las Retenciones de concepto se leen en líneas 71-78: `cfdi:Impuestos/cfdi:Retenciones/cfdi:Retencion` con atributos `Impuesto`, `TipoFactor`, `TasaOCuota`, `Base`, `Importe`) y `CFDI.BuildPdf/Mappers/Common/BaseCfdiMapper.cs` (CfdiRelacionados: `cfdi:CfdiRelacionados` con `TipoRelacion` y `cfdi:CfdiRelacionado UUID`).
Crea `CFDI.BuildPdf.Tests/TestData/cfdi_cartaporte_retenciones.xml` = copia del fixture base + (a) un nodo `cfdi:CfdiRelacionados TipoRelacion="04"` con 2 `cfdi:CfdiRelacionado` (UUIDs válidos cualesquiera), (b) en al menos un `cfdi:Concepto`, un `cfdi:Impuestos/cfdi:Retenciones/cfdi:Retencion` (p. ej. ISR, `Impuesto="001"`, `TipoFactor="Tasa"`, `TasaOCuota="0.100000"`, `Base` e `Importe` coherentes). Mantén el resto del XML válido.

- [ ] **Step 2: Crear el fixture de Nómina con Incapacidades + HorasExtra**

Lee `CFDI.BuildPdf/Mappers/Nomina/NominaMapper.cs` para los nombres exactos de nodos del complemento Nómina (`nomina12:Incapacidades/Incapacidad` y `nomina12:HorasExtra` bajo una `Percepcion`). Crea `CFDI.BuildPdf.Tests/TestData/cfdi_nomina_incapacidades.xml` = copia de `cfdi_nomina.xml` + (a) `nomina12:Incapacidades` con 2 `nomina12:Incapacidad` (DiasIncapacidad/TipoIncapacidad/ImporteMonetario), (b) bajo una `nomina12:Percepcion`, un `nomina12:HorasExtra` (Dias/TipoHoras/HorasExtra/ImportePagado). Mantén el XML válido.

- [ ] **Step 3: Añadir loaders**

En `CFDI.BuildPdf.Tests/Helpers/TestXmlLoader.cs`, junto a `LoadCartaPorte()`/`LoadNomina()`, añade:
```csharp
        public static XDocument LoadCartaPorteRetenciones()
            => Load("CFDI.BuildPdf.Tests.TestData.cfdi_cartaporte_retenciones.xml");

        public static XDocument LoadNominaIncapacidades()
            => Load("CFDI.BuildPdf.Tests.TestData.cfdi_nomina_incapacidades.xml");
```
(Los `.xml` bajo `TestData/**` ya se embeben automáticamente vía el `EmbeddedResource` del csproj — no hace falta tocar el csproj.)

- [ ] **Step 4: Añadir snapshots de ViewModel para los nuevos fixtures**

En `CFDI.BuildPdf.Tests/Golden/ViewModelSnapshotTests.cs`, añade dos `[Fact] [Trait("Category","Golden")]` siguiendo el patrón existente: uno que mapee `LoadCartaPorteRetenciones()` con `CartaPorteMapper` → `Snapshot.Match(json, "CartaPorteRetenciones.viewmodel.json")`, y otro que mapee `LoadNominaIncapacidades()` con `NominaMapper` → `Snapshot.Match(json, "NominaIncapacidades.viewmodel.json")`. Reusa el `JsonOpts` compartido.

- [ ] **Step 5: Generar e inspeccionar baselines (primera corrida falla a propósito)**

Run: `dotnet test CFDI.BuildPdf.Tests/CFDI.BuildPdf.Tests.csproj --filter "Category=Golden"`
Expected: los 2 nuevos fallan creando sus baselines. ABRE `CartaPorteRetenciones.viewmodel.json` y `NominaIncapacidades.viewmodel.json` y CONFIRMA que las ramas objetivo están pobladas: en Carta Porte el array `Retenciones` de un Concepto tiene 1 elemento y `TipoRelacion`/`RelacionadosUuids` están poblados; en Nómina `Incapacidades` tiene 2 elementos y alguna `Percepcion` tiene `HorasExtra`. Si están vacíos, el XML no golpeó el path esperado → corrige el XML (no el baseline) y repite.

- [ ] **Step 6: Añadir smoke de PDF para los nuevos fixtures**

En `CFDI.BuildPdf.Tests/Golden/PdfSmokeTests.cs`, añade dos `[Fact] [Trait("Category","Golden")]` espejo de los existentes, generando con `CfdiPdf.DesdeXmlStringAsync(TestXmlLoader.LoadCartaPorteRetenciones().ToString())` y `...LoadNominaIncapacidades()...`, con las mismas aserciones (`%PDF`, length>1000, pages>=1, texto>200).

- [ ] **Step 7: Re-correr y confirmar verde**

Run: `dotnet test CFDI.BuildPdf.Tests/CFDI.BuildPdf.Tests.csproj --filter "Category=Golden"`
Expected: PASS (6 golden: 3 snapshot + 3 smoke... en realidad 4 snapshot + 4 smoke contando los nuevos). Confirma que los baselines viejos (`CartaPorte.viewmodel.json`, `Nomina.viewmodel.json`) NO cambiaron (`git status --porcelain`).

- [ ] **Step 8: Commit**

```bash
git add CFDI.BuildPdf.Tests/TestData/cfdi_cartaporte_retenciones.xml CFDI.BuildPdf.Tests/TestData/cfdi_nomina_incapacidades.xml CFDI.BuildPdf.Tests/Helpers/TestXmlLoader.cs CFDI.BuildPdf.Tests/Golden/ViewModelSnapshotTests.cs CFDI.BuildPdf.Tests/Golden/PdfSmokeTests.cs CFDI.BuildPdf.Tests/Golden/Snapshots/CartaPorteRetenciones.viewmodel.json CFDI.BuildPdf.Tests/Golden/Snapshots/NominaIncapacidades.viewmodel.json
git commit -m "test: fixtures y golden de casos límite (Retenciones, CfdiRelacionados, Incapacidades, HorasExtra) (F1)"
```

---

## Task 3: Colapsar `GenerateQr` en `GenerateBase64` (D3)

**Files:**
- Modify: `CFDI.BuildPdf/Helpers/QrGeneratorService.cs`

- [ ] **Step 1: Confirmar que `GenerateQr` solo lo usa `GenerateBase64`**

Run: `git grep -n "GenerateQr" -- "CFDI.BuildPdf/**" "CFDI.BuildPdf.Tests/**"`
Expected: las únicas referencias están dentro de `QrGeneratorService.cs` (la definición estática y la llamada desde `GenerateBase64`). Si hay otra referencia, PARAR y reportar.

- [ ] **Step 2: Inlinear el cuerpo y eliminar el método estático**

Reemplazar el contenido de la clase `QrGeneratorService` para que quede UN solo método (mover el cuerpo de `GenerateQr` dentro de `GenerateBase64` y borrar `GenerateQr`):
```csharp
        /// <inheritdoc />
        public string GenerateBase64(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);

            var qrCodeBytes = qrCode.GetGraphic(20);
            return Convert.ToBase64String(qrCodeBytes);
        }
```
(Eliminar el método `public static string GenerateQr(...)` y su comentario de "backward-compatibility".)

- [ ] **Step 3: Build + golden**

Run: `dotnet test CFDI.BuildPdf.sln`
Expected: todo verde; `git status --porcelain` sin cambios en baselines (el QR real solo se ejercita en smoke, que sigue produciendo un PDF válido; los snapshots usan `FakeQrGenerator`).

- [ ] **Step 4: Commit**

```bash
git add CFDI.BuildPdf/Helpers/QrGeneratorService.cs
git commit -m "refactor: colapsar GenerateQr en GenerateBase64 (D3) (F1)"
```

---

## Task 4: Unificar los ViewModels de impuesto en `ImpuestoConceptoViewModel` (D4)

**Files:**
- Modify: `CFDI.BuildPdf/Models/CfdiCartaPorteViewModel.cs`
- Modify: `CFDI.BuildPdf/Mappers/CartaPorte/CartaPorteMapper.cs`
- Modify: `CFDI.BuildPdf/PdfBuilders/CartaPorte/CartaPorteDocumentBuilder.cs`

Contexto verificado: `TrasladoImpuestoViewModel` (líneas 68-75) y `RetencionConceptoViewModel` (77-84) son idénticos (`Impuesto`, `TipoFactor`, `TasaOCuota`, `Base`, `Importe`). Nadie ramifica por el tipo concreto. **NO tocar `RetencionImpuestoViewModel` (86-90, solo `Impuesto`+`Importe`)** — es un tipo distinto del resumen a nivel comprobante. Como System.Text.Json serializa propiedades (no el nombre del tipo), **los baselines no cambian**.

- [ ] **Step 1: Crear el tipo unificado y eliminar los dos idénticos**

En `CFDI.BuildPdf/Models/CfdiCartaPorteViewModel.cs`, reemplazar las clases `TrasladoImpuestoViewModel` (68-75) y `RetencionConceptoViewModel` (77-84) por una sola:
```csharp
    /// <summary>
    /// Impuesto a nivel concepto (traslado o retención). Mismas 5 columnas en el PDF.
    /// </summary>
    public class ImpuestoConceptoViewModel
    {
        public string Impuesto { get; set; }       // 001 ISR, 002 IVA, 003 IEPS
        public string TipoFactor { get; set; }     // Tasa, Cuota, Exento
        public decimal TasaOCuota { get; set; }
        public decimal Base { get; set; }
        public decimal Importe { get; set; }
    }
```
(Conserva los modificadores `string` exactamente como estaban — si el proyecto usa `string` no anulable con `= null!` o similar, replícalo igual que las clases vecinas para no introducir warnings nuevos.)

- [ ] **Step 2: Repuntar las propiedades que referencian los tipos viejos**

En el mismo archivo: `ConceptoViewModel.Traslados` (línea 64) `List<TrasladoImpuestoViewModel>` → `List<ImpuestoConceptoViewModel>`; `ConceptoViewModel.Retenciones` (65) `List<RetencionConceptoViewModel>` → `List<ImpuestoConceptoViewModel>`; `CfdiCartaPorteViewModel.TrasladosResumen` (26) `List<TrasladoImpuestoViewModel>` → `List<ImpuestoConceptoViewModel>`. (NO tocar `RetencionesResumen` en línea 32, que usa `RetencionImpuestoViewModel`.)

- [ ] **Step 3: Repuntar el mapper**

En `CFDI.BuildPdf/Mappers/CartaPorte/CartaPorteMapper.cs`, cambiar los `new TrasladoImpuestoViewModel`/`new RetencionConceptoViewModel` y cualquier `List<...>` de esos tipos por `ImpuestoConceptoViewModel` en los sitios: ~60-67 (Traslados de concepto), ~71-78 (Retenciones de concepto), ~86-96 y ~108-124 (TrasladosResumen). Usa `git grep -n "TrasladoImpuestoViewModel\|RetencionConceptoViewModel" -- CFDI.BuildPdf` para encontrarlos todos.

- [ ] **Step 4: Repuntar el builder**

En `CFDI.BuildPdf/PdfBuilders/CartaPorte/CartaPorteDocumentBuilder.cs`, cambiar los tipos en los loops de render (~350-377 Traslados, ~379-406 Retenciones, ~459-472 TrasladosResumen) a `ImpuestoConceptoViewModel`.

- [ ] **Step 5: Confirmar que no queda ninguna referencia a los tipos viejos**

Run: `git grep -n "TrasladoImpuestoViewModel\|RetencionConceptoViewModel" -- CFDI.BuildPdf CFDI.BuildPdf.Tests`
Expected: SIN salida (los dos tipos viejos ya no existen ni se referencian). `RetencionImpuestoViewModel` SÍ debe seguir existiendo (no confundir).

- [ ] **Step 6: Build + golden (baselines NO cambian)**

Run: `dotnet test CFDI.BuildPdf.sln`
Expected: todo verde. `git status --porcelain` → ningún `*.viewmodel.json` modificado (la serialización es idéntica). Si algún baseline cambia, PARAR e investigar.

- [ ] **Step 7: Commit**

```bash
git add CFDI.BuildPdf/Models/CfdiCartaPorteViewModel.cs CFDI.BuildPdf/Mappers/CartaPorte/CartaPorteMapper.cs CFDI.BuildPdf/PdfBuilders/CartaPorte/CartaPorteDocumentBuilder.cs
git commit -m "refactor: unificar Traslado/RetencionConceptoViewModel en ImpuestoConceptoViewModel (D4) (F1)"
```

---

## Task 5: Extraer `TryDecodeLogo` a `CfdiPdfSections` (dedup idéntico)

**Files:**
- Modify: `CFDI.BuildPdf/PdfBuilders/Common/CfdiPdfSections.cs`
- Modify: `CFDI.BuildPdf/PdfBuilders/CartaPorte/CartaPorteDocumentBuilder.cs`
- Modify: `CFDI.BuildPdf/PdfBuilders/Nomina/NominaDocumentBuilder.cs`

Contexto: `TryDecodeLogo` es byte-idéntico en ambos builders (CartaPorte 850-863, Nómina 534-547). Firma: `(string logoBase64, ILogger logger, out byte[]? logoBytes)` → `bool`.

- [ ] **Step 1: Mover `TryDecodeLogo` a `CfdiPdfSections`**

Copia el cuerpo idéntico de `TryDecodeLogo` a `CfdiPdfSections` como `internal static bool TryDecodeLogo(string logoBase64, ILogger logger, out byte[]? logoBytes)` (lee el cuerpo real de uno de los builders para copiarlo exacto). Añade los `using` que necesite (`Microsoft.Extensions.Logging`).

- [ ] **Step 2: Eliminar las copias locales y repuntar las llamadas**

Borra el método `TryDecodeLogo` de ambos builders y cambia sus llamadas a `CfdiPdfSections.TryDecodeLogo(...)`.

- [ ] **Step 3: Build + golden**

Run: `dotnet test CFDI.BuildPdf.sln`
Expected: verde; baselines sin cambios (el logo se decodifica igual; los fixtures no traen logo, así que el path es el mismo `false`).

- [ ] **Step 4: Commit**

```bash
git add CFDI.BuildPdf/PdfBuilders/Common/CfdiPdfSections.cs CFDI.BuildPdf/PdfBuilders/CartaPorte/CartaPorteDocumentBuilder.cs CFDI.BuildPdf/PdfBuilders/Nomina/NominaDocumentBuilder.cs
git commit -m "refactor: extraer TryDecodeLogo común a CfdiPdfSections (F1)"
```

---

## Task 6: Extraer el encabezado compartido `ComposeEncabezado` + `FiscalRow`

**Files:**
- Modify: `CFDI.BuildPdf/PdfBuilders/Common/CfdiPdfSections.cs`
- Modify: `CFDI.BuildPdf/PdfBuilders/CartaPorte/CartaPorteDocumentBuilder.cs`
- Modify: `CFDI.BuildPdf/PdfBuilders/Nomina/NominaDocumentBuilder.cs`

Contexto: el bloque encabezado (logo + emisor + datos fiscales) es casi idéntico — CartaPorte en `ComposeLogoYCertificados` (106-172), Nómina en `ComposeEncabezado` (79-145). Todas las propiedades usadas (`EmisorNombre`, `EmisorRFC`, `EmisorRegimenFiscal`, `LugarExpedicion`, `UUID`, `FechaCertificacion`, `NoCertificadoSAT`, `NoCertificadoEmisor`, `RfcProvCertif`, `Version`) están en `CfdiViewModelBase`, así que un método compartido tipado a la BASE funciona para ambos. `FiscalRow` es byte-idéntico (CartaPorte 174-185, Nómina 147-158).

- [ ] **Step 1: Crear `ComposeEncabezado` y `FiscalRow` en `CfdiPdfSections`**

Añade a `CfdiPdfSections`:
- `public static void ComposeEncabezado(IContainer container, CfdiViewModelBase model)` — copia el cuerpo de render del encabezado de uno de los builders (usa el de Nómina `ComposeEncabezado` 79-145 como base por estar completo en un solo método), reemplazando el tipo del modelo por `CfdiViewModelBase`. Usa `CfdiPdfSections.TryDecodeLogo` (ya movido en Task 5), `NombreRegimenFiscal` y `NombrePac` (siguen en `CfdiPdfSections` en este punto; se repuntan a `SatCatalogos` en Task 7) y `FiscalRow`.
- `private static void FiscalRow(ColumnDescriptor col, string label, string? value)` — copia el cuerpo idéntico.

Verifica leyendo AMBOS bloques que el render es equivalente (mismo orden de filas/labels). Si hay alguna diferencia real entre las dos versiones (no solo el nombre/typed model), documenta y resuélvela conservando el output del fixture que ya esté cubierto.

- [ ] **Step 2: Repuntar ambos builders al encabezado compartido**

- En `NominaDocumentBuilder`: reemplaza la llamada a su `ComposeEncabezado` por `CfdiPdfSections.ComposeEncabezado(c, model)` y borra el método local `ComposeEncabezado` + `FiscalRow`.
- En `CartaPorteDocumentBuilder`: donde llama a `ComposeEncabezado`/`ComposeLogoYCertificados`, reemplaza por `CfdiPdfSections.ComposeEncabezado(c, model)`; borra el stub vacío `ComposeEncabezado` (100-104), el método `ComposeLogoYCertificados` (106-172) y `FiscalRow` (174-185).

- [ ] **Step 3: Build + golden — VIGILAR los baselines de PDF**

Run: `dotnet test CFDI.BuildPdf.sln`
Expected: verde. Los snapshots de ViewModel no cambian (esto es solo render). Los smoke de PDF deben seguir pasando. Como el encabezado es lo único que se deduplica y el bloque era "casi idéntico", revisa que ambos PDFs sigan generándose; si el smoke de alguno baja de los umbrales o falla, compara el render del encabezado viejo vs nuevo y reconcilia.

- [ ] **Step 4: Commit**

```bash
git add CFDI.BuildPdf/PdfBuilders/Common/CfdiPdfSections.cs CFDI.BuildPdf/PdfBuilders/CartaPorte/CartaPorteDocumentBuilder.cs CFDI.BuildPdf/PdfBuilders/Nomina/NominaDocumentBuilder.cs
git commit -m "refactor: extraer encabezado fiscal común a CfdiPdfSections.ComposeEncabezado (F1)"
```

---

## Task 7: Extraer `SatCatalogos` (separar datos SAT del render)

**Files:**
- Create: `CFDI.BuildPdf/Catalogs/SatCatalogos.cs`
- Modify: `CFDI.BuildPdf/PdfBuilders/Common/CfdiPdfSections.cs`
- Modify: callers (`PdfBuilders/**`, y cualquier `Mappers/**` que use `CfdiPdfSections.Nombre*`)

Contexto: en `CfdiPdfSections` los miembros de líneas 152-1018 son catálogos SAT puros (sin QuestPDF): 27 métodos `Nombre*` (`NombreTipoComprobante` … `NombreTipoHoras`) + el diccionario privado `PacsConocidos` (300-307, usado por `NombrePac`). Se mueven a `SatCatalogos`. Se QUEDAN en `CfdiPdfSections`: los 5 render (18-122) y los format-helpers (`FormatCurrency`, `Format6`, `Format2`, `FormatTasaOCuota`) + el `ComposeEncabezado`/`FiscalRow`/`TryDecodeLogo` añadidos en Tasks 5-6.

- [ ] **Step 1: Crear `SatCatalogos` con los catálogos movidos**

Crear `CFDI.BuildPdf/Catalogs/SatCatalogos.cs` con `namespace CFDI.BuildPdf.Catalogs` y `internal static class SatCatalogos`. Mueve ahí (cuerpo idéntico) los 27 métodos `Nombre*` y el diccionario privado `PacsConocidos` (que usa `NombrePac`). NO muevas los format-helpers ni el render. Conserva los modificadores de acceso (`public static`) tal cual estaban.

- [ ] **Step 2: Quitar esos miembros de `CfdiPdfSections`**

Borra de `CfdiPdfSections.cs` los 27 `Nombre*` y `PacsConocidos` ya movidos. Añade `using CFDI.BuildPdf.Catalogs;` a `CfdiPdfSections.cs` y repunta dentro de él cualquier llamada a `Nombre*`/`NombrePac` (p. ej. en `ComposeEncabezado` y `ComposeFooterFiscal`) a `SatCatalogos.Nombre*`.

- [ ] **Step 3: Repuntar todos los callers externos**

Run: `git grep -n "CfdiPdfSections\.\(Nombre\|NombrePac\)" -- CFDI.BuildPdf`
Para cada archivo que aparezca (builders y/o mappers), añade `using CFDI.BuildPdf.Catalogs;` y cambia `CfdiPdfSections.NombreX(` → `SatCatalogos.NombreX(`. Repite el grep hasta que dé vacío.

- [ ] **Step 4: Confirmar separación limpia**

Run: `git grep -n "QuestPDF" -- CFDI.BuildPdf/Catalogs/SatCatalogos.cs`
Expected: SIN salida (los catálogos no dependen de QuestPDF).

- [ ] **Step 5: Build + golden**

Run: `dotnet test CFDI.BuildPdf.sln`
Expected: todo verde; baselines sin cambios (solo se reubicaron métodos puros, cuerpos idénticos).

- [ ] **Step 6: Commit**

```bash
git add CFDI.BuildPdf/Catalogs/SatCatalogos.cs CFDI.BuildPdf/PdfBuilders/Common/CfdiPdfSections.cs CFDI.BuildPdf/PdfBuilders/CartaPorte/CartaPorteDocumentBuilder.cs CFDI.BuildPdf/PdfBuilders/Nomina/NominaDocumentBuilder.cs
git commit -m "refactor: extraer catálogos SAT a SatCatalogos (separar datos del render) (F1)"
```

---

## Task 8: Verificación completa de F1

**Files:** ninguno (verificación).

- [ ] **Step 1: Suite completa con cobertura**

Run: `dotnet test CFDI.BuildPdf.sln --collect:"XPlat Code Coverage"`
Expected: `Passed!` — todos los tests (66 de F0 + los 4 golden nuevos de Task 2 = ~70). 0 fallos.

- [ ] **Step 2: Confirmar que los baselines originales NO cambiaron**

Run: `git log --oneline f0-baseline..HEAD -- "CFDI.BuildPdf.Tests/Golden/Snapshots/CartaPorte.viewmodel.json" "CFDI.BuildPdf.Tests/Golden/Snapshots/Nomina.viewmodel.json"`
Expected: SIN salida (los baselines happy-path de F0 nunca se modificaron durante F1; solo se añadieron los nuevos). Si aparece algún commit, revisar que el cambio fue intencional y justificado.

- [ ] **Step 3: Repo limpio + tag de checkpoint**

Run: `git status --porcelain` (esperado: vacío).
```bash
git tag -f f1-reuso -m "F1: reúso/extracción (SatCatalogos, encabezado común, ImpuestoConceptoViewModel, GenerateQr)"
```

---

## Self-Review

**Spec coverage (F1):**
- Extraer `SatCatalogos` de `CfdiPdfSections` → Task 7 ✅
- Subir encabezado/logo/footer común → Tasks 5 (TryDecodeLogo), 6 (ComposeEncabezado/FiscalRow) ✅
- Unificar ViewModels de impuesto → Task 4 ✅
- Colapsar `GenerateQr` → Task 3 ✅
- Carry-forward F0: cultura fija → Task 1 ✅; fixtures de casos límite → Task 2 ✅ (con `RegimenesAduaneros`/`DocumentosAduaneros` explícitamente diferidos por no estar mapeados).

**Placeholder scan:** los refactors (mover/renombrar) se especifican con miembros exactos + rangos de línea del mapeo + repunte por `git grep`, validados por golden; el código NUEVO (SatCatalogos shell, ImpuestoConceptoViewModel, TestCulture, loaders) va literal. Sin TBD/TODO. ✅

**Consistencia:** `ImpuestoConceptoViewModel` (Task 4) se usa idéntico en mapper y builder; `CfdiPdfSections.ComposeEncabezado(IContainer, CfdiViewModelBase)` y `TryDecodeLogo`/`FiscalRow` se definen en Task 5-6 y se consumen ahí mismo; `SatCatalogos.Nombre*` (Task 7) repunta todos los callers vía grep. El orden Task 6 (encabezado, aún llama `CfdiPdfSections.Nombre*`) → Task 7 (mueve a `SatCatalogos` y repunta) es deliberado. ✅

**Riesgo principal:** Task 6 (encabezado) es el único con riesgo de cambio de render; mitigado porque el bloque era "casi idéntico" (solo difería nombre de método y tipo del modelo, ambos resueltos con la clase base) y porque el encabezado siempre se ejercita en los smoke. Tasks 3,4,5,7 son reubicación/renombrado mecánico, snapshot-preserving.

**Notas para fases siguientes:**
- En F2 (registro de complementos por namespace), `ComposeEncabezado` ya compartido facilita que un complemento nuevo reutilice el encabezado.
- Fixtures de `RegimenesAduaneros`/`DocumentosAduaneros` quedan pendientes para cuando se añada su mapeo (no es F1).

## Carry-forward de la ejecución de F1

- **BUG real (pre-existente) en `NumberToWordsConverter`:** para montos como 12,500.00 produce "DOCE MIL QUINIENTOS **CERO** PESOS 00/100 MXN" (el "CERO" sobra; debería ser "DOCE MIL QUINIENTOS PESOS..."). Detectado al generar el baseline de `NominaIncapacidades`. Es un cambio de **comportamiento**, así que NO entró en F1 (que es behavior-preserving). Arreglarlo en su propia tarea/bugfix con su test (y regenerar el baseline afectado intencionalmente). Está congelado en `NominaIncapacidades.viewmodel.json` como oráculo del estado actual.
- **`ImpuestoConceptoViewModel`** ya es el tipo unificado de impuesto a nivel concepto; al volver los modelos `internal` en F4, recordar que `RetencionImpuestoViewModel` (resumen comprobante, solo Impuesto+Importe) es un tipo DISTINTO que debe permanecer.
- **`SatCatalogos`** (namespace `CFDI.BuildPdf.Catalogs`) queda como datos puros testeables; en F4/testing reforzado conviene añadir tests unitarios directos de los catálogos (hoy se cubren indirectamente vía snapshots).
- Warnings CA1305/CA1860/CA1861/CA1848 en builders siguen pendientes (se endurecen en F4).
