# CFDI.BuildPdf v3 — Fase F2: núcleo de arquitectura (registro de complementos) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reemplazar el `switch` del orquestador por un **registro de handlers por namespace** (Open/Closed), sin cambiar comportamiento: añadir un complemento nuevo debe requerir solo una clase handler + un registro, sin tocar orquestador ni fachada.

**Architecture:** `ICfdiComplementHandler` (declara `ComplementNamespaces` + `Priority` + `Generate`) con una base `ComplementHandlerBase<TModel>` que coordina mapper+builder y aplica el `LogoBase64` en un solo lugar. `CfdiPdfGenerator` recibe `IEnumerable<ICfdiComplementHandler>`, valida unicidad de namespaces al construir, y despacha inspeccionando los namespaces presentes en el XML (desempate por `Priority`). Se elimina `CanMap` (superficie muerta). La fachada y DI construyen los handlers.

**Tech Stack:** .NET 8.0, xUnit, QuestPDF, PdfPig (tests). Validado por la red de golden tests (F0 + F1): los baselines NO deben cambiar.

**Spec:** `docs/superpowers/specs/2026-06-16-cfdi-buildpdf-v3-enterprise-design.md` (§5, F2). **Base:** tag `f1-reuso`. **Rama:** `Refactor` (commits LOCALES; NO push hasta autorización del usuario).

**Invariante de F2:** tras cada cambio, `dotnet test CFDI.BuildPdf.sln` → todo verde (~70) y `git status --porcelain` sin ningún `*.viewmodel.json` modificado. El despacho nuevo debe enrutar idéntico al `switch` viejo; si un baseline cambia o un smoke falla, investigar.

---

## File Structure (F2)

- Create: `CFDI.BuildPdf/Complements/ICfdiComplementHandler.cs`
- Create: `CFDI.BuildPdf/Complements/ComplementHandlerBase.cs`
- Create: `CFDI.BuildPdf/Complements/CartaPorteComplementHandler.cs`
- Create: `CFDI.BuildPdf/Complements/NominaComplementHandler.cs`
- Modify: `CFDI.BuildPdf/Services/CfdiPdfGenerator.cs` — despacho por registro (sin `switch`, sin `ICfdiTypeDetector`).
- Modify: `CFDI.BuildPdf/Configuration/ServiceCollectionExtensions.cs` — registrar los handlers.
- Modify: `CFDI.BuildPdf/Service/CfdiPdf.cs` — construir los handlers en la fachada.
- Modify: `CFDI.BuildPdf/Abstractions/ICfdiModelMapper.cs`, `Mappers/Common/BaseCfdiMapper.cs`, `Mappers/CartaPorte/CartaPorteMapper.cs`, `Mappers/Nomina/NominaMapper.cs` — eliminar `CanMap`.
- Create: `CFDI.BuildPdf.Tests/ComplementDispatchTests.cs` — tests del despacho (migración de los 4 tests `CanMap`).
- Modify: `CFDI.BuildPdf.Tests/CartaPorteMapperTests.cs`, `NominaMapperTests.cs` — quitar los tests `CanMap`.

> `ICfdiTypeDetector`/`CfdiType`/`CfdiTypeDetector` se mantienen SIN cambios en F2 (siguen como utilidad pública; su visibilidad se decide en F4). El orquestador deja de depender del detector.

---

## Task 1: Crear la abstracción de handlers de complemento

**Files:** Create the 4 files under `CFDI.BuildPdf/Complements/`.

- [ ] **Step 1: `ICfdiComplementHandler.cs`**
```csharp
using System.Collections.Generic;
using System.Xml.Linq;
using CFDI.BuildPdf.Abstractions;

namespace CFDI.BuildPdf.Complements
{
    /// <summary>
    /// Maneja la generación de PDF de un complemento CFDI concreto.
    /// El orquestador selecciona el handler cuyo <see cref="ComplementNamespaces"/> esté
    /// presente en el XML; <see cref="Priority"/> desempata si aplican varios al mismo documento.
    /// </summary>
    internal interface ICfdiComplementHandler
    {
        /// <summary>Namespace(s) de complemento que este handler reconoce (incluye la versión).</summary>
        IReadOnlyCollection<string> ComplementNamespaces { get; }

        /// <summary>Prioridad de desempate cuando varios handlers aplican al mismo documento. Mayor gana.</summary>
        int Priority { get; }

        /// <summary>Genera el PDF a partir del XML CFDI ya cargado.</summary>
        byte[] Generate(XDocument xdoc, CfdiPdfOptions options);
    }
}
```

- [ ] **Step 2: `ComplementHandlerBase.cs`**
```csharp
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Models;

namespace CFDI.BuildPdf.Complements
{
    /// <summary>
    /// Base para handlers de complemento: coordina el mapper y el builder de un tipo
    /// y aplica las opciones comunes (logo) en un solo lugar.
    /// </summary>
    internal abstract class ComplementHandlerBase<TModel> : ICfdiComplementHandler
        where TModel : CfdiViewModelBase
    {
        private readonly ICfdiModelMapper<TModel> _mapper;
        private readonly IPdfDocumentBuilder<TModel> _builder;

        protected ComplementHandlerBase(ICfdiModelMapper<TModel> mapper, IPdfDocumentBuilder<TModel> builder)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        public abstract IReadOnlyCollection<string> ComplementNamespaces { get; }

        public virtual int Priority => 0;

        public byte[] Generate(XDocument xdoc, CfdiPdfOptions options)
        {
            var model = _mapper.Map(xdoc);

            if (!string.IsNullOrEmpty(options.LogoBase64))
                model.LogoBase64 = options.LogoBase64;

            return _builder.Build(model, options);
        }
    }
}
```

- [ ] **Step 3: `CartaPorteComplementHandler.cs`**
```csharp
using System.Collections.Generic;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Models;

namespace CFDI.BuildPdf.Complements
{
    /// <summary>Handler del complemento Carta Porte 3.1.</summary>
    internal sealed class CartaPorteComplementHandler : ComplementHandlerBase<CfdiCartaPorteViewModel>
    {
        public CartaPorteComplementHandler(
            ICfdiModelMapper<CfdiCartaPorteViewModel> mapper,
            IPdfDocumentBuilder<CfdiCartaPorteViewModel> builder)
            : base(mapper, builder) { }

        public override IReadOnlyCollection<string> ComplementNamespaces { get; }
            = new[] { "http://www.sat.gob.mx/CartaPorte31" };
    }
}
```

- [ ] **Step 4: `NominaComplementHandler.cs`**
```csharp
using System.Collections.Generic;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Models;

namespace CFDI.BuildPdf.Complements
{
    /// <summary>Handler del complemento Nómina 1.2.</summary>
    internal sealed class NominaComplementHandler : ComplementHandlerBase<CfdiNominaViewModel>
    {
        public NominaComplementHandler(
            ICfdiModelMapper<CfdiNominaViewModel> mapper,
            IPdfDocumentBuilder<CfdiNominaViewModel> builder)
            : base(mapper, builder) { }

        public override IReadOnlyCollection<string> ComplementNamespaces { get; }
            = new[] { "http://www.sat.gob.mx/nomina12" };
    }
}
```

- [ ] **Step 5: Build (compila aunque aún no se use)**
Run: `dotnet build CFDI.BuildPdf/CFDI.BuildPdf.csproj -c Debug`
Expected: `Build succeeded` (warnings ok).

- [ ] **Step 6: Commit**
```bash
git add CFDI.BuildPdf/Complements/
git commit -m "feat: abstracción de handlers de complemento (ICfdiComplementHandler + base + Carta Porte/Nómina) (F2)"
```

---

## Task 2: Conmutar el orquestador al registro de handlers

**Files:** Modify `CFDI.BuildPdf/Services/CfdiPdfGenerator.cs`, `CFDI.BuildPdf/Configuration/ServiceCollectionExtensions.cs`, `CFDI.BuildPdf/Service/CfdiPdf.cs`.

Estas 3 ediciones deben ir JUNTAS (cambian el constructor de `CfdiPdfGenerator` y sus dos sitios de construcción), de modo que la solución compile y corra.

- [ ] **Step 1: Reescribir `CfdiPdfGenerator.cs` (reemplazar TODO el archivo)**
```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Complements;

namespace CFDI.BuildPdf.Services
{
    /// <summary>
    /// Orquestador principal de generación de PDF desde XML CFDI.
    /// Detecta el complemento por namespace y delega en el <see cref="ICfdiComplementHandler"/> correspondiente.
    /// </summary>
    internal class CfdiPdfGenerator : ICfdiPdfGenerator
    {
        private readonly IReadOnlyList<ICfdiComplementHandler> _handlers;

        public CfdiPdfGenerator(IEnumerable<ICfdiComplementHandler> handlers)
        {
            if (handlers is null)
                throw new ArgumentNullException(nameof(handlers));

            _handlers = handlers.OrderByDescending(h => h.Priority).ToList();

            // Unicidad: dos handlers no pueden declarar el mismo namespace de complemento.
            var seen = new HashSet<string>();
            foreach (var handler in _handlers)
                foreach (var ns in handler.ComplementNamespaces)
                    if (!seen.Add(ns))
                        throw new InvalidOperationException(
                            $"Más de un handler declara el namespace de complemento '{ns}'.");
        }

        /// <inheritdoc />
        public Task<byte[]> GenerarDesdeRutaAsync(string rutaXml, CfdiPdfOptions? options = null)
        {
            if (string.IsNullOrWhiteSpace(rutaXml))
                throw new ArgumentException("La ruta del XML no puede ser nula ni vacía.", nameof(rutaXml));
            if (!File.Exists(rutaXml))
                throw new FileNotFoundException($"No se encontró el archivo XML en la ruta especificada: {rutaXml}", rutaXml);

            var xdoc = LoadXDocument(() => XDocument.Load(rutaXml), $"archivo '{rutaXml}'");
            return Task.FromResult(GenerarPdfInterno(xdoc, options));
        }

        /// <inheritdoc />
        public Task<byte[]> GenerarDesdeXmlStringAsync(string xmlContent, CfdiPdfOptions? options = null)
        {
            if (string.IsNullOrWhiteSpace(xmlContent))
                throw new ArgumentException("El contenido XML no puede ser nulo ni vacío.", nameof(xmlContent));

            var xdoc = LoadXDocument(() =>
            {
                using var reader = new StringReader(xmlContent);
                return XDocument.Load(reader);
            }, "cadena XML");

            return Task.FromResult(GenerarPdfInterno(xdoc, options));
        }

        /// <inheritdoc />
        public Task<byte[]> GenerarDesdeXmlBytesAsync(byte[] xmlBytes, CfdiPdfOptions? options = null)
        {
            if (xmlBytes is null)
                throw new ArgumentNullException(nameof(xmlBytes));
            if (xmlBytes.Length == 0)
                throw new ArgumentException("El arreglo de bytes del XML está vacío.", nameof(xmlBytes));

            var xdoc = LoadXDocument(() =>
            {
                using var ms = new MemoryStream(xmlBytes);
                return XDocument.Load(ms);
            }, "arreglo de bytes XML");

            return Task.FromResult(GenerarPdfInterno(xdoc, options));
        }

        /// <inheritdoc />
        public Task<byte[]> GenerarDesdeStreamAsync(Stream xmlStream, CfdiPdfOptions? options = null)
        {
            if (xmlStream is null)
                throw new ArgumentNullException(nameof(xmlStream));
            if (!xmlStream.CanRead)
                throw new ArgumentException("El Stream proporcionado no es legible.", nameof(xmlStream));

            var xdoc = LoadXDocument(() => XDocument.Load(xmlStream), "Stream XML");
            return Task.FromResult(GenerarPdfInterno(xdoc, options));
        }

        private static XDocument LoadXDocument(Func<XDocument> loader, string origen)
        {
            try
            {
                return loader();
            }
            catch (XmlException ex)
            {
                throw new CfdiXmlInvalidoException(
                    $"El {origen} no contiene un XML válido: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Lógica central: selecciona el handler cuyo namespace de complemento esté presente y delega.
        /// </summary>
        private byte[] GenerarPdfInterno(XDocument xdoc, CfdiPdfOptions? options)
        {
            var opts = options ?? new CfdiPdfOptions();
            var handler = ResolveHandler(xdoc);

            if (handler is null)
                throw new CfdiComplementoNoSoportadoException(
                    "Tipo de CFDI no soportado. Actualmente la librería solo soporta Carta Porte 3.1 y Nómina 1.2.");

            return handler.Generate(xdoc, opts);
        }

        /// <summary>
        /// Devuelve el handler de mayor prioridad cuyo namespace de complemento aparece en el documento.
        /// </summary>
        private ICfdiComplementHandler? ResolveHandler(XDocument xdoc)
        {
            var root = xdoc.Root;
            if (root is null)
                return null;

            var present = new HashSet<string>(root.Descendants().Select(e => e.Name.NamespaceName));
            // _handlers ya está ordenado por Priority descendente.
            return _handlers.FirstOrDefault(h => h.ComplementNamespaces.Any(present.Contains));
        }
    }
}
```

- [ ] **Step 2: Registrar los handlers en `ServiceCollectionExtensions.cs`**
Añadir `using CFDI.BuildPdf.Complements;` a los usings. Luego, en `AddCfdiPdfServices`, JUSTO ANTES de la línea `// Orquestador` / `services.AddTransient<ICfdiPdfGenerator, CfdiPdfGenerator>();`, añadir:
```csharp
            // Handlers de complemento (Transient: sin estado, ligeros)
            services.AddTransient<ICfdiComplementHandler, CartaPorteComplementHandler>();
            services.AddTransient<ICfdiComplementHandler, NominaComplementHandler>();
```
Dejar el resto igual (incluida la línea `services.AddTransient<ICfdiTypeDetector, CfdiTypeDetector>();`, que se conserva como utilidad pública). El `CfdiPdfGenerator` ahora resolverá `IEnumerable<ICfdiComplementHandler>` (los dos registrados).

- [ ] **Step 3: Construir los handlers en la fachada `CfdiPdf.cs`**
Añadir `using CFDI.BuildPdf.Complements;`. Reemplazar el cuerpo de la factory `_generator` (la parte que hace `return new CfdiPdfGenerator(... 5 args ...)`) por:
```csharp
            var qrGenerator = new QrGeneratorService();

            var handlers = new ICfdiComplementHandler[]
            {
                new CartaPorteComplementHandler(new CartaPorteMapper(qrGenerator), new CartaPorteDocumentBuilder()),
                new NominaComplementHandler(new NominaMapper(qrGenerator), new NominaDocumentBuilder())
            };

            return new CfdiPdfGenerator(handlers);
```
(Conserva la línea `QuestPDF.Settings.License = MapLicense(_licenseType);` que está antes. Mantén los `using` de Mappers/PdfBuilders que siguen usándose. El `using CFDI.BuildPdf.Services;` sigue siendo necesario para `CfdiPdfGenerator`.)

- [ ] **Step 4: Build + suite completa**
Run: `dotnet test CFDI.BuildPdf.sln`
Expected: todo verde (~70). Luego `git status --porcelain` → NINGÚN `*.viewmodel.json` modificado. Los smoke de Carta Porte y Nómina (happy + edge) deben seguir verdes — eso prueba que el despacho por namespace enruta idéntico al `switch`. Si un smoke falla con `CfdiComplementoNoSoportadoException`, el namespace declarado en el handler no coincide con el del fixture → revisar.

- [ ] **Step 5: Commit**
```bash
git add CFDI.BuildPdf/Services/CfdiPdfGenerator.cs CFDI.BuildPdf/Configuration/ServiceCollectionExtensions.cs CFDI.BuildPdf/Service/CfdiPdf.cs
git commit -m "refactor: despacho por registro de handlers en CfdiPdfGenerator (sin switch) (F2)"
```

---

## Task 3: Eliminar `CanMap` y migrar sus tests

**Files:** Modify `ICfdiModelMapper.cs`, `BaseCfdiMapper.cs`, `CartaPorteMapper.cs`, `NominaMapper.cs`; create `ComplementDispatchTests.cs`; edit `CartaPorteMapperTests.cs`, `NominaMapperTests.cs`.

`CanMap` no lo usa producción (el despacho ahora es por namespace en el orquestador). Se elimina de la interfaz y las implementaciones, y sus 4 tests se migran.

- [ ] **Step 1: Quitar `CanMap` de la interfaz**
En `CFDI.BuildPdf/Abstractions/ICfdiModelMapper.cs`, eliminar el método `bool CanMap(XDocument xdoc);` y su doc-comment. Conservar `Map`. (Si `using System.Xml.Linq;` queda sin usar tras quitarlo, déjalo igual: `Map(XDocument)` aún lo usa.)

- [ ] **Step 2: Quitar `CanMap` de la base y las implementaciones**
- `CFDI.BuildPdf/Mappers/Common/BaseCfdiMapper.cs`: eliminar `public abstract bool CanMap(XDocument xdoc);` (línea ~33) y su doc-comment.
- `CFDI.BuildPdf/Mappers/CartaPorte/CartaPorteMapper.cs`: eliminar el método `public override bool CanMap(XDocument xdoc) { ... }` (~24-28). NO tocar el campo `Cp` (lo usa `MapComplemento`).
- `CFDI.BuildPdf/Mappers/Nomina/NominaMapper.cs`: eliminar el método `public override bool CanMap(XDocument xdoc) { ... }` (~24-28). NO tocar el campo `Nom`.

- [ ] **Step 3: Confirmar que `CanMap` desapareció**
Run: `git grep -n "CanMap" -- CFDI.BuildPdf`
Expected: SIN salida en código de producción. (Quedará en `CFDI.BuildPdf.Tests` hasta el Step 5; y en docs/CHANGELOG, lo cual es ok.)

- [ ] **Step 4: Crear `ComplementDispatchTests.cs` (migración)**
Crear `CFDI.BuildPdf.Tests/ComplementDispatchTests.cs`:
```csharp
using System.Threading.Tasks;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Complements;
using CFDI.BuildPdf.Mappers.CartaPorte;
using CFDI.BuildPdf.Mappers.Nomina;
using CFDI.BuildPdf.PdfBuilders.CartaPorte;
using CFDI.BuildPdf.PdfBuilders.Nomina;
using CFDI.BuildPdf.Service;
using CFDI.BuildPdf.Tests.Helpers;
using Xunit;

namespace CFDI.BuildPdf.Tests
{
    /// <summary>
    /// Reemplaza los antiguos tests de CanMap: el despacho ahora es por namespace de complemento.
    /// </summary>
    public class ComplementDispatchTests
    {
        [Fact]
        public void CartaPorteHandler_DeclaraNamespaceCartaPorte31()
        {
            var handler = new CartaPorteComplementHandler(
                new CartaPorteMapper(new FakeQrGenerator()), new CartaPorteDocumentBuilder());

            Assert.Contains("http://www.sat.gob.mx/CartaPorte31", handler.ComplementNamespaces);
            Assert.DoesNotContain("http://www.sat.gob.mx/nomina12", handler.ComplementNamespaces);
        }

        [Fact]
        public void NominaHandler_DeclaraNamespaceNomina12()
        {
            var handler = new NominaComplementHandler(
                new NominaMapper(new FakeQrGenerator()), new NominaDocumentBuilder());

            Assert.Contains("http://www.sat.gob.mx/nomina12", handler.ComplementNamespaces);
            Assert.DoesNotContain("http://www.sat.gob.mx/CartaPorte31", handler.ComplementNamespaces);
        }

        [Fact]
        public async Task CfdiSinComplementoSoportado_LanzaCfdiComplementoNoSoportado()
        {
            // CFDI 4.0 bien formado pero sin complemento Carta Porte ni Nómina.
            const string xmlSinComplemento =
                "<cfdi:Comprobante xmlns:cfdi=\"http://www.sat.gob.mx/cfd/4\" Version=\"4.0\" " +
                "Total=\"0\" SubTotal=\"0\"></cfdi:Comprobante>";

            await Assert.ThrowsAsync<CfdiComplementoNoSoportadoException>(
                () => CfdiPdf.DesdeXmlStringAsync(xmlSinComplemento));
        }
    }
}
```

- [ ] **Step 5: Quitar los 4 tests `CanMap` de los mappers**
- En `CFDI.BuildPdf.Tests/CartaPorteMapperTests.cs`: eliminar los métodos `CanMap_CartaPorteXml_RetornaTrue` y `CanMap_NominaXml_RetornaFalse` (y cualquier `using`/helper que quede sin uso solo por ellos). Conservar el resto de tests del mapper.
- En `CFDI.BuildPdf.Tests/NominaMapperTests.cs`: eliminar `CanMap_NominaXml_RetornaTrue` y `CanMap_CartaPorteXml_RetornaFalse`. Conservar el resto.

- [ ] **Step 6: Build + suite**
Run: `dotnet test CFDI.BuildPdf.sln`
Expected: todo verde. El conteo total cambia (−4 tests CanMap, +3 de dispatch). `git grep -n "CanMap" -- CFDI.BuildPdf.Tests` → SIN salida. Baselines sin cambios.

- [ ] **Step 7: Commit**
```bash
git add CFDI.BuildPdf/Abstractions/ICfdiModelMapper.cs CFDI.BuildPdf/Mappers/Common/BaseCfdiMapper.cs CFDI.BuildPdf/Mappers/CartaPorte/CartaPorteMapper.cs CFDI.BuildPdf/Mappers/Nomina/NominaMapper.cs CFDI.BuildPdf.Tests/ComplementDispatchTests.cs CFDI.BuildPdf.Tests/CartaPorteMapperTests.cs CFDI.BuildPdf.Tests/NominaMapperTests.cs
git commit -m "refactor: eliminar CanMap y migrar sus tests al despacho por namespace (D2/O2) (F2)"
```

---

## Task 4: Verificación + cierre de F2

**Files:** ninguno (verificación).

- [ ] **Step 1: Suite completa con cobertura**
Run: `dotnet test CFDI.BuildPdf.sln --collect:"XPlat Code Coverage"`
Expected: `Passed!` 0 fallos.

- [ ] **Step 2: Prueba de escalabilidad (revisión manual, no código): confirmar Open/Closed**
Verifica leyendo que añadir un complemento nuevo requeriría SOLO: (a) un mapper + builder (ya existe el patrón), (b) una clase `XxxComplementHandler` declarando su namespace, (c) UNA línea de registro en `ServiceCollectionExtensions` y una entrada en el arreglo de la fachada. `CfdiPdfGenerator` NO se toca. Documenta esta confirmación en el reporte.

- [ ] **Step 3: Baselines intactos + repo limpio**
Run: `git log --oneline f1-reuso..HEAD -- "CFDI.BuildPdf.Tests/Golden/Snapshots/"`
Expected: SIN salida (ningún baseline tocado en F2).
Run: `git status --porcelain` (vacío).

- [ ] **Step 4: Tag de checkpoint**
```bash
git tag -f f2-nucleo -m "F2: núcleo de arquitectura (registro de handlers por namespace, sin switch, sin CanMap)"
```

---

## Self-Review

**Spec coverage (F2):**
- `ICfdiComplementHandler` + `ComplementHandlerBase<TModel>` + handlers Carta Porte/Nómina → Task 1 ✅
- Reescribir `CfdiPdfGenerator` con despacho por namespace (sin `switch`) → Task 2 ✅
- Mover el patch de `LogoBase64` a la base del handler → Task 1 (`ComplementHandlerBase.Generate`) ✅
- Detección interna por namespace; el orquestador deja de usar `ICfdiTypeDetector` → Task 2 ✅
- Eliminar `CanMap` (D2/O2) y migrar los 4 tests → Task 3 ✅
- Determinismo: unicidad de namespaces (error en arranque) + desempate por `Priority` → Task 2 (ctor + ResolveHandler) ✅

**Placeholder scan:** código completo en cada `Create`; el rewrite de `CfdiPdfGenerator` se da como archivo íntegro; las ediciones de mappers/DI/fachada son puntuales y precisas. Sin TBD/TODO.

**Consistencia de tipos:** `ICfdiComplementHandler` (ComplementNamespaces/Priority/Generate) usado idéntico por la base, los 2 handlers, el orquestador (IEnumerable), DI (AddTransient) y la fachada (arreglo). `ComplementHandlerBase<TModel> where TModel : CfdiViewModelBase` satisface las restricciones de `ICfdiModelMapper<TModel>` (class) e `IPdfDocumentBuilder<in TModel>` (CfdiViewModelBase). El `LogoBase64` se aplica en un solo sitio.

**Riesgo principal:** que el despacho por namespace no enrute igual que el `switch`+`Detect`. Mitigación: ambos fixtures (CartaPorte con elementos `{CartaPorte31}`, Nómina con `{nomina12}`) los enruta el `ResolveHandler` por presencia de namespace; los smoke (happy + edge) + snapshots son el oráculo; el caso "sin complemento" se cubre con el test de dispatch nuevo.

**Decisiones deliberadas:**
- `ICfdiTypeDetector`/`CfdiType`/`CfdiTypeDetector` se conservan sin cambios (utilidad pública); el orquestador ya no los usa. Su visibilidad/eliminación se decide en F4. Sus tests (`CfdiTypeDetectorTests`) siguen verdes sin migración.
- Async sigue siendo `Task.FromResult` (X3) — el async honesto se aborda en F3, no aquí.

**Notas para F3:** el grafo de la fachada (`CfdiPdf.cs`) y el de DI (`ServiceCollectionExtensions`) construyen los mismos handlers en dos lugares; F3 unifica ambos en un `CfdiPdfFactory` (composition root) compartido.
