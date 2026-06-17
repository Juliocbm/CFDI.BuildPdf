# CFDI.BuildPdf v3 — Fase F4: cierre de superficie pública + quality gate — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reducir la superficie pública de ~45 a ~9 tipos (caja cerrada), consolidar los namespaces públicos en `CFDI.BuildPdf`, completar la documentación XML y endurecer el build — **sin cambiar el PDF generado** (golden tests son el oráculo).

**Architecture:** Lo público queda en el namespace raíz `CFDI.BuildPdf`: `CfdiPdf` (fachada), `ICfdiPdfGenerator`, `CfdiPdfOptions`+`PdfOrientation`, `CfdiPdfLicenseType`, y la jerarquía de excepciones. La extensión DI va a `Microsoft.Extensions.DependencyInjection` (convención .NET). Todo lo demás (`Models`, `ICfdiModelMapper<T>`, `IPdfDocumentBuilder<T>`, `IQrGenerator`, `ICfdiTypeDetector`, `CfdiType`, mappers, builders, handlers, catálogos, QR, factory, generador) pasa a `internal`. `InternalsVisibleTo CFDI.BuildPdf.Tests` ya existe → los tests siguen compilando.

**Tech Stack:** .NET 8.0, xUnit, QuestPDF, PdfPig (tests).

**Spec:** `docs/superpowers/specs/2026-06-16-cfdi-buildpdf-v3-enterprise-design.md` (§6/§7, F4). **Base:** tag `f3-composition`. **Rama:** `Refactor` (commits LOCALES; NO push hasta autorización del usuario).

**Invariante de F4:** tras cada tarea, `dotnet test CFDI.BuildPdf.sln` → todo verde y `git status --porcelain` sin `*.viewmodel.json` modificado. La internalización/renombrado de namespaces es cambio de visibilidad/organización, no de comportamiento: el compilador caza referencias rotas y los golden tests cuidan el render.

**Por qué es seguro internalizar:** ningún tipo PÚBLICO expone un tipo que se vuelve internal. Verificado: `CfdiPdf`/`ICfdiPdfGenerator` usan solo `string`/`byte[]`/`Stream`/`CfdiPdfOptions` (público) → `Task<byte[]>`; `CfdiPdfOptions` usa `bool`/`string`/`PdfOrientation` (público); las excepciones solo llevan mensajes. No habrá errores de accesibilidad inconsistente (CS0051/CS0050).

---

## File Structure (F4)

- Modify (→ `internal`): `Abstractions/ICfdiModelMapper.cs`, `Abstractions/IPdfDocumentBuilder.cs`, `Abstractions/IQrGenerator.cs`, `Abstractions/ICfdiTypeDetector.cs`, `Abstractions/CfdiType.cs`.
- Modify (→ `internal`, todas las clases): `Models/CfdiViewModelBase.cs`, `Models/CfdiCartaPorteViewModel.cs`, `Models/CfdiNominaViewModel.cs`.
- Modify (quitar registro DI): `Configuration/ServiceCollectionExtensions.cs` (dropear `ICfdiTypeDetector`).
- Modify (namespace → `CFDI.BuildPdf`): `Service/CfdiPdf.cs`, `Abstractions/ICfdiPdfGenerator.cs`, `Abstractions/CfdiPdfOptions.cs`, `Abstractions/CfdiPdfLicenseType.cs`, `Abstractions/CfdiPdfException.cs`.
- Modify (namespace → `Microsoft.Extensions.DependencyInjection`): `Configuration/ServiceCollectionExtensions.cs`.
- Modify (usings, guiado por compilador): cualquier archivo de `CFDI.BuildPdf` y `CFDI.BuildPdf.Tests` que referencie los tipos movidos.
- Modify: `CFDI.BuildPdf/CFDI.BuildPdf.csproj` (`GenerateDocumentationFile`, `TreatWarningsAsErrors`).
- Create: `CFDI.BuildPdf.Tests/InvalidXmlTests.cs`.

---

## Task 1: Internalizar contratos internos + `CfdiType` + dropear registro DI de `ICfdiTypeDetector`

**Files:** `Abstractions/ICfdiModelMapper.cs`, `IPdfDocumentBuilder.cs`, `IQrGenerator.cs`, `ICfdiTypeDetector.cs`, `CfdiType.cs`; `Configuration/ServiceCollectionExtensions.cs`.

- [ ] **Step 1: Cambiar a `internal` los 5 tipos**
En cada archivo, cambiar el modificador del tipo de `public` a `internal` (NO mover de namespace en esta tarea; siguen en `CFDI.BuildPdf.Abstractions`):
- `ICfdiModelMapper.cs`: `public interface ICfdiModelMapper<TModel>` → `internal interface ...`
- `IPdfDocumentBuilder.cs`: `public interface IPdfDocumentBuilder<in TModel>` → `internal ...`
- `IQrGenerator.cs`: `public interface IQrGenerator` → `internal ...`
- `ICfdiTypeDetector.cs`: `public interface ICfdiTypeDetector` → `internal ...`
- `CfdiType.cs`: `public enum CfdiType` → `internal enum CfdiType`

- [ ] **Step 2: Dropear el registro DI de `ICfdiTypeDetector`**
En `Configuration/ServiceCollectionExtensions.cs`, eliminar el comentario y la línea:
```csharp
            // Utilidad pública de detección de tipo (no usada por el orquestador; su visibilidad se decide en F4).
            services.AddTransient<ICfdiTypeDetector, CfdiTypeDetector>();
```
(Ya no es API pública: deja de registrarse. La detección la hacen los handlers internamente.) Si tras quitarla queda un `using` sin uso, déjalo si lo siguen usando otras líneas; el compilador avisa.

- [ ] **Step 3: Build + suite**
Run: `dotnet test CFDI.BuildPdf.sln`
Expected: todo verde. Los tests acceden a los tipos internal vía `InternalsVisibleTo`. `CfdiTypeDetectorTests` (que instancia `new CfdiTypeDetector()` y usa `CfdiType`) sigue compilando porque la clase y el enum son internal-visibles a los tests. `git status --porcelain` → ningún baseline modificado. Si el compilador marca accesibilidad inconsistente, PARAR y reportar (no debería ocurrir).

- [ ] **Step 4: Commit**
```bash
git add CFDI.BuildPdf/Abstractions/ICfdiModelMapper.cs CFDI.BuildPdf/Abstractions/IPdfDocumentBuilder.cs CFDI.BuildPdf/Abstractions/IQrGenerator.cs CFDI.BuildPdf/Abstractions/ICfdiTypeDetector.cs CFDI.BuildPdf/Abstractions/CfdiType.cs CFDI.BuildPdf/Configuration/ServiceCollectionExtensions.cs
git commit -m "refactor: internalizar contratos internos (mapper/builder/qr/detector) + CfdiType y dropear su registro DI (O3) (F4)"
```

---

## Task 2: Internalizar los ViewModels (`Models`)

**Files:** `Models/CfdiViewModelBase.cs`, `Models/CfdiCartaPorteViewModel.cs`, `Models/CfdiNominaViewModel.cs`.

- [ ] **Step 1: Cambiar a `internal` TODAS las clases de los 3 archivos**
En cada archivo, cambiar cada declaración de tipo de nivel superior `public abstract class`/`public class` a `internal abstract class`/`internal class`. Incluye (no exhaustivo — cambia TODAS las que encuentres):
- `CfdiViewModelBase.cs`: `CfdiViewModelBase`.
- `CfdiCartaPorteViewModel.cs`: `CfdiCartaPorteViewModel`, `AddendaViewModel`, `AddendaSeccionViewModel`, `ConceptoViewModel`, `ImpuestoConceptoViewModel`, `RetencionImpuestoViewModel`, `CartaPorteViewModel`, `UbicacionViewModel`, `MercanciaViewModel`, `AutotransporteViewModel`, `SeguroViewModel`, `RemolqueViewModel`, `FiguraTransporteViewModel`.
- `CfdiNominaViewModel.cs`: `CfdiNominaViewModel`, `ConceptoNominaViewModel`, `NominaViewModel`, `EmisorNominaViewModel`, `ReceptorNominaViewModel`, `PercepcionesNominaViewModel`, `PercepcionDetalleViewModel`, `HoraExtraViewModel`, `DeduccionesNominaViewModel`, `DeduccionDetalleViewModel`, `OtrosPagosNominaViewModel`, `OtroPagoDetalleViewModel`, `SubsidioAlEmpleoViewModel`, `IncapacidadViewModel`.
Usa `git grep -n "public class\|public abstract class" -- CFDI.BuildPdf/Models` para encontrarlas todas y cambiar cada una. Las propiedades quedan igual (siguen siendo `public` dentro de un tipo `internal` — es correcto).

- [ ] **Step 2: Confirmar que no queda ningún tipo público en Models**
Run: `git grep -n "public class\|public abstract class" -- CFDI.BuildPdf/Models`
Expected: SIN salida.

- [ ] **Step 3: Build + suite**
Run: `dotnet test CFDI.BuildPdf.sln`
Expected: todo verde (los snapshots serializan los modelos vía sus propiedades públicas, que no cambian → JSON idéntico; `InternalsVisibleTo` permite a los tests usarlos). `git status --porcelain` → ningún baseline modificado. Si un snapshot cambia, PARAR e investigar (no debería: internal no afecta la serialización de propiedades).

- [ ] **Step 4: Commit**
```bash
git add CFDI.BuildPdf/Models/
git commit -m "refactor: internalizar todos los ViewModels (Models) (O3) (F4)"
```

---

## Task 3: Consolidar los namespaces públicos

**Files:** los 6 archivos de tipos públicos + usings guiados por el compilador en `CFDI.BuildPdf` y `CFDI.BuildPdf.Tests`.

Mover los tipos PÚBLICOS al namespace raíz `CFDI.BuildPdf`, y la extensión DI a `Microsoft.Extensions.DependencyInjection`. Los tipos internal se quedan en sus sub-namespaces actuales (no importan: son internos).

- [ ] **Step 1: Cambiar las declaraciones de namespace de los tipos públicos**
- `Service/CfdiPdf.cs`: `namespace CFDI.BuildPdf.Service` → `namespace CFDI.BuildPdf`.
- `Abstractions/ICfdiPdfGenerator.cs`: `namespace CFDI.BuildPdf.Abstractions` → `namespace CFDI.BuildPdf`.
- `Abstractions/CfdiPdfOptions.cs` (contiene `CfdiPdfOptions` y `PdfOrientation`): → `namespace CFDI.BuildPdf`.
- `Abstractions/CfdiPdfLicenseType.cs`: → `namespace CFDI.BuildPdf`.
- `Abstractions/CfdiPdfException.cs` (3 excepciones): → `namespace CFDI.BuildPdf`.
- `Configuration/ServiceCollectionExtensions.cs`: `namespace CFDI.BuildPdf.Configuration` → `namespace Microsoft.Extensions.DependencyInjection`.
(Los archivos de interfaces internas y `CfdiType` se quedan en `CFDI.BuildPdf.Abstractions` — no se mueven.)

- [ ] **Step 2: Compilar y arreglar usings, guiado por el compilador**
Run: `dotnet build CFDI.BuildPdf.sln -c Debug`
Para CADA error `CS0246`/`CS0234`/`CS0103` por tipo no encontrado, añadir el `using` correcto en ese archivo:
- Para `CfdiPdf`, `ICfdiPdfGenerator`, `CfdiPdfOptions`, `PdfOrientation`, `CfdiPdfLicenseType`, `CfdiPdfException`/`CfdiXmlInvalidoException`/`CfdiComplementoNoSoportadoException` → `using CFDI.BuildPdf;`.
- Para `AddCfdiPdfServices` (extensión) → `using Microsoft.Extensions.DependencyInjection;` (probablemente ya presente en consumidores).
- En `ServiceCollectionExtensions.cs`, que ahora está en `Microsoft.Extensions.DependencyInjection`, añadir `using CFDI.BuildPdf;` (para `CfdiPdf.MapLicense`, `CfdiPdfOptions`, `CfdiPdfLicenseType`, `ICfdiPdfGenerator`) y conservar el `using CFDI.BuildPdf.Services;` (para `CfdiTypeDetector`) y el namespace de la factory (`CFDI.BuildPdf.Configuration`) vía `using CFDI.BuildPdf.Configuration;`.
Repetir build → fix hasta `Build succeeded`. Tocar también los archivos de test (`Golden/*.cs`, `*MapperTests.cs`, `ComplementDispatchTests.cs`, `CompositionRootTests.cs`, helpers) cuyos `using CFDI.BuildPdf.Service;`/`using CFDI.BuildPdf.Abstractions;` ahora deben incluir `using CFDI.BuildPdf;`.

- [ ] **Step 3: Confirmar la superficie pública consolidada**
Run: `git grep -n "namespace CFDI.BuildPdf.Service\b" -- CFDI.BuildPdf` → SIN salida (la fachada ya no está en `.Service`).
Run: `git grep -rn "namespace CFDI.BuildPdf$" -- CFDI.BuildPdf/Service CFDI.BuildPdf/Abstractions` → muestra la fachada + los tipos públicos en el namespace raíz.

- [ ] **Step 4: Suite completa**
Run: `dotnet test CFDI.BuildPdf.sln`
Expected: todo verde. `git status --porcelain` → ningún `*.viewmodel.json` modificado (cambio de namespace no afecta el render ni la serialización).

- [ ] **Step 5: Commit**
```bash
git add -A
git commit -m "refactor: consolidar namespaces públicos en CFDI.BuildPdf y DI en Microsoft.Extensions.DependencyInjection (X1) (F4)"
```

---

## Task 4: Documentación XML completa + endurecer el build

**Files:** `CFDI.BuildPdf/CFDI.BuildPdf.csproj`; los ~9 archivos de tipos públicos (docs faltantes).

- [ ] **Step 1: Activar la generación de docs y medir warnings**
En `CFDI.BuildPdf/CFDI.BuildPdf.csproj`, dentro del `<PropertyGroup>`, añadir:
```xml
<GenerateDocumentationFile>true</GenerateDocumentationFile>
```
Run: `dotnet build CFDI.BuildPdf/CFDI.BuildPdf.csproj -c Debug 2>&1 | grep -iE "warning (CS|CA)" | sort | uniq -c | sort -rn | head -40`
Anota el inventario de warnings (CS1591 = doc XML faltante en miembro público; CSxxxx = compilador/nullable; CAxxxx = analizadores).

- [ ] **Step 2: Completar docs XML en la API pública**
Para cada `CS1591` (miembro público sin doc), añadir un `/// <summary>...</summary>` claro en los tipos públicos: `CfdiPdf` (ya documentado en su mayoría), `ICfdiPdfGenerator`, `CfdiPdfOptions`/`PdfOrientation`, `CfdiPdfLicenseType`, las 3 excepciones, `ServiceCollectionExtensions`. (Solo los miembros PÚBLICOS necesitan doc; los internal no emiten CS1591.)

- [ ] **Step 3: Activar `TreatWarningsAsErrors` de forma segura**
En el `csproj`, añadir `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.
Run: `dotnet build CFDI.BuildPdf/CFDI.BuildPdf.csproj -c Debug`
- Si quedan **warnings de compilador (CSxxxx)** (p. ej. nullable CS8618/CS8625): arréglalos (son reales y de bajo riesgo) — inicializadores, `?`, `= null!` donde corresponda, SIN cambiar lógica de render.
- Si quedan **warnings de analizadores (CAxxxx)** cuyo arreglo cambiaría el output (p. ej. `CA1305` cultura en builders) o son ruido diferido: NO los arregles aquí (cambiarían el PDF). En su lugar, en `.editorconfig` baja esas reglas a `suggestion` (no `warning`), o añade `<WarningsNotAsErrors>CA1305;CA1310;CA1860;CA1861;CA1848;CA1822</WarningsNotAsErrors>` al csproj, con un comentario indicando que se difieren. El objetivo: el build pasa con `TreatWarningsAsErrors=true` SIN arreglos riesgosos de CA.
Build hasta `Build succeeded` con `TreatWarningsAsErrors=true`.

- [ ] **Step 4: Suite completa (golden = sin cambios)**
Run: `dotnet test CFDI.BuildPdf.sln`
Expected: todo verde; `git status --porcelain` → ningún `*.viewmodel.json` modificado. (Si un baseline cambió, algún "arreglo" de warning alteró el output → revertir ese arreglo y diferir la regla.)

- [ ] **Step 5: Commit**
```bash
git add CFDI.BuildPdf/CFDI.BuildPdf.csproj CFDI.BuildPdf/ .editorconfig
git commit -m "build: docs XML completas en API pública + TreatWarningsAsErrors (X2) (F4)"
```

---

## Task 5: Test de XML inválido (carry-forward)

**Files:** Create `CFDI.BuildPdf.Tests/InvalidXmlTests.cs`.

- [ ] **Step 1: Crear el test**
```csharp
using System.Threading.Tasks;
using CFDI.BuildPdf;
using Xunit;

namespace CFDI.BuildPdf.Tests
{
    /// <summary>
    /// Verifica el manejo de XML mal formado en los puntos de entrada públicos.
    /// </summary>
    public class InvalidXmlTests
    {
        [Fact]
        public async Task DesdeXmlString_XmlMalFormado_LanzaCfdiXmlInvalido()
        {
            const string xmlRoto = "<cfdi:Comprobante xmlns:cfdi=\"http://www.sat.gob.mx/cfd/4\" Version=\"4.0\"";

            await Assert.ThrowsAsync<CfdiXmlInvalidoException>(
                () => CfdiPdf.DesdeXmlStringAsync(xmlRoto));
        }

        [Fact]
        public async Task DesdeXmlBytes_XmlMalFormado_LanzaCfdiXmlInvalido()
        {
            var bytesRotos = System.Text.Encoding.UTF8.GetBytes("<no-cerrado>");

            await Assert.ThrowsAsync<CfdiXmlInvalidoException>(
                () => CfdiPdf.DesdeXmlBytesAsync(bytesRotos));
        }
    }
}
```
(Usa `using CFDI.BuildPdf;` porque tras la Task 3 la fachada y la excepción están en el namespace raíz. Si la consolidación dejara la excepción en otro namespace, ajusta el `using` según el compilador.)

- [ ] **Step 2: Build + suite**
Run: `dotnet test CFDI.BuildPdf.sln --filter "FullyQualifiedName~InvalidXmlTests"`
Expected: 2 PASS. Luego la suite completa: `dotnet test CFDI.BuildPdf.sln` → todo verde.

- [ ] **Step 3: Commit**
```bash
git add CFDI.BuildPdf.Tests/InvalidXmlTests.cs
git commit -m "test: cobertura de XML mal formado (CfdiXmlInvalidoException) (F4)"
```

---

## Task 6: Verificación + cierre de F4

**Files:** ninguno (verificación).

- [ ] **Step 1: Suite con cobertura**
Run: `dotnet test CFDI.BuildPdf.sln --collect:"XPlat Code Coverage"` → `Passed!` 0 fallos.

- [ ] **Step 2: Confirmar la superficie pública (~9 tipos)**
Verifica que SOLO estos tipos quedan `public` en la librería: `CfdiPdf`, `ServiceCollectionExtensions`, `ICfdiPdfGenerator`, `CfdiPdfOptions`, `PdfOrientation`, `CfdiPdfLicenseType`, `CfdiPdfException`, `CfdiXmlInvalidoException`, `CfdiComplementoNoSoportadoException`.
Run: `git grep -n "public class\|public interface\|public enum\|public static class\|public sealed class\|public abstract class" -- CFDI.BuildPdf` y confirma que la lista coincide (más posibles miembros, pero ningún OTRO tipo público). Documenta la lista en el reporte.

- [ ] **Step 3: Baselines intactos + repo limpio + tag**
Run: `git log --oneline f3-composition..HEAD -- "CFDI.BuildPdf.Tests/Golden/Snapshots/"` → SIN salida.
Run: `git status --porcelain` → vacío.
```bash
git tag -f f4-superficie -m "F4: superficie pública mínima (caja cerrada), namespaces consolidados, docs XML, TreatWarningsAsErrors"
```

---

## Self-Review

**Spec coverage (F4):**
- Internalizar interfaces/`CfdiType` → Task 1 ✅; Models → Task 2 ✅ (O3).
- Dropear `ICfdiTypeDetector` del DI (decisión F4) → Task 1 ✅.
- Consolidar namespaces públicos a `CFDI.BuildPdf` + DI a `Microsoft.Extensions.DependencyInjection` → Task 3 ✅ (X1).
- Docs XML completas en API pública → Task 4 ✅ (X2).
- `TreatWarningsAsErrors` → Task 4 ✅ (con CA diferidas de forma segura).
- Test de XML inválido (carry-forward F3) → Task 5 ✅.

**Placeholder scan:** los `Create` llevan código completo; las internalizaciones y los renombrados de namespace son cambios mecánicos precisos; los arreglos de `using` son guiados por el compilador (procedimiento determinista, no placeholder). Sin TBD/TODO.

**Riesgo principal:** la consolidación de namespaces (Task 3) toca muchos `using`; mitigación: el compilador caza cada referencia rota y los golden tests cuidan el comportamiento. `TreatWarningsAsErrors` podría tentar a "arreglar" CA1305 (cultura) en builders, lo que cambiaría el PDF; el plan lo prohíbe explícitamente y manda diferir esas reglas.

**Decisiones deliberadas:**
- Los tipos internal que estaban en `CFDI.BuildPdf.Abstractions` se quedan ahí (namespace ahora interno) para minimizar churn; no es superficie pública, así que su namespace es irrelevante.
- `CfdiType`/`ICfdiTypeDetector` se vuelven internal y `ICfdiTypeDetector` sale del DI: la detección es 100% interna (handlers). `CfdiTypeDetector` y sus tests siguen vivos vía `InternalsVisibleTo`.

**Notas para F5:** bump de `<Version>` a `3.0.0`; SourceLink + build determinista + símbolos; `MIGRATION.md` (incluyendo el cambio de namespaces y los tipos que salen de público — esp. `IQrGenerator`); `CHANGELOG`; README v3; actualizar el `ConsoleDemo` (net8 + referencia al proyecto/paquete v3 + nuevos namespaces); GitHub Actions (build→test→pack→publish).

## Carry-forward de la ejecución de F4 (para F5)

La revisión final confirmó READY FOR F5 sin issues Critical/Important. Plan de F5 (todo confirmado por la revisión):
1. **Version → 3.0.0** (`CFDI.BuildPdf.csproj`, hoy 2.0.8).
2. **Packaging hardening:** SourceLink (`Microsoft.SourceLink.GitHub`), `Deterministic`, `ContinuousIntegrationBuild` (en CI), `EmbedUntrackedSources`, `PublishRepositoryUrl`. Hoy solo hay `IncludeSymbols`/`snupkg`.
3. **`MIGRATION.md` (no existe):** documentar (a) namespaces — DI ext → `Microsoft.Extensions.DependencyInjection`; el resto → `CFDI.BuildPdf` (se van `.Abstractions`/`.Service`); (b) tipos retirados de público — **especialmente `IQrGenerator`** (era público; quien implementara un QR propio se rompe), modelos, `ICfdiTypeDetector`/`CfdiType` (eliminados), `ICfdiModelMapper`/`IPdfDocumentBuilder` (internal); (c) net6→net8.
4. **`CHANGELOG.md`:** entrada 3.0.0 con la reducción de superficie + breaking changes.
5. **README v3:** ejemplos con los namespaces consolidados.
6. **ConsoleDemo (roto vs. el source nuevo):** sigue en net6, referencia el paquete 2.0.8 y usa los namespaces viejos (`.Abstractions`/`.Service`). Compila solo contra el paquete viejo. F5: cambiar a `ProjectReference` + net8 + `using CFDI.BuildPdf;`.
7. **GitHub Actions:** no existe workflow; añadir build→test(+cobertura)→pack→publish (publish en tag, API key en secrets).
8. **Deuda diferida:** la lista `WarningsNotAsErrors` (CA1305/1310/1311/1304 cultura — cambiarían el PDF — + nullable CS86xx) → limpieza dedicada golden-guarded, fuera de las fases behavior-preserving.

**Observación menor (no bloquea):** las CARPETAS `Abstractions/` y `Service/` persisten aunque sus tipos públicos ahora declaran `namespace CFDI.BuildPdf` (los tipos internos siguen en sub-namespaces). Inofensivo; considerar renombrar carpetas en una limpieza futura.
