# CFDI.BuildPdf v3 — Fase F0: base net8 + red de seguridad (golden tests) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Migrar la solución a net8.0 y montar una red de seguridad de golden tests (snapshot de ViewModel + smoke de PDF) que congele el comportamiento actual antes de refactorizar, con **cero cambios de comportamiento**.

**Architecture:** No se toca código de producción funcional en F0. Se actualizan `TargetFramework` y paquetes; se añade tooling de calidad (`.editorconfig`, analizadores en modo *warning*); y se crean pruebas de regresión que (a) serializan el ViewModel mapeado a JSON y lo comparan contra un baseline commiteado, y (b) generan el PDF por la fachada y verifican que es un PDF válido con contenido. Estos tests serán el oráculo que valida F1–F4.

**Tech Stack:** .NET 8.0, xUnit, coverlet, QuestPDF 2024.3.5, QRCoder 1.6.0, System.Text.Json (BCL), UglyToad.PdfPig (solo en tests).

**Spec de referencia:** `docs/superpowers/specs/2026-06-16-cfdi-buildpdf-v3-enterprise-design.md` (Fase F0, §8 y §9).

---

## File Structure (F0)

- Modify: `CFDI.BuildPdf/CFDI.BuildPdf.csproj` — `TargetFramework` net8.0; `Microsoft.Extensions.*` 8.0.0; props de análisis.
- Modify: `CFDI.BuildPdf.Tests/CFDI.BuildPdf.Tests.csproj` — `TargetFramework` net8.0; ref a `UglyToad.PdfPig`.
- Create: `.editorconfig` (raíz del repo) — estilo + nullable + severidades en *warning*.
- Create: `CFDI.BuildPdf.Tests/Golden/Snapshot.cs` — helper de snapshot por archivo.
- Create: `CFDI.BuildPdf.Tests/Golden/ViewModelSnapshotTests.cs` — snapshots deterministas de los ViewModels mapeados.
- Create: `CFDI.BuildPdf.Tests/Golden/PdfSmokeTests.cs` — smoke del PDF generado (PdfPig).
- Create (generados en primera ejecución): `CFDI.BuildPdf.Tests/Golden/Snapshots/CartaPorte.viewmodel.json`, `Nomina.viewmodel.json`.

> El demo (`CFDI.BuildPdf.ConsoleDemo`) consume el paquete NuGet 2.0.8 y **no se toca en F0**; se actualiza en F5.

---

## Task 1: Migrar librería y tests a net8.0

**Files:**
- Modify: `CFDI.BuildPdf/CFDI.BuildPdf.csproj`
- Modify: `CFDI.BuildPdf.Tests/CFDI.BuildPdf.Tests.csproj`

- [ ] **Step 1: Cambiar el TargetFramework y los paquetes de la librería**

En `CFDI.BuildPdf/CFDI.BuildPdf.csproj`, cambiar la línea:
```xml
<TargetFramework>net6.0</TargetFramework>
```
por:
```xml
<TargetFramework>net8.0</TargetFramework>
```

Y en el `ItemGroup` de `PackageReference`, cambiar las tres referencias `Microsoft.Extensions.*` de 6.x a 8.0.0 (dejar QuestPDF y QRCoder igual):
```xml
<PackageReference Include="QuestPDF" Version="2024.3.5" />
<PackageReference Include="QRCoder" Version="1.6.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Options" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
```

- [ ] **Step 2: Cambiar el TargetFramework de los tests**

En `CFDI.BuildPdf.Tests/CFDI.BuildPdf.Tests.csproj`, cambiar:
```xml
<TargetFramework>net6.0</TargetFramework>
```
por:
```xml
<TargetFramework>net8.0</TargetFramework>
```

- [ ] **Step 3: Restaurar y compilar la solución**

Run: `dotnet build CFDI.BuildPdf.sln -c Debug`
Expected: `Build succeeded`. Pueden aparecer warnings de NuGet por el demo (que sigue en net6 y referencia el paquete 2.0.8); eso es esperado y no rompe el build de la librería/tests.

- [ ] **Step 4: Ejecutar los tests existentes para confirmar que siguen verdes**

Run: `dotnet test CFDI.BuildPdf.Tests/CFDI.BuildPdf.Tests.csproj`
Expected: `Passed!` con el mismo número de tests que antes (los de `CfdiTypeDetectorTests`, `CartaPorteMapperTests`, `NominaMapperTests`, `NumberToWordsConverterTests`, `QrUrlBuilderTests`).

- [ ] **Step 5: Commit**

```bash
git add CFDI.BuildPdf/CFDI.BuildPdf.csproj CFDI.BuildPdf.Tests/CFDI.BuildPdf.Tests.csproj
git commit -m "build: migrar librería y tests a net8.0 (F0)"
```

---

## Task 2: Añadir .editorconfig y analizadores (modo warning)

**Files:**
- Create: `.editorconfig`
- Modify: `CFDI.BuildPdf/CFDI.BuildPdf.csproj`

- [ ] **Step 1: Crear `.editorconfig` en la raíz del repo**

Crear `.editorconfig` con este contenido:
```ini
root = true

[*]
charset = utf-8
end_of_line = crlf
insert_final_newline = true
indent_style = space
trim_trailing_whitespace = true

[*.cs]
indent_size = 4

# Nullable / calidad
dotnet_diagnostic.CS8600.severity = warning
dotnet_diagnostic.CS8602.severity = warning
dotnet_diagnostic.CS8603.severity = warning
dotnet_diagnostic.CS8625.severity = warning

# Estilo .NET (en warning por ahora; se endurece en F4)
dotnet_style_qualification_for_field = false:warning
dotnet_style_object_initializer = true:suggestion
csharp_style_var_for_built_in_types = false:suggestion
csharp_prefer_braces = true:suggestion

[*.{csproj,props,targets,xml}]
indent_size = 2

[*.{json,yml,yaml}]
indent_size = 2
```

- [ ] **Step 2: Activar el nivel de análisis en la librería (sin romper el build)**

En `CFDI.BuildPdf/CFDI.BuildPdf.csproj`, dentro del primer `<PropertyGroup>` (donde está `<Nullable>enable</Nullable>`), añadir estas dos líneas:
```xml
<AnalysisLevel>latest-recommended</AnalysisLevel>
<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
```
NO añadir `TreatWarningsAsErrors` todavía (eso es F4).

- [ ] **Step 3: Compilar y confirmar que sigue verde (los warnings son aceptables)**

Run: `dotnet build CFDI.BuildPdf/CFDI.BuildPdf.csproj -c Debug`
Expected: `Build succeeded`. Puede listar warnings de estilo/nullable; **no deben ser errores**. Si algún analizador produce un *error*, bajar esa regla a `warning` en `.editorconfig`.

- [ ] **Step 4: Commit**

```bash
git add .editorconfig CFDI.BuildPdf/CFDI.BuildPdf.csproj
git commit -m "chore: añadir .editorconfig y analizadores en modo warning (F0)"
```

---

## Task 3: Añadir PdfPig a los tests

**Files:**
- Modify: `CFDI.BuildPdf.Tests/CFDI.BuildPdf.Tests.csproj`

- [ ] **Step 1: Añadir la referencia a PdfPig**

En `CFDI.BuildPdf.Tests/CFDI.BuildPdf.Tests.csproj`, dentro del `ItemGroup` de `PackageReference`, añadir:
```xml
<PackageReference Include="UglyToad.PdfPig" Version="0.1.9" />
```

- [ ] **Step 2: Restaurar y compilar los tests**

Run: `dotnet build CFDI.BuildPdf.Tests/CFDI.BuildPdf.Tests.csproj -c Debug`
Expected: `Build succeeded` (PdfPig restaurado).

- [ ] **Step 3: Commit**

```bash
git add CFDI.BuildPdf.Tests/CFDI.BuildPdf.Tests.csproj
git commit -m "test: añadir UglyToad.PdfPig para smoke de PDF (F0)"
```

---

## Task 4: Crear el helper de snapshot

**Files:**
- Create: `CFDI.BuildPdf.Tests/Golden/Snapshot.cs`

- [ ] **Step 1: Crear el helper `Snapshot`**

Crear `CFDI.BuildPdf.Tests/Golden/Snapshot.cs`:
```csharp
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace CFDI.BuildPdf.Tests.Golden
{
    /// <summary>
    /// Snapshot por archivo: compara un texto actual contra un baseline commiteado.
    /// Si el baseline no existe, lo crea y falla pidiendo revisión (primera ejecución).
    /// El baseline se ubica junto a este archivo de test, en la carpeta Snapshots/.
    /// </summary>
    internal static class Snapshot
    {
        public static void Match(string actual, string snapshotFileName, [CallerFilePath] string callerFilePath = "")
        {
            var snapshotDir = Path.Combine(Path.GetDirectoryName(callerFilePath)!, "Snapshots");
            Directory.CreateDirectory(snapshotDir);
            var path = Path.Combine(snapshotDir, snapshotFileName);

            var normalizedActual = Normalize(actual);

            if (!File.Exists(path))
            {
                File.WriteAllText(path, normalizedActual);
                Assert.Fail($"Snapshot baseline creado: {path}. Revísalo, confírmalo y vuelve a ejecutar el test.");
            }

            var expected = Normalize(File.ReadAllText(path));
            Assert.Equal(expected, normalizedActual);
        }

        private static string Normalize(string text) => text.Replace("\r\n", "\n").TrimEnd();
    }
}
```

- [ ] **Step 2: Compilar los tests**

Run: `dotnet build CFDI.BuildPdf.Tests/CFDI.BuildPdf.Tests.csproj -c Debug`
Expected: `Build succeeded`.

- [ ] **Step 3: Commit**

```bash
git add CFDI.BuildPdf.Tests/Golden/Snapshot.cs
git commit -m "test: helper de snapshot por archivo (F0)"
```

---

## Task 5: Golden — snapshot del ViewModel de Carta Porte

**Files:**
- Create: `CFDI.BuildPdf.Tests/Golden/ViewModelSnapshotTests.cs`
- Test: el mismo archivo

- [ ] **Step 1: Escribir el test de snapshot de Carta Porte**

Crear `CFDI.BuildPdf.Tests/Golden/ViewModelSnapshotTests.cs`:
```csharp
using System.Text.Json;
using CFDI.BuildPdf.Mappers.CartaPorte;
using CFDI.BuildPdf.Tests.Helpers;
using Xunit;

namespace CFDI.BuildPdf.Tests.Golden
{
    public class ViewModelSnapshotTests
    {
        private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

        [Fact]
        public void CartaPorte_ViewModel_CoincideConBaseline()
        {
            var xdoc = TestXmlLoader.LoadCartaPorte();
            var mapper = new CartaPorteMapper(new FakeQrGenerator());

            var model = mapper.Map(xdoc);
            var json = JsonSerializer.Serialize(model, JsonOpts);

            Snapshot.Match(json, "CartaPorte.viewmodel.json");
        }
    }
}
```

- [ ] **Step 2: Primera ejecución — crea el baseline y falla a propósito**

Run: `dotnet test CFDI.BuildPdf.Tests/CFDI.BuildPdf.Tests.csproj --filter "FullyQualifiedName~CartaPorte_ViewModel_CoincideConBaseline"`
Expected: FAIL con el mensaje "Snapshot baseline creado: ...CartaPorte.viewmodel.json". Esto es esperado en la primera corrida.

- [ ] **Step 3: Inspeccionar el baseline generado**

Abrir `CFDI.BuildPdf.Tests/Golden/Snapshots/CartaPorte.viewmodel.json` y verificar que es un JSON razonable del ViewModel (emisor, receptor, conceptos, complemento Carta Porte, `QRCodeBase64` = "FAKE_QR_BASE64"). No editarlo a mano; es el oráculo del comportamiento actual.

- [ ] **Step 4: Segunda ejecución — ahora debe pasar**

Run: `dotnet test CFDI.BuildPdf.Tests/CFDI.BuildPdf.Tests.csproj --filter "FullyQualifiedName~CartaPorte_ViewModel_CoincideConBaseline"`
Expected: PASS.

- [ ] **Step 5: Commit (test + baseline)**

```bash
git add CFDI.BuildPdf.Tests/Golden/ViewModelSnapshotTests.cs CFDI.BuildPdf.Tests/Golden/Snapshots/CartaPorte.viewmodel.json
git commit -m "test: golden snapshot del ViewModel de Carta Porte (F0)"
```

---

## Task 6: Golden — snapshot del ViewModel de Nómina

**Files:**
- Modify: `CFDI.BuildPdf.Tests/Golden/ViewModelSnapshotTests.cs`

- [ ] **Step 1: Añadir el test de snapshot de Nómina**

En `CFDI.BuildPdf.Tests/Golden/ViewModelSnapshotTests.cs`, añadir el `using` del mapper de Nómina junto a los demás:
```csharp
using CFDI.BuildPdf.Mappers.Nomina;
```
y añadir este método dentro de la clase `ViewModelSnapshotTests`:
```csharp
        [Fact]
        public void Nomina_ViewModel_CoincideConBaseline()
        {
            var xdoc = TestXmlLoader.LoadNomina();
            var mapper = new NominaMapper(new FakeQrGenerator());

            var model = mapper.Map(xdoc);
            var json = JsonSerializer.Serialize(model, JsonOpts);

            Snapshot.Match(json, "Nomina.viewmodel.json");
        }
```

- [ ] **Step 2: Primera ejecución — crea el baseline y falla**

Run: `dotnet test CFDI.BuildPdf.Tests/CFDI.BuildPdf.Tests.csproj --filter "FullyQualifiedName~Nomina_ViewModel_CoincideConBaseline"`
Expected: FAIL con "Snapshot baseline creado: ...Nomina.viewmodel.json".

- [ ] **Step 3: Inspeccionar el baseline y re-ejecutar**

Abrir `CFDI.BuildPdf.Tests/Golden/Snapshots/Nomina.viewmodel.json`, verificar que es coherente (emisor, receptor, percepciones, deducciones, totales).
Run: `dotnet test CFDI.BuildPdf.Tests/CFDI.BuildPdf.Tests.csproj --filter "FullyQualifiedName~Nomina_ViewModel_CoincideConBaseline"`
Expected: PASS.

- [ ] **Step 4: Commit (test + baseline)**

```bash
git add CFDI.BuildPdf.Tests/Golden/ViewModelSnapshotTests.cs CFDI.BuildPdf.Tests/Golden/Snapshots/Nomina.viewmodel.json
git commit -m "test: golden snapshot del ViewModel de Nómina (F0)"
```

---

## Task 7: Golden — smoke del PDF de Carta Porte

**Files:**
- Create: `CFDI.BuildPdf.Tests/Golden/PdfSmokeTests.cs`
- Test: el mismo archivo

- [ ] **Step 1: Escribir el smoke test de Carta Porte**

Crear `CFDI.BuildPdf.Tests/Golden/PdfSmokeTests.cs`:
```csharp
using System.Linq;
using System.Threading.Tasks;
using CFDI.BuildPdf.Service;
using CFDI.BuildPdf.Tests.Helpers;
using UglyToad.PdfPig;
using Xunit;

namespace CFDI.BuildPdf.Tests.Golden
{
    public class PdfSmokeTests
    {
        [Fact]
        public async Task CartaPorte_GeneraPdfValidoConContenido()
        {
            var xml = TestXmlLoader.LoadCartaPorte().ToString();

            var pdfBytes = await CfdiPdf.DesdeXmlStringAsync(xml);

            // Es un PDF válido y no trivial
            Assert.NotNull(pdfBytes);
            Assert.True(pdfBytes.Length > 1000, $"PDF demasiado pequeño: {pdfBytes.Length} bytes");
            Assert.Equal((byte)'%', pdfBytes[0]);
            Assert.Equal((byte)'P', pdfBytes[1]);
            Assert.Equal((byte)'D', pdfBytes[2]);
            Assert.Equal((byte)'F', pdfBytes[3]);

            // Tiene páginas y contenido textual real
            using var pdf = PdfDocument.Open(pdfBytes);
            Assert.True(pdf.NumberOfPages >= 1);
            var texto = string.Join(" ", pdf.GetPages().Select(p => p.Text));
            Assert.True(texto.Length > 200, $"Texto extraído demasiado corto: {texto.Length} chars");
        }
    }
}
```

- [ ] **Step 2: Ejecutar el smoke test**

Run: `dotnet test CFDI.BuildPdf.Tests/CFDI.BuildPdf.Tests.csproj --filter "FullyQualifiedName~CartaPorte_GeneraPdfValidoConContenido"`
Expected: PASS. (Si falla por licencia QuestPDF, confirmar que la fachada usa Community por defecto; no debería requerir configuración.)

- [ ] **Step 3: Commit**

```bash
git add CFDI.BuildPdf.Tests/Golden/PdfSmokeTests.cs
git commit -m "test: smoke del PDF de Carta Porte con PdfPig (F0)"
```

---

## Task 8: Golden — smoke del PDF de Nómina

**Files:**
- Modify: `CFDI.BuildPdf.Tests/Golden/PdfSmokeTests.cs`

- [ ] **Step 1: Añadir el smoke test de Nómina**

En `CFDI.BuildPdf.Tests/Golden/PdfSmokeTests.cs`, añadir este método dentro de la clase `PdfSmokeTests`:
```csharp
        [Fact]
        public async Task Nomina_GeneraPdfValidoConContenido()
        {
            var xml = TestXmlLoader.LoadNomina().ToString();

            var pdfBytes = await CfdiPdf.DesdeXmlStringAsync(xml);

            Assert.NotNull(pdfBytes);
            Assert.True(pdfBytes.Length > 1000, $"PDF demasiado pequeño: {pdfBytes.Length} bytes");
            Assert.Equal((byte)'%', pdfBytes[0]);
            Assert.Equal((byte)'P', pdfBytes[1]);
            Assert.Equal((byte)'D', pdfBytes[2]);
            Assert.Equal((byte)'F', pdfBytes[3]);

            using var pdf = PdfDocument.Open(pdfBytes);
            Assert.True(pdf.NumberOfPages >= 1);
            var texto = string.Join(" ", pdf.GetPages().Select(p => p.Text));
            Assert.True(texto.Length > 200, $"Texto extraído demasiado corto: {texto.Length} chars");
        }
```

- [ ] **Step 2: Ejecutar el smoke test de Nómina**

Run: `dotnet test CFDI.BuildPdf.Tests/CFDI.BuildPdf.Tests.csproj --filter "FullyQualifiedName~Nomina_GeneraPdfValidoConContenido"`
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add CFDI.BuildPdf.Tests/Golden/PdfSmokeTests.cs
git commit -m "test: smoke del PDF de Nómina con PdfPig (F0)"
```

---

## Task 9: Verificación completa de la red de seguridad

**Files:** ninguno (solo verificación)

- [ ] **Step 1: Ejecutar TODA la suite con cobertura**

Run: `dotnet test CFDI.BuildPdf.sln --collect:"XPlat Code Coverage"`
Expected: `Passed!` — todos los tests verdes (los preexistentes + los 4 golden nuevos). Se genera un reporte de cobertura en `TestResults/`.

- [ ] **Step 2: Confirmar el estado del repo limpio y los baselines presentes**

Run: `git status --porcelain`
Expected: salida vacía (todo commiteado).
Run: `git log --oneline -8`
Expected: ver los commits de F0 (migración net8, editorconfig, pdfpig, snapshot helper, 4 golden tests).

- [ ] **Step 3: Etiqueta de checkpoint de fin de fase (opcional pero recomendado)**

```bash
git tag f0-baseline -m "F0: net8 + red de seguridad golden tests"
```

---

## Self-Review

**Spec coverage (F0):**
- net8.0 migración → Task 1 ✅
- `Microsoft.Extensions.*` 8.x → Task 1 ✅
- `.editorconfig` + analizadores (sin romper build aún) → Task 2 ✅
- Golden tests: snapshot de ViewModel (determinista) → Tasks 5–6 ✅; smoke de PDF (texto + nº páginas) → Tasks 7–8 ✅
- Cobertura recogida en CI/local → Task 9 ✅ (el *gate* de umbral se configura en F5 junto con CI).

**Placeholder scan:** sin TBD/TODO; todos los pasos llevan código o comando concreto y salida esperada. ✅

**Type/identidad consistency:** `Snapshot.Match(string, string)` definido en Task 4 y usado idéntico en Tasks 5–6. `CartaPorteMapper(IQrGenerator)` y `NominaMapper(IQrGenerator)` coinciden con los constructores reales (`IQrGenerator qrGenerator, ILogger<...>? logger = null`). `CfdiPdf.DesdeXmlStringAsync(string)` es la firma real de la fachada (namespace actual `CFDI.BuildPdf.Service`, aún sin consolidar en F0). `TestXmlLoader.LoadCartaPorte()/LoadNomina()` existen. `FakeQrGenerator` devuelve "FAKE_QR_BASE64". ✅

**Notas para fases siguientes (NO se implementan en F0):**
- El namespace `CFDI.BuildPdf.Service` del `using` en `PdfSmokeTests.cs` se actualizará a `CFDI.BuildPdf` en F4 al consolidar namespaces.
- Si en F1+ un refactor cambia legítimamente el ViewModel (p. ej. unificar `Traslado/RetencionConceptoViewModel`), el snapshot fallará a propósito: se revisa el diff y, si es el cambio esperado, se regenera borrando el `.json` y re-ejecutando.
