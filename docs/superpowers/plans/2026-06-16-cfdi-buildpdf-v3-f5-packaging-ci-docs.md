# CFDI.BuildPdf v3 — Fase F5: packaging, CI/CD, docs y migración (release-prep) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Dejar el v3 listo para publicar: bump a 3.0.0, packaging enterprise (SourceLink, build determinista, símbolos), documentación de migración (MIGRATION/CHANGELOG/README), demo actualizado, y pipeline de CI/CD. **No se toca lógica de la librería**; los golden tests siguen verdes. **El pipeline se configura pero NO se publica nada** — el publish real solo corre cuando el usuario etiquete y suba (con su autorización).

**Tech Stack:** .NET 8.0, GitHub Actions, NuGet, Microsoft.SourceLink.GitHub.

**Spec:** `docs/superpowers/specs/2026-06-16-cfdi-buildpdf-v3-enterprise-design.md` (§10, F5). **Base:** tag `f4-superficie`. **Rama:** `Refactor` (commits LOCALES; NO push hasta autorización del usuario).

**Invariante de F5:** tras cada tarea, `dotnet test CFDI.BuildPdf.sln` → verde y `git status --porcelain` sin `*.viewmodel.json` modificado. F5 es config/docs; no cambia el render.

---

## File Structure (F5)

- Modify: `CFDI.BuildPdf/CFDI.BuildPdf.csproj` — Version 3.0.0; SourceLink + propiedades de packaging.
- Modify: `CFDI.BuildPdf.ConsoleDemo/CFDI.BuildPdf.ConsoleDemo.csproj` — net8 + ProjectReference.
- Modify: `CFDI.BuildPdf.ConsoleDemo/Program.cs` — namespaces v3.
- Create: `MIGRATION.md` (raíz).
- Modify: `CHANGELOG.md` — entrada 3.0.0.
- Modify: `README.md` — ejemplos con namespaces v3 + nota de migración.
- Create: `.github/workflows/build.yml`.

---

## Task 1: Packaging hardening + Version 3.0.0 + SourceLink

**Files:** `CFDI.BuildPdf/CFDI.BuildPdf.csproj`.

- [ ] **Step 1: Bump version y añadir propiedades de packaging determinista**
En el primer `<PropertyGroup>` de `CFDI.BuildPdf/CFDI.BuildPdf.csproj`:
- Cambiar `<Version>2.0.8</Version>` → `<Version>3.0.0</Version>`.
- Añadir (junto a las otras propiedades de packaging, p. ej. tras `<PackageReadmeFile>README.md</PackageReadmeFile>`):
```xml
		<PublishRepositoryUrl>true</PublishRepositoryUrl>
		<EmbedUntrackedSources>true</EmbedUntrackedSources>
		<Deterministic>true</Deterministic>
		<IncludeSymbols>true</IncludeSymbols>
		<SymbolPackageFormat>snupkg</SymbolPackageFormat>
		<PackageReleaseNotes>v3.0.0: superficie pública mínima (caja cerrada), net8.0, namespaces consolidados en CFDI.BuildPdf, arquitectura de handlers por complemento. Ver MIGRATION.md.</PackageReleaseNotes>
```
(`IncludeSymbols`/`SymbolPackageFormat` ya existen — no los dupliques; solo asegúrate de que estén. Si ya están, omite esas dos líneas.)

- [ ] **Step 2: Añadir el paquete SourceLink**
En el `ItemGroup` de `PackageReference`, añadir:
```xml
<PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All" />
```

- [ ] **Step 3: Build + pack + suite**
Run: `dotnet build CFDI.BuildPdf.sln -c Release`
Expected: `Build succeeded` (con `TreatWarningsAsErrors`; los CA/nullable diferidos siguen como warnings). Como `GeneratePackageOnBuild=true`, debe generar `CFDI.BuildPdf.3.0.0.nupkg` + `.snupkg` en `bin/Release`.
Run: `dotnet test CFDI.BuildPdf.sln` → todo verde. `git status --porcelain` → ningún `*.viewmodel.json` modificado.

- [ ] **Step 4: Confirmar el paquete v3**
Run: `find . -name "CFDI.BuildPdf.3.0.0.nupkg" -o -name "CFDI.BuildPdf.3.0.0.snupkg" | head` → debe listar el `.nupkg` (y el `.snupkg`). (No los commitees; `bin/` está en `.gitignore`.)

- [ ] **Step 5: Commit**
```bash
git add CFDI.BuildPdf/CFDI.BuildPdf.csproj
git commit -m "build: v3.0.0 + SourceLink/build determinista/símbolos (packaging enterprise) (F5)"
```

---

## Task 2: Migrar el ConsoleDemo a net8 + ProjectReference + namespaces v3

**Files:** `CFDI.BuildPdf.ConsoleDemo/CFDI.BuildPdf.ConsoleDemo.csproj`, `CFDI.BuildPdf.ConsoleDemo/Program.cs`.

El demo hoy apunta a net6.0 y consume el PAQUETE NuGet 2.0.8 (API vieja). Lo cambiamos para que referencie el PROYECTO local v3 (también valida en compilación que la API pública es usable) y use los namespaces nuevos.

- [ ] **Step 1: csproj del demo → net8 + ProjectReference**
En `CFDI.BuildPdf.ConsoleDemo/CFDI.BuildPdf.ConsoleDemo.csproj`:
- Cambiar `<TargetFramework>net6.0</TargetFramework>` → `<TargetFramework>net8.0</TargetFramework>`.
- Reemplazar `<PackageReference Include="CFDI.BuildPdf" Version="2.0.8" />` por:
```xml
<ProjectReference Include="..\CFDI.BuildPdf\CFDI.BuildPdf.csproj" />
```

- [ ] **Step 2: Program.cs → namespaces v3**
En `CFDI.BuildPdf.ConsoleDemo/Program.cs`, reemplazar las líneas:
```csharp
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Service;
```
por:
```csharp
using CFDI.BuildPdf;
```
(Todos los tipos que usa el demo — `CfdiPdf`, `CfdiPdfLicenseType`, `CfdiPdfOptions`, `CfdiXmlInvalidoException`, `CfdiComplementoNoSoportadoException` — están ahora en `CFDI.BuildPdf`. La línea `using System.Diagnostics;` se conserva.)

- [ ] **Step 3: Build del demo**
Run: `dotnet build CFDI.BuildPdf.ConsoleDemo/CFDI.BuildPdf.ConsoleDemo.csproj -c Debug`
Expected: `Build succeeded` (ya NO debe aparecer el warning NETSDK1138 de net6 EOL; compila contra el proyecto local v3). Si un tipo no resuelve, ajusta el `using` según el compilador (todo lo público está en `CFDI.BuildPdf`).

- [ ] **Step 4: Smoke run del demo (end-to-end real con un fixture)**
Genera un PDF real con uno de los XML de prueba para confirmar que el demo funciona contra v3:
```bash
dotnet run --project CFDI.BuildPdf.ConsoleDemo/CFDI.BuildPdf.ConsoleDemo.csproj -- "CFDI.BuildPdf.Tests/TestData/cfdi_cartaporte.xml" "C:/tmp/demo_cartaporte.pdf"
```
Expected: `OK — PDF generado en ... ms (... bytes).` y el archivo existe. (Ajusta la ruta de salida a una carpeta escribible, p. ej. `c:/tmp`.) Borra el PDF de prueba al terminar.

- [ ] **Step 5: Suite completa (no debe romperse)**
Run: `dotnet test CFDI.BuildPdf.sln` → todo verde. `git status --porcelain` → ningún `*.viewmodel.json` modificado.

- [ ] **Step 6: Commit**
```bash
git add CFDI.BuildPdf.ConsoleDemo/CFDI.BuildPdf.ConsoleDemo.csproj CFDI.BuildPdf.ConsoleDemo/Program.cs
git commit -m "demo: migrar ConsoleDemo a net8 + ProjectReference + namespaces v3 (F5)"
```

---

## Task 3: Documentación de release (MIGRATION + CHANGELOG + README)

**Files:** Create `MIGRATION.md`; Modify `CHANGELOG.md`, `README.md`.

- [ ] **Step 1: Crear `MIGRATION.md` (raíz)** con EXACTAMENTE este contenido:
```markdown
# Guía de migración de CFDI.BuildPdf v2.x → v3.0.0

v3.0.0 es una versión **mayor** con cambios que rompen compatibilidad (breaking changes), pensada para **opt-in**: tu proyecto en 2.x sigue funcionando hasta que decidas actualizar. Esta guía cubre todo lo que cambia al pasar a 3.0.0.

## TL;DR para el 99% de los consumidores
Si solo usas la fachada estática `CfdiPdf` o `AddCfdiPdfServices`, la migración es básicamente **cambiar los `using`**:

```diff
- using CFDI.BuildPdf.Service;
- using CFDI.BuildPdf.Abstractions;
- using CFDI.BuildPdf.Configuration;
+ using CFDI.BuildPdf;
+ using Microsoft.Extensions.DependencyInjection; // si usas AddCfdiPdfServices
```
El resto de tu código (`CfdiPdf.DesdeRutaAsync(...)`, `new CfdiPdfOptions { ... }`, `AddCfdiPdfServices(...)`, catch de `CfdiXmlInvalidoException`/`CfdiComplementoNoSoportadoException`) **no cambia**.

## 1. Target framework: net6.0 → net8.0
v3 requiere **.NET 8.0+**. Si sigues en net6/net7, permanece en 2.x (rama de mantenimiento `release/2.x`) hasta que puedas actualizar.

## 2. Namespaces consolidados
Toda la API pública vive ahora en `CFDI.BuildPdf` (antes repartida en `.Service` y `.Abstractions`). La extensión de DI se movió a `Microsoft.Extensions.DependencyInjection` (convención .NET), por lo que `services.AddCfdiPdfServices(...)` es descubrible sin un `using` extra.

| Tipo público | Namespace v2 | Namespace v3 |
|---|---|---|
| `CfdiPdf` (fachada) | `CFDI.BuildPdf.Service` | `CFDI.BuildPdf` |
| `ICfdiPdfGenerator` | `CFDI.BuildPdf.Abstractions` | `CFDI.BuildPdf` |
| `CfdiPdfOptions`, `PdfOrientation` | `CFDI.BuildPdf.Abstractions` | `CFDI.BuildPdf` |
| `CfdiPdfLicenseType` | `CFDI.BuildPdf.Abstractions` | `CFDI.BuildPdf` |
| `CfdiPdfException` y derivadas | `CFDI.BuildPdf.Abstractions` | `CFDI.BuildPdf` |
| `AddCfdiPdfServices` | `CFDI.BuildPdf.Configuration` | `Microsoft.Extensions.DependencyInjection` |

## 3. Superficie pública reducida (caja cerrada)
Para una API estable y mantenible, v3 deja públicos solo 9 tipos: `CfdiPdf`, `ServiceCollectionExtensions` (`AddCfdiPdfServices`), `ICfdiPdfGenerator`, `CfdiPdfOptions`, `PdfOrientation`, `CfdiPdfLicenseType`, `CfdiPdfException`, `CfdiXmlInvalidoException`, `CfdiComplementoNoSoportadoException`.

Pasaron a **internal** (ya no referenciables desde fuera):
- Todos los ViewModels (`CfdiCartaPorteViewModel`, `CfdiNominaViewModel`, etc.). La librería es XML→`byte[]`; los modelos eran DTOs internos.
- Las interfaces genéricas `ICfdiModelMapper<T>` e `IPdfDocumentBuilder<T>`.
- **`IQrGenerator`** — si en v2 reemplazabas el generador de QR registrando tu propia implementación vía DI, eso ya no es posible en v3. Si lo necesitas, abre un issue describiendo el caso de uso.

Se **eliminaron** (eran detección redundante, hoy el despacho es por namespace del complemento):
- `ICfdiTypeDetector`, `CfdiType`, `CfdiTypeDetector`.

Y se eliminó el método redundante `CanMap` de los mappers.

## 4. Comportamiento sin cambios
El PDF generado es **idéntico** a v2.0.8: mismos mapeos, mismos catálogos SAT, mismo layout. v3 es un refactor de arquitectura/superficie, validado contra una red de pruebas golden que congela el output.

## Soporte de v2.x
La rama `release/2.x` recibe parches críticos. v3 es opt-in.
```

- [ ] **Step 2: Añadir la entrada 3.0.0 al `CHANGELOG.md`** (insertar JUSTO DEBAJO de la línea `y este proyecto usa [Versionado Semántico]...` y ANTES de `## [2.0.8]`):
```markdown

## [3.0.0] - 2026-06-17

Versión mayor: refactor a nivel enterprise. **El PDF generado no cambia** respecto a 2.0.8 (validado con pruebas golden). Ver [MIGRATION.md](MIGRATION.md).

### Changed (BREAKING)
- **Target framework `net6.0` → `net8.0`** (LTS).
- **Namespaces consolidados:** toda la API pública en `CFDI.BuildPdf`; `AddCfdiPdfServices` en `Microsoft.Extensions.DependencyInjection`. Antes en `.Service`/`.Abstractions`/`.Configuration`.
- **Superficie pública reducida a 9 tipos (caja cerrada):** los ViewModels, `ICfdiModelMapper<T>`, `IPdfDocumentBuilder<T>` e **`IQrGenerator`** pasaron a `internal`.
- Despacho de complementos por **registro de handlers por namespace** (Open/Closed): añadir un complemento nuevo ya no toca el orquestador.
- Composition root único (`CfdiPdfFactory`) compartido por la fachada y el contenedor DI.
- I/O de carga de XML ahora **asíncrono honesto** (`XDocument.LoadAsync`).
- Licencia QuestPDF **idempotente** (no degrada una licencia ya configurada).

### Added
- `CfdiPdf.ConfigureLogging(ILoggerFactory)` para diagnóstico de los mappers en el camino de la fachada.
- Red de pruebas golden (snapshots de ViewModel + smoke de PDF) y cobertura ampliada de casos límite (retenciones a nivel concepto, CfdiRelacionados, incapacidades, horas extra).
- SourceLink, build determinista y símbolos `snupkg` en el paquete.

### Removed (BREAKING)
- `ICfdiTypeDetector`, `CfdiType` y `CfdiTypeDetector` (detección redundante; el despacho es por namespace).
- Método `CanMap` de `ICfdiModelMapper`.
```

- [ ] **Step 3: Actualizar el `README.md`** — actualizar los `using` en los ejemplos de código a los namespaces v3:
- Reemplazar `using CFDI.BuildPdf.Service;` → `using CFDI.BuildPdf;` en los bloques de ejemplo.
- Reemplazar `using CFDI.BuildPdf.Abstractions;` → `using CFDI.BuildPdf;`.
- Reemplazar `using CFDI.BuildPdf.Configuration;` → `using Microsoft.Extensions.DependencyInjection;`.
- En la sección de instalación o al inicio, añadir una nota: `> **v3.0.0** cambia namespaces y requiere .NET 8. Migrando desde v2.x → ver [MIGRATION.md](MIGRATION.md).`
Usa `git grep -n "using CFDI.BuildPdf" -- README.md` para localizar todos los bloques y ajustarlos. No cambies la prosa descriptiva salvo la nota de migración.

- [ ] **Step 4: Confirmar que nada se rompió**
Run: `dotnet test CFDI.BuildPdf.sln` → verde (las docs no afectan el build, pero el README va dentro del paquete). `git status --porcelain` → ningún `*.viewmodel.json` modificado.

- [ ] **Step 5: Commit**
```bash
git add MIGRATION.md CHANGELOG.md README.md
git commit -m "docs: MIGRATION.md v2→v3, CHANGELOG 3.0.0 y README con namespaces v3 (F5)"
```

---

## Task 4: GitHub Actions (build → test → pack → publish)

**Files:** Create `.github/workflows/build.yml`.

- [ ] **Step 1: Crear el workflow**
Crear `.github/workflows/build.yml` con EXACTAMENTE este contenido:
```yaml
name: build

on:
  push:
    branches: [ master ]
    tags: [ 'v*' ]
  pull_request:
    branches: [ master ]

jobs:
  build-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0   # SourceLink necesita el historial completo
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet restore CFDI.BuildPdf.sln
      - run: dotnet build CFDI.BuildPdf.sln -c Release --no-restore -p:ContinuousIntegrationBuild=true
      - run: dotnet test CFDI.BuildPdf.sln -c Release --no-build --collect:"XPlat Code Coverage"

  publish:
    needs: build-test
    if: startsWith(github.ref, 'refs/tags/v')
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet pack CFDI.BuildPdf/CFDI.BuildPdf.csproj -c Release -p:ContinuousIntegrationBuild=true -o ./artifacts
      - run: dotnet nuget push "./artifacts/*.nupkg" --api-key "${{ secrets.NUGET_API_KEY }}" --source https://api.nuget.org/v3/index.json --skip-duplicate
```

- [ ] **Step 2: Validar la sintaxis YAML**
Run: `python -c "import yaml,sys; yaml.safe_load(open('.github/workflows/build.yml')); print('YAML OK')"` (o cualquier validador disponible). Expected: `YAML OK`. (No podemos ejecutar el workflow localmente; solo validamos sintaxis. Documenta que requiere el secret `NUGET_API_KEY` en el repo y que `publish` solo corre en tags `v*`.)

- [ ] **Step 3: Commit**
```bash
git add .github/workflows/build.yml
git commit -m "ci: workflow GitHub Actions build/test + pack/publish en tag v* (F5)"
```

---

## Task 5: Verificación + cierre de F5 (v3 completo)

**Files:** ninguno (verificación).

- [ ] **Step 1: Build Release + pack + suite con cobertura**
Run: `dotnet build CFDI.BuildPdf.sln -c Release` → succeeded; genera `CFDI.BuildPdf.3.0.0.nupkg` + `.snupkg`.
Run: `dotnet test CFDI.BuildPdf.sln --collect:"XPlat Code Coverage"` → `Passed!` 0 fallos.

- [ ] **Step 2: Inventario final del paquete v3**
Run: `find . -name "CFDI.BuildPdf.3.0.0.*nupkg"` → lista el `.nupkg` y el `.snupkg`. Opcional: `unzip -l` el `.nupkg` para confirmar que incluye `README.md`, `logotipo.png`, `CHANGELOG.md` y el `CFDI.BuildPdf.xml` (doc).

- [ ] **Step 3: Baselines intactos + repo limpio + tag**
Run: `git log --oneline f4-superficie..HEAD -- "CFDI.BuildPdf.Tests/Golden/Snapshots/"` → SIN salida.
Run: `git status --porcelain` → vacío.
```bash
git tag -f v3.0.0 -m "CFDI.BuildPdf v3.0.0 (refactor enterprise: caja cerrada, net8, arquitectura de handlers)"
```
(El tag `v3.0.0` es el que dispararía el publish en CI — pero solo cuando el usuario lo SUBA con su autorización. Localmente solo marca el commit.)

---

## Self-Review

**Spec coverage (F5):**
- Version 3.0.0 → Task 1 ✅
- SourceLink + determinista + símbolos → Task 1 ✅
- MIGRATION.md → Task 3 ✅; CHANGELOG 3.0.0 → Task 3 ✅; README v3 → Task 3 ✅
- ConsoleDemo migrado (net8 + ProjectReference + namespaces) → Task 2 ✅
- GitHub Actions build→test→pack→publish → Task 4 ✅

**Placeholder scan:** los `Create` (MIGRATION.md, workflow) llevan contenido completo; la entrada de CHANGELOG es literal; las ediciones de csproj/demo/README son precisas. Sin TBD/TODO.

**Riesgos:** mínimos (config/docs, sin lógica). El demo ahora compila contra el proyecto local → valida la usabilidad de la API pública v3 (un beneficio extra). El workflow no se ejecuta localmente; solo se valida sintaxis. El publish requiere `NUGET_API_KEY` y solo corre en tags — nada se publica sin que el usuario suba el tag.

**Notas finales:** con F5 el v3 queda completo en local (5 fases + release-prep). La deuda diferida de nullable/CA (lista `WarningsNotAsErrors`) queda para una limpieza dedicada golden-guarded posterior. La rama `release/2.x` (para parches de v2) se crea desde el commit de v2.0.8 cuando el usuario lo decida, antes de integrar v3 a `master`.
