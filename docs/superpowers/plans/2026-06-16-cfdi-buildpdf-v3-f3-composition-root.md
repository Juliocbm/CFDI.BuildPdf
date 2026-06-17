# CFDI.BuildPdf v3 — Fase F3: composition root + fachada (async, licencia, logging) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Unificar el grafo duplicado fachada-vs-DI en un único composition root (`CfdiPdfFactory`), hacer el I/O honestamente asíncrono, volver idempotente la licencia QuestPDF (sin degradación silenciosa) y permitir inyectar logging en el camino de la fachada — todo **sin cambiar el PDF generado** (golden tests son el oráculo).

**Architecture:** `CfdiPdfFactory.CreateGenerator(ILoggerFactory?)` (internal) define en UN solo lugar el grafo `mapper→builder→handler→orquestador`. La fachada estática y `AddCfdiPdfServices` lo usan ambos. La licencia se aplica solo si no está ya configurada (`ConfigureQuestPdfLicense` la fuerza explícitamente). El orquestador carga el XML con `XDocument.LoadAsync`.

**Tech Stack:** .NET 8.0, xUnit, QuestPDF, PdfPig (tests), Microsoft.Extensions.DependencyInjection (tests).

**Spec:** `docs/superpowers/specs/2026-06-16-cfdi-buildpdf-v3-enterprise-design.md` (§5/§8, F3). **Base:** tag `f2-nucleo`. **Rama:** `Refactor` (commits LOCALES; NO push hasta autorización del usuario).

**Invariante de F3:** tras cada cambio, `dotnet test CFDI.BuildPdf.sln` → todo verde y `git status --porcelain` sin ningún `*.viewmodel.json` modificado. La generación no cambia (mismo grafo, misma carga de XML); solo cambian el cableado, el async y la política de licencia/logging.

---

## File Structure (F3)

- Create: `CFDI.BuildPdf/Configuration/CfdiPdfFactory.cs` — composition root interno.
- Modify: `CFDI.BuildPdf/Service/CfdiPdf.cs` — usar el factory; `ConfigureLogging`; licencia idempotente.
- Modify: `CFDI.BuildPdf/Configuration/ServiceCollectionExtensions.cs` — registrar el orquestador vía el factory; licencia idempotente.
- Modify: `CFDI.BuildPdf/Services/CfdiPdfGenerator.cs` — `XDocument.LoadAsync` (async honesto).
- Modify: `CFDI.BuildPdf.Tests/CFDI.BuildPdf.Tests.csproj` — referencia a `Microsoft.Extensions.DependencyInjection`.
- Create: `CFDI.BuildPdf.Tests/AssemblyConfig.cs` — desactivar paralelización (la licencia QuestPDF es un global de proceso).
- Create: `CFDI.BuildPdf.Tests/CompositionRootTests.cs` — factory cablea loggers; DI resuelve y genera; licencia idempotente.

---

## Task 1: Composition root + fachada + ConfigureLogging + licencia idempotente

**Files:** Create `CFDI.BuildPdf/Configuration/CfdiPdfFactory.cs`; Modify `CFDI.BuildPdf/Service/CfdiPdf.cs`.

- [ ] **Step 1: Crear `CfdiPdfFactory.cs`**
```csharp
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Complements;
using CFDI.BuildPdf.Helpers;
using CFDI.BuildPdf.Mappers.CartaPorte;
using CFDI.BuildPdf.Mappers.Nomina;
using CFDI.BuildPdf.PdfBuilders.CartaPorte;
using CFDI.BuildPdf.PdfBuilders.Nomina;
using CFDI.BuildPdf.Services;
using Microsoft.Extensions.Logging;

namespace CFDI.BuildPdf.Configuration
{
    /// <summary>
    /// Composition root interno: define en UN solo lugar el grafo mapper→builder→handler→orquestador.
    /// Lo usan tanto la fachada estática (<see cref="Service.CfdiPdf"/>) como
    /// <see cref="ServiceCollectionExtensions.AddCfdiPdfServices"/>, evitando duplicar el cableado.
    /// </summary>
    internal static class CfdiPdfFactory
    {
        /// <summary>
        /// Construye el orquestador con todos los handlers de complemento soportados.
        /// </summary>
        /// <param name="loggerFactory">Factory opcional para inyectar loggers en los mappers.</param>
        public static ICfdiPdfGenerator CreateGenerator(ILoggerFactory? loggerFactory = null)
        {
            var qrGenerator = new QrGeneratorService();

            var handlers = new ICfdiComplementHandler[]
            {
                new CartaPorteComplementHandler(
                    new CartaPorteMapper(qrGenerator, loggerFactory?.CreateLogger<CartaPorteMapper>()),
                    new CartaPorteDocumentBuilder()),
                new NominaComplementHandler(
                    new NominaMapper(qrGenerator, loggerFactory?.CreateLogger<NominaMapper>()),
                    new NominaDocumentBuilder())
            };

            return new CfdiPdfGenerator(handlers);
        }
    }
}
```

- [ ] **Step 2: Actualizar los `using` de `CfdiPdf.cs`**
En `CFDI.BuildPdf/Service/CfdiPdf.cs`, reemplazar el bloque de `using` superior por:
```csharp
using System;
using System.IO;
using System.Threading.Tasks;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Configuration;
using Microsoft.Extensions.Logging;
using QuestPDF.Infrastructure;
```
(Se eliminan los usings de `Complements`, `Helpers`, `Mappers.*`, `PdfBuilders.*`, `Services` — ya no se usan porque el factory construye el grafo.)

- [ ] **Step 3: Añadir el campo `_loggerFactory` y volver idempotente el Lazy**
En `CfdiPdf`, reemplazar la declaración del campo `_licenseType` y el `Lazy` por:
```csharp
        private static CfdiPdfLicenseType _licenseType = CfdiPdfLicenseType.Community;
        private static ILoggerFactory? _loggerFactory;

        private static readonly Lazy<ICfdiPdfGenerator> _generator = new(() =>
        {
            // Idempotente: no pisar una licencia ya configurada explícitamente.
            if (QuestPDF.Settings.License is null)
                QuestPDF.Settings.License = MapLicense(_licenseType);

            return CfdiPdfFactory.CreateGenerator(_loggerFactory);
        });
```

- [ ] **Step 4: `ConfigureQuestPdfLicense` aplica siempre (intención explícita)**
Reemplazar el cuerpo de `ConfigureQuestPdfLicense` por:
```csharp
        public static void ConfigureQuestPdfLicense(CfdiPdfLicenseType licenseType)
        {
            _licenseType = licenseType;
            // Intención explícita del consumidor: aplicar de inmediato (puede sobre-escribir el default idempotente).
            QuestPDF.Settings.License = MapLicense(licenseType);
        }
```

- [ ] **Step 5: Añadir `ConfigureLogging`**
Inmediatamente después de `ConfigureQuestPdfLicense`, añadir:
```csharp
        /// <summary>
        /// Configura el <see cref="ILoggerFactory"/> para el diagnóstico de los mappers en el camino de la fachada estática.
        /// Debe llamarse una sola vez al inicio del proceso, antes del primer uso. Si no se llama, no se emite logging.
        /// </summary>
        /// <param name="loggerFactory">Factory de loggers de tu aplicación.</param>
        public static void ConfigureLogging(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }
```
(El método `MapLicense` y todos los métodos `Desde*Async`/`Guardar*`/`Escribir*` quedan SIN cambios.)

- [ ] **Step 6: Build + suite**
Run: `dotnet test CFDI.BuildPdf.sln`
Expected: todo verde (~71 — la suite actual). `git status --porcelain` → ningún `*.viewmodel.json` modificado.

- [ ] **Step 7: Commit**
```bash
git add CFDI.BuildPdf/Configuration/CfdiPdfFactory.cs CFDI.BuildPdf/Service/CfdiPdf.cs
git commit -m "refactor: composition root CfdiPdfFactory + ConfigureLogging + licencia idempotente en la fachada (D1/X4) (F3)"
```

---

## Task 2: DI usa el composition root + test de integración DI + licencia idempotente

**Files:** Modify `ServiceCollectionExtensions.cs`, `CFDI.BuildPdf.Tests.csproj`; Create `AssemblyConfig.cs`, `CompositionRootTests.cs`.

- [ ] **Step 1: Reescribir `AddCfdiPdfServices` para usar el factory**
En `CFDI.BuildPdf/Configuration/ServiceCollectionExtensions.cs`, reemplazar el cuerpo del método `AddCfdiPdfServices` (de la línea `if (services is null)` hasta `return services;`) por:
```csharp
            if (services is null)
                throw new ArgumentNullException(nameof(services));

            // Idempotente: no pisar una licencia ya configurada (evita degradación silenciosa).
            if (QuestPDF.Settings.License is null)
                QuestPDF.Settings.License = CfdiPdf.MapLicense(licenseType);

            if (configure != null)
                services.Configure(configure);

            // Orquestador construido por el composition root compartido (usa el ILoggerFactory del contenedor si está).
            services.AddTransient<ICfdiPdfGenerator>(sp => CfdiPdfFactory.CreateGenerator(sp.GetService<ILoggerFactory>()));

            // Utilidad pública de detección de tipo (no usada por el orquestador; su visibilidad se decide en F4).
            services.AddTransient<ICfdiTypeDetector, CfdiTypeDetector>();

            return services;
```
Luego ajustar los `using` del archivo: quitar los que quedan sin uso (`CFDI.BuildPdf.Complements`, `CFDI.BuildPdf.Helpers`, `CFDI.BuildPdf.Mappers.CartaPorte`, `CFDI.BuildPdf.Mappers.Nomina`, `CFDI.BuildPdf.Models`, `CFDI.BuildPdf.PdfBuilders.CartaPorte`, `CFDI.BuildPdf.PdfBuilders.Nomina`) y AÑADIR `using Microsoft.Extensions.Logging;`. Conservar `using CFDI.BuildPdf.Abstractions;`, `using CFDI.BuildPdf.Service;` (para `CfdiPdf.MapLicense`), `using CFDI.BuildPdf.Services;` (para `CfdiTypeDetector`), `using Microsoft.Extensions.DependencyInjection;`, `using System;`. (El grafo de mappers/builders/handlers ya NO se registra individualmente: lo arma el factory. `CfdiTypeDetector` se sigue registrando como utilidad pública.)

- [ ] **Step 2: Build de la librería**
Run: `dotnet build CFDI.BuildPdf/CFDI.BuildPdf.csproj -c Debug`
Expected: `Build succeeded`. Si algún `using` quedó necesario, el compilador lo dirá; ajústalo.

- [ ] **Step 3: Añadir el paquete DI a los tests**
En `CFDI.BuildPdf.Tests/CFDI.BuildPdf.Tests.csproj`, dentro del `ItemGroup` de `PackageReference`, añadir:
```xml
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
```

- [ ] **Step 4: Desactivar la paralelización de tests (licencia QuestPDF = global de proceso)**
Crear `CFDI.BuildPdf.Tests/AssemblyConfig.cs`:
```csharp
using Xunit;

// La licencia QuestPDF (QuestPDF.Settings.License) y la fachada estática son estado global de proceso.
// Serializamos los tests para que los que lo manipulan no compitan con los que generan PDFs.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

- [ ] **Step 5: Crear `CompositionRootTests.cs`**
```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Configuration;
using CFDI.BuildPdf.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CFDI.BuildPdf.Tests
{
    public class CompositionRootTests
    {
        [Fact]
        public void Factory_CableaLoggersEnLosMappers()
        {
            var spy = new SpyLoggerFactory();

            var generator = CfdiPdfFactory.CreateGenerator(spy);

            Assert.NotNull(generator);
            // El factory crea un logger por cada mapper a través del ILoggerFactory provisto.
            Assert.Contains(spy.CategoriasCreadas, c => c.Contains("CartaPorteMapper"));
            Assert.Contains(spy.CategoriasCreadas, c => c.Contains("NominaMapper"));
        }

        [Fact]
        public async Task DI_ResuelveOrquestadorYGeneraPdf()
        {
            var services = new ServiceCollection();
            services.AddCfdiPdfServices();
            using var provider = services.BuildServiceProvider();

            var generator = provider.GetRequiredService<ICfdiPdfGenerator>();
            var xml = TestXmlLoader.LoadCartaPorte().ToString();

            var pdf = await generator.GenerarDesdeXmlStringAsync(xml);

            Assert.True(pdf.Length > 1000);
            Assert.Equal((byte)'%', pdf[0]);
        }

        [Fact]
        public void Licencia_NoSeDegradaSiYaEstaConfigurada()
        {
            var original = QuestPDF.Settings.License;
            try
            {
                QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Enterprise;

                // AddCfdiPdfServices con Community por defecto NO debe pisar la licencia ya configurada.
                new ServiceCollection().AddCfdiPdfServices(licenseType: CfdiPdfLicenseType.Community);

                Assert.Equal(QuestPDF.Infrastructure.LicenseType.Enterprise, QuestPDF.Settings.License);
            }
            finally
            {
                QuestPDF.Settings.License = original;
            }
        }

        private sealed class SpyLoggerFactory : ILoggerFactory
        {
            public List<string> CategoriasCreadas { get; } = new();
            public ILogger CreateLogger(string categoryName)
            {
                CategoriasCreadas.Add(categoryName);
                return Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
            }
            public void AddProvider(ILoggerProvider provider) { }
            public void Dispose() { }
        }
    }
}
```

- [ ] **Step 6: Build + suite**
Run: `dotnet test CFDI.BuildPdf.sln`
Expected: todo verde, +3 tests nuevos. `git status --porcelain` → ningún `*.viewmodel.json` modificado. (Si `DI_ResuelveOrquestadorYGeneraPdf` falla por licencia, confirma que `AddCfdiPdfServices` la fija a Community cuando está null.)

- [ ] **Step 7: Commit**
```bash
git add CFDI.BuildPdf/Configuration/ServiceCollectionExtensions.cs CFDI.BuildPdf.Tests/CFDI.BuildPdf.Tests.csproj CFDI.BuildPdf.Tests/AssemblyConfig.cs CFDI.BuildPdf.Tests/CompositionRootTests.cs
git commit -m "refactor: DI usa el composition root + tests de DI/logger/licencia idempotente (D1/X4) (F3)"
```

---

## Task 3: I/O asíncrono honesto (X3)

**Files:** Modify `CFDI.BuildPdf/Services/CfdiPdfGenerator.cs`.

Reemplazar `Task.FromResult(...)` por carga asíncrona real con `XDocument.LoadAsync`. La generación (CPU-bound) sigue síncrona tras el `await`.

- [ ] **Step 1: Reescribir los 4 `Generar*Async` y el helper de carga**
En `CFDI.BuildPdf/Services/CfdiPdfGenerator.cs`:
- Añadir `using System.Threading;` a los usings (para `CancellationToken`).
- Reemplazar los 4 métodos `Generar*Async` y el helper `LoadXDocument` por estas versiones (el constructor, `GenerarPdfInterno` y `ResolveHandler` quedan IGUAL):
```csharp
        /// <inheritdoc />
        public async Task<byte[]> GenerarDesdeRutaAsync(string rutaXml, CfdiPdfOptions? options = null)
        {
            if (string.IsNullOrWhiteSpace(rutaXml))
                throw new ArgumentException("La ruta del XML no puede ser nula ni vacía.", nameof(rutaXml));
            if (!File.Exists(rutaXml))
                throw new FileNotFoundException($"No se encontró el archivo XML en la ruta especificada: {rutaXml}", rutaXml);

            await using var stream = new FileStream(rutaXml, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            var xdoc = await LoadXDocumentAsync(() => XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None), $"archivo '{rutaXml}'");
            return GenerarPdfInterno(xdoc, options);
        }

        /// <inheritdoc />
        public async Task<byte[]> GenerarDesdeXmlStringAsync(string xmlContent, CfdiPdfOptions? options = null)
        {
            if (string.IsNullOrWhiteSpace(xmlContent))
                throw new ArgumentException("El contenido XML no puede ser nulo ni vacío.", nameof(xmlContent));

            using var reader = new StringReader(xmlContent);
            var xdoc = await LoadXDocumentAsync(() => XDocument.LoadAsync(reader, LoadOptions.None, CancellationToken.None), "cadena XML");
            return GenerarPdfInterno(xdoc, options);
        }

        /// <inheritdoc />
        public async Task<byte[]> GenerarDesdeXmlBytesAsync(byte[] xmlBytes, CfdiPdfOptions? options = null)
        {
            if (xmlBytes is null)
                throw new ArgumentNullException(nameof(xmlBytes));
            if (xmlBytes.Length == 0)
                throw new ArgumentException("El arreglo de bytes del XML está vacío.", nameof(xmlBytes));

            using var ms = new MemoryStream(xmlBytes);
            var xdoc = await LoadXDocumentAsync(() => XDocument.LoadAsync(ms, LoadOptions.None, CancellationToken.None), "arreglo de bytes XML");
            return GenerarPdfInterno(xdoc, options);
        }

        /// <inheritdoc />
        public async Task<byte[]> GenerarDesdeStreamAsync(Stream xmlStream, CfdiPdfOptions? options = null)
        {
            if (xmlStream is null)
                throw new ArgumentNullException(nameof(xmlStream));
            if (!xmlStream.CanRead)
                throw new ArgumentException("El Stream proporcionado no es legible.", nameof(xmlStream));

            var xdoc = await LoadXDocumentAsync(() => XDocument.LoadAsync(xmlStream, LoadOptions.None, CancellationToken.None), "Stream XML");
            return GenerarPdfInterno(xdoc, options);
        }

        private static async Task<XDocument> LoadXDocumentAsync(Func<Task<XDocument>> loader, string origen)
        {
            try
            {
                return await loader();
            }
            catch (XmlException ex)
            {
                throw new CfdiXmlInvalidoException(
                    $"El {origen} no contiene un XML válido: {ex.Message}", ex);
            }
        }
```

- [ ] **Step 2: Build + suite**
Run: `dotnet test CFDI.BuildPdf.sln`
Expected: todo verde. `git status --porcelain` → ningún `*.viewmodel.json` modificado. Los smoke (que `await` la generación) prueban que la carga async produce el mismo PDF. El test `CfdiSinComplementoSoportado` (que espera `CfdiComplementoNoSoportadoException`) sigue verde (ahora la excepción surge al await, comportamiento equivalente para quien await).

- [ ] **Step 3: Commit**
```bash
git add CFDI.BuildPdf/Services/CfdiPdfGenerator.cs
git commit -m "refactor: carga de XML asíncrona honesta con XDocument.LoadAsync (X3) (F3)"
```

---

## Task 4: Verificación + cierre de F3

**Files:** ninguno (verificación).

- [ ] **Step 1: Suite completa con cobertura**
Run: `dotnet test CFDI.BuildPdf.sln --collect:"XPlat Code Coverage"`
Expected: `Passed!` 0 fallos.

- [ ] **Step 2: Confirmar un solo composition root**
Verifica leyendo: el grafo `new CartaPorteComplementHandler(... new CartaPorteMapper ...)` aparece SOLO en `CfdiPdfFactory.cs` (ni en la fachada ni en `ServiceCollectionExtensions`). `git grep -n "new CartaPorteComplementHandler\|new NominaComplementHandler" -- CFDI.BuildPdf` → solo en `CfdiPdfFactory.cs`. Documenta en el reporte.

- [ ] **Step 3: Baselines intactos + repo limpio**
Run: `git log --oneline f2-nucleo..HEAD -- "CFDI.BuildPdf.Tests/Golden/Snapshots/"`
Expected: SIN salida.
Run: `git status --porcelain` (vacío).

- [ ] **Step 4: Tag de checkpoint**
```bash
git tag -f f3-composition -m "F3: composition root (CfdiPdfFactory), async honesto, licencia idempotente, ConfigureLogging"
```

---

## Self-Review

**Spec coverage (F3):**
- `CfdiPdfFactory` composition root compartido por fachada y DI (D1) → Tasks 1 y 2 ✅
- Licencia idempotente (X4) → Tasks 1 (fachada) y 2 (DI) + test ✅
- `ConfigureLogging(ILoggerFactory)` en la fachada → Task 1 ✅
- I/O async honesto (`XDocument.LoadAsync`) (X3) → Task 3 ✅
- Test de integración DI (camino antes no cubierto) → Task 2 ✅

**Placeholder scan:** código completo en cada `Create`; ediciones de fachada/DI/generador precisas; sin TBD/TODO.

**Consistencia:** `CfdiPdfFactory.CreateGenerator(ILoggerFactory?)` se usa idéntico en la fachada (`_loggerFactory`) y en DI (`sp.GetService<ILoggerFactory>()`). El grafo mapper→builder→handler→orquestador queda en un solo sitio. `MapLicense` (interno, en `CfdiPdf`) lo siguen usando ambos sitios de licencia.

**Riesgos:**
- El cambio de async podría alterar el momento en que se lanzan `ArgumentException`/`FileNotFoundException` (ahora dentro del Task en vez de síncrono). Para consumidores que `await` (incluidos todos los tests/smoke) el comportamiento es idéntico. Mitigación: golden/smoke verdes.
- La licencia es un global de proceso; los tests que la tocan se serializan vía `DisableTestParallelization`. Mitigación: el test de idempotencia guarda/restaura el valor.
- Quitar las registraciones individuales de DI (mappers/builders/handlers) cambia el grafo de DI: los consumidores ya NO pueden resolver `ICfdiModelMapper<T>` etc. del contenedor — aceptable (son internos en caja cerrada; el contrato público es `ICfdiPdfGenerator`). El test de integración DI confirma que `ICfdiPdfGenerator` resuelve y genera.

**Notas para F4:** `services.Configure(configure)` registra `IOptions<CfdiPdfOptions>` pero nada lo consume (las opciones se pasan por llamada). Revisar en F4 si conviene conectarlo o documentar que `configure` fija defaults que hoy no se aplican automáticamente. La idempotencia de licencia asume "primera configuración gana"; documentar en README (F5) que se configure una vez al inicio.
