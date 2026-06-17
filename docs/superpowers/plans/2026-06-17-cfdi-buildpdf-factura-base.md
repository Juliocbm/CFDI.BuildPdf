# Factura base CFDI 4.0 (Ingreso/Egreso) — Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Generar el PDF de facturas CFDI 4.0 base (TipoDeComprobante `I` Ingreso y `E` Egreso) sin complemento Carta Porte ni Nómina.

**Architecture:** Se evoluciona el despacho de handlers de "casado por namespace" a un predicado `CanHandle(XDocument)`. Se extraen las secciones de render genéricas (conceptos, totales, cliente/emisión, forma de pago) y el mapeo genérico de conceptos/impuestos —hoy dentro de Carta Porte— a puntos compartidos, reutilizados por la nueva factura base y por Carta Porte (DRY). Se agregan `CfdiFacturaViewModel`, `FacturaMapper`, `FacturaDocumentBuilder`, `FacturaComplementHandler`. Sin breaking changes (3.1.0).

**Tech Stack:** .NET 8, C#, QuestPDF 2024.3.5, xUnit, PdfPig (UglyToad.PdfPig), System.Text.Json (golden snapshots).

**Decisiones del spec confirmadas en el plan:**
- Alcance: `I` y `E` únicamente. `T`/`P` siguen lanzando `CfdiComplementoNoSoportadoException`.
- NO se introduce base intermedia `CfdiComprobanteViewModel` (cambiaría el orden JSON del golden de Carta Porte). Cada VM declara sus campos; el DRY se da en mapper/render.
- Preservación de comportamiento de Carta Porte verificada con un golden de **texto extraído del PDF** (Task 1) + los golden de ViewModel existentes.

**Comandos base:**
- Compilar: `dotnet build CFDI.BuildPdf.sln -c Release`
- Tests: `dotnet test CFDI.BuildPdf.sln -c Release`
- Un test: `dotnet test CFDI.BuildPdf.sln -c Release --filter "FullyQualifiedName~<Nombre>"`
- Nota golden: `Snapshot.Match` crea el baseline en `CFDI.BuildPdf.Tests/Golden/Snapshots/` la primera vez y **falla** pidiendo revisión; al re-ejecutar, pasa. Revisar siempre el baseline creado antes de confirmarlo.

---

## Mapa de archivos

| Acción | Archivo | Responsabilidad |
|---|---|---|
| Crear | `CFDI.BuildPdf.Tests/Golden/PdfTextRegressionTests.cs` | Red de seguridad: texto del PDF de Carta Porte no cambia |
| Modificar | `CFDI.BuildPdf/Complements/ICfdiComplementHandler.cs` | Contrato `CanHandle(XDocument)` |
| Crear | `CFDI.BuildPdf/Complements/IComplementNamespacesProvider.cs` | Expone namespaces para el guard de unicidad |
| Crear | `CFDI.BuildPdf/Complements/CfdiHandlerBase.cs` | Flujo `Generate` (mapper→logo→builder) compartido |
| Modificar | `CFDI.BuildPdf/Complements/ComplementHandlerBase.cs` | Hereda de `CfdiHandlerBase`; `CanHandle` por namespace |
| Modificar | `CFDI.BuildPdf/Services/CfdiPdfGenerator.cs` | Despacho por `CanHandle`; guard por `IComplementNamespacesProvider`; mensaje |
| Crear | `CFDI.BuildPdf/Models/ConceptoViewModel.cs` | Modelos genéricos movidos desde el VM de Carta Porte |
| Modificar | `CFDI.BuildPdf/Models/CfdiCartaPorteViewModel.cs` | Quita los modelos movidos |
| Crear | `CFDI.BuildPdf/Models/CfdiFacturaViewModel.cs` | ViewModel de factura base |
| Modificar | `CFDI.BuildPdf/Mappers/Common/BaseCfdiMapper.cs` | Helpers `MapConceptos` / `MapResumenImpuestos` |
| Modificar | `CFDI.BuildPdf/Mappers/CartaPorte/CartaPorteMapper.cs` | Usa los helpers compartidos |
| Crear | `CFDI.BuildPdf/Mappers/Factura/FacturaMapper.cs` | Mapper de factura base |
| Crear | `CFDI.BuildPdf/PdfBuilders/Common/ComprobanteSections.cs` | Secciones de render genéricas |
| Modificar | `CFDI.BuildPdf/PdfBuilders/CartaPorte/CartaPorteDocumentBuilder.cs` | Usa `ComprobanteSections` |
| Crear | `CFDI.BuildPdf/PdfBuilders/Factura/FacturaDocumentBuilder.cs` | Builder de factura base |
| Crear | `CFDI.BuildPdf/Complements/FacturaComplementHandler.cs` | Handler factura (CanHandle por tipo) |
| Modificar | `CFDI.BuildPdf/Configuration/CfdiPdfFactory.cs` | Registra el handler de factura |
| Crear | `CFDI.BuildPdf.Tests/TestData/cfdi_factura_ingreso.xml` | Fixture sintético Ingreso |
| Crear | `CFDI.BuildPdf.Tests/TestData/cfdi_factura_egreso.xml` | Fixture sintético Egreso |
| Modificar | `CFDI.BuildPdf.Tests/Helpers/TestXmlLoader.cs` | Loaders de los fixtures |
| Modificar | `CFDI.BuildPdf.Tests/Golden/ViewModelSnapshotTests.cs` | Golden ViewModel I + E |
| Modificar | `CFDI.BuildPdf.Tests/Golden/PdfSmokeTests.cs` | Smoke factura |
| Modificar | `CFDI.BuildPdf.Tests/ComplementDispatchTests.cs` | Despacho factura I/E y rechazo T/P |
| Modificar | `CHANGELOG.md`, `README.md`, `CFDI.BuildPdf/CFDI.BuildPdf.csproj` | Versión 3.1.0 |

---

## Task 1: Red de seguridad — golden de texto del PDF de Carta Porte

Antes de extraer secciones de render, fijar el texto extraído del PDF de Carta Porte como baseline para detectar cualquier cambio.

**Files:**
- Create: `CFDI.BuildPdf.Tests/Golden/PdfTextRegressionTests.cs`
- (genera) `CFDI.BuildPdf.Tests/Golden/Snapshots/CartaPorte.pdftext.txt`, `CartaPorteRetenciones.pdftext.txt`

- [ ] **Step 1: Escribir el test**

```csharp
using System.Linq;
using System.Threading.Tasks;
using CFDI.BuildPdf;
using CFDI.BuildPdf.Tests.Helpers;
using UglyToad.PdfPig;
using Xunit;

namespace CFDI.BuildPdf.Tests.Golden
{
    /// <summary>
    /// Fija el texto extraído del PDF de Carta Porte como baseline. Sirve de red de
    /// seguridad: al extraer las secciones de render compartidas, el texto debe quedar idéntico.
    /// </summary>
    public class PdfTextRegressionTests
    {
        private static async Task<string> ExtraerTexto(System.Xml.Linq.XDocument xdoc)
        {
            var pdfBytes = await CfdiPdf.DesdeXmlStringAsync(xdoc.ToString());
            using var pdf = PdfDocument.Open(pdfBytes);
            return string.Join("\n", pdf.GetPages().Select(p => p.Text));
        }

        [Fact]
        [Trait("Category", "Golden")]
        public async Task CartaPorte_TextoPdf_CoincideConBaseline()
        {
            var texto = await ExtraerTexto(TestXmlLoader.LoadCartaPorte());
            Snapshot.Match(texto, "CartaPorte.pdftext.txt");
        }

        [Fact]
        [Trait("Category", "Golden")]
        public async Task CartaPorteRetenciones_TextoPdf_CoincideConBaseline()
        {
            var texto = await ExtraerTexto(TestXmlLoader.LoadCartaPorteRetenciones());
            Snapshot.Match(texto, "CartaPorteRetenciones.pdftext.txt");
        }
    }
}
```

- [ ] **Step 2: Ejecutar para crear baselines (primera ejecución falla)**

Run: `dotnet test CFDI.BuildPdf.sln -c Release --filter "FullyQualifiedName~PdfTextRegressionTests"`
Expected: FAIL — "Snapshot baseline creado: …CartaPorte.pdftext.txt" y "…CartaPorteRetenciones.pdftext.txt".

- [ ] **Step 3: Revisar los baselines creados**

Abrir `CFDI.BuildPdf.Tests/Golden/Snapshots/CartaPorte.pdftext.txt` y confirmar que contiene texto de un PDF de Carta Porte (emisor, conceptos, ubicaciones, etc.). Es el estado "antes" de la extracción.

- [ ] **Step 4: Re-ejecutar para verificar verde**

Run: `dotnet test CFDI.BuildPdf.sln -c Release --filter "FullyQualifiedName~PdfTextRegressionTests"`
Expected: PASS (2/2).

- [ ] **Step 5: Commit**

```bash
git add CFDI.BuildPdf.Tests/Golden/PdfTextRegressionTests.cs CFDI.BuildPdf.Tests/Golden/Snapshots/CartaPorte.pdftext.txt CFDI.BuildPdf.Tests/Golden/Snapshots/CartaPorteRetenciones.pdftext.txt
git commit -m "test: baseline de texto PDF de Carta Porte (red de seguridad pre-extracción)"
```

---

## Task 2: Despacho por predicado `CanHandle(XDocument)`

Evoluciona el contrato del handler sin cambiar el comportamiento de Carta Porte/Nómina.

**Files:**
- Modify: `CFDI.BuildPdf/Complements/ICfdiComplementHandler.cs`
- Create: `CFDI.BuildPdf/Complements/IComplementNamespacesProvider.cs`
- Create: `CFDI.BuildPdf/Complements/CfdiHandlerBase.cs`
- Modify: `CFDI.BuildPdf/Complements/ComplementHandlerBase.cs`
- Modify: `CFDI.BuildPdf/Services/CfdiPdfGenerator.cs`

- [ ] **Step 1: Reemplazar `ICfdiComplementHandler` por el contrato `CanHandle`**

Contenido completo de `ICfdiComplementHandler.cs`:

```csharp
using System.Xml.Linq;
using CFDI.BuildPdf.Abstractions;

namespace CFDI.BuildPdf.Complements
{
    /// <summary>
    /// Maneja la generación de PDF de un tipo de CFDI concreto.
    /// El orquestador elige el handler de mayor <see cref="Priority"/> cuyo
    /// <see cref="CanHandle"/> devuelva true para el documento.
    /// </summary>
    internal interface ICfdiComplementHandler
    {
        /// <summary>Indica si este handler puede procesar el CFDI dado.</summary>
        bool CanHandle(XDocument xdoc);

        /// <summary>Prioridad de desempate cuando varios handlers aplican. Mayor gana.</summary>
        int Priority { get; }

        /// <summary>Genera el PDF a partir del XML CFDI ya cargado.</summary>
        byte[] Generate(XDocument xdoc, CfdiPdfOptions options);
    }
}
```

- [ ] **Step 2: Crear `IComplementNamespacesProvider`** (para el guard de unicidad)

```csharp
using System.Collections.Generic;

namespace CFDI.BuildPdf.Complements
{
    /// <summary>
    /// Implementado por los handlers que casan por namespace de complemento.
    /// El orquestador lo usa para validar que dos handlers no declaren el mismo namespace.
    /// </summary>
    internal interface IComplementNamespacesProvider
    {
        IReadOnlyCollection<string> ComplementNamespaces { get; }
    }
}
```

- [ ] **Step 3: Crear `CfdiHandlerBase<TModel>`** (extrae el flujo `Generate`)

```csharp
using System;
using System.Xml.Linq;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Models;

namespace CFDI.BuildPdf.Complements
{
    /// <summary>
    /// Base de handlers: coordina mapper→builder y aplica las opciones comunes (logo).
    /// Las subclases definen únicamente <see cref="CanHandle"/> (y, si aplica, <see cref="Priority"/>).
    /// </summary>
    internal abstract class CfdiHandlerBase<TModel> : ICfdiComplementHandler
        where TModel : CfdiViewModelBase
    {
        private readonly ICfdiModelMapper<TModel> _mapper;
        private readonly IPdfDocumentBuilder<TModel> _builder;

        protected CfdiHandlerBase(ICfdiModelMapper<TModel> mapper, IPdfDocumentBuilder<TModel> builder)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        public abstract bool CanHandle(XDocument xdoc);

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

- [ ] **Step 4: Reescribir `ComplementHandlerBase<TModel>`** para heredar de `CfdiHandlerBase` y casar por namespace

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Models;

namespace CFDI.BuildPdf.Complements
{
    /// <summary>
    /// Base para handlers de complemento que casan por namespace (Carta Porte, Nómina).
    /// </summary>
    internal abstract class ComplementHandlerBase<TModel> : CfdiHandlerBase<TModel>, IComplementNamespacesProvider
        where TModel : CfdiViewModelBase
    {
        protected ComplementHandlerBase(ICfdiModelMapper<TModel> mapper, IPdfDocumentBuilder<TModel> builder)
            : base(mapper, builder) { }

        /// <summary>Namespace(s) de complemento que este handler reconoce (incluye la versión).</summary>
        public abstract IReadOnlyCollection<string> ComplementNamespaces { get; }

        public override bool CanHandle(XDocument xdoc)
        {
            var root = xdoc.Root;
            if (root is null) return false;
            var present = new HashSet<string>(root.Descendants().Select(e => e.Name.NamespaceName));
            return ComplementNamespaces.Any(present.Contains);
        }
    }
}
```

(Los archivos `CartaPorteComplementHandler.cs` y `NominaComplementHandler.cs` **no cambian**: siguen heredando de `ComplementHandlerBase` y declarando `ComplementNamespaces`.)

- [ ] **Step 5: Actualizar `CfdiPdfGenerator`** — despacho por `CanHandle`, guard por `IComplementNamespacesProvider`

En `CFDI.BuildPdf/Services/CfdiPdfGenerator.cs`:

Reemplazar el constructor (el guard de unicidad) por:

```csharp
        public CfdiPdfGenerator(IEnumerable<ICfdiComplementHandler> handlers)
        {
            if (handlers is null)
                throw new ArgumentNullException(nameof(handlers));

            _handlers = handlers.OrderByDescending(h => h.Priority).ToList();

            // Unicidad: dos handlers no pueden declarar el mismo namespace de complemento.
            var seen = new HashSet<string>();
            foreach (var provider in _handlers.OfType<IComplementNamespacesProvider>())
                foreach (var ns in provider.ComplementNamespaces)
                    if (!seen.Add(ns))
                        throw new InvalidOperationException(
                            $"Más de un handler declara el namespace de complemento '{ns}'.");
        }
```

Reemplazar `ResolveHandler` por:

```csharp
        /// <summary>
        /// Devuelve el handler de mayor prioridad que declara poder procesar el documento.
        /// </summary>
        private ICfdiComplementHandler? ResolveHandler(XDocument xdoc)
        {
            // _handlers ya está ordenado por Priority descendente.
            return _handlers.FirstOrDefault(h => h.CanHandle(xdoc));
        }
```

Añadir `using CFDI.BuildPdf.Complements;` si no está (ya está). El mensaje de excepción se actualiza en Task 9.

- [ ] **Step 6: Compilar y correr toda la suite**

Run: `dotnet build CFDI.BuildPdf.sln -c Release` → 0 errores.
Run: `dotnet test CFDI.BuildPdf.sln -c Release`
Expected: PASS (los 71 existentes + los 2 de Task 1 = 73). Carta Porte/Nómina sin cambios de comportamiento; `ComplementDispatchTests` siguen compilando (usan `ComplementNamespaces` del tipo concreto, que persiste).

- [ ] **Step 7: Commit**

```bash
git add CFDI.BuildPdf/Complements/ICfdiComplementHandler.cs CFDI.BuildPdf/Complements/IComplementNamespacesProvider.cs CFDI.BuildPdf/Complements/CfdiHandlerBase.cs CFDI.BuildPdf/Complements/ComplementHandlerBase.cs CFDI.BuildPdf/Services/CfdiPdfGenerator.cs
git commit -m "refactor(dispatch): despacho de handlers por predicado CanHandle"
```

---

## Task 3: Mover modelos genéricos de concepto a su propio archivo

**Files:**
- Create: `CFDI.BuildPdf/Models/ConceptoViewModel.cs`
- Modify: `CFDI.BuildPdf/Models/CfdiCartaPorteViewModel.cs`

- [ ] **Step 1: Crear `Models/ConceptoViewModel.cs`** con los 3 modelos genéricos (movidos verbatim desde `CfdiCartaPorteViewModel.cs` líneas 51-81)

```csharp
using System.Collections.Generic;

namespace CFDI.BuildPdf.Models
{
    internal class ConceptoViewModel
    {
        public string ClaveProductoServicio { get; set; }
        public string NumeroIdentificacion { get; set; }
        public decimal Cantidad { get; set; }
        public string ClaveUnidad { get; set; }
        public string Unidad { get; set; }
        public string Descripcion { get; set; }
        public decimal ValorUnitario { get; set; }
        public decimal Importe { get; set; }
        public decimal Descuento { get; set; }
        public string ObjetoImpuesto { get; set; }

        public List<ImpuestoConceptoViewModel> Traslados { get; set; } = new();
        public List<ImpuestoConceptoViewModel> Retenciones { get; set; } = new();
    }

    internal class ImpuestoConceptoViewModel
    {
        public string Impuesto { get; set; }       // IVA, ISR, etc.
        public string TipoFactor { get; set; }      // Tasa, Cuota, Exento
        public decimal TasaOCuota { get; set; }
        public decimal Base { get; set; }
        public decimal Importe { get; set; }
    }

    internal class RetencionImpuestoViewModel
    {
        public string Impuesto { get; set; }       // IVA, ISR, etc.
        public decimal Importe { get; set; }
    }
}
```

- [ ] **Step 2: Eliminar esas 3 clases de `CfdiCartaPorteViewModel.cs`**

Borrar de `CFDI.BuildPdf/Models/CfdiCartaPorteViewModel.cs` las clases `ConceptoViewModel`, `ImpuestoConceptoViewModel` y `RetencionImpuestoViewModel` (líneas 51-81). El resto del archivo (`CfdiCartaPorteViewModel`, `AddendaViewModel`, `CartaPorteViewModel`, etc.) queda igual. Conservar el `using System.Collections.Generic;` (lo usan otras clases del archivo).

- [ ] **Step 3: Compilar y correr golden**

Run: `dotnet build CFDI.BuildPdf.sln -c Release` → 0 errores.
Run: `dotnet test CFDI.BuildPdf.sln -c Release --filter "Category=Golden"`
Expected: PASS. Mover declaraciones de clase (mismo namespace, misma forma) **no** cambia el JSON serializado → golden de Carta Porte intactos.

- [ ] **Step 4: Commit**

```bash
git add CFDI.BuildPdf/Models/ConceptoViewModel.cs CFDI.BuildPdf/Models/CfdiCartaPorteViewModel.cs
git commit -m "refactor(models): extraer modelos de concepto/impuesto genéricos a su propio archivo"
```

---

## Task 4: Crear `CfdiFacturaViewModel`

**Files:**
- Create: `CFDI.BuildPdf/Models/CfdiFacturaViewModel.cs`

- [ ] **Step 1: Crear el ViewModel**

```csharp
using System.Collections.Generic;

namespace CFDI.BuildPdf.Models
{
    /// <summary>
    /// ViewModel para una factura CFDI 4.0 base (Ingreso/Egreso) sin complemento.
    /// Hereda las propiedades comunes de <see cref="CfdiViewModelBase"/>.
    /// </summary>
    internal class CfdiFacturaViewModel : CfdiViewModelBase
    {
        public string CondicionesPago { get; set; }

        public List<ConceptoViewModel> Conceptos { get; set; } = new();

        public decimal TotalImpuestosTrasladados { get; set; }
        public decimal TotalImpuestosRetenidos { get; set; }

        public List<ImpuestoConceptoViewModel> TrasladosResumen { get; set; } = new();
        public List<RetencionImpuestoViewModel> RetencionesResumen { get; set; } = new();
    }
}
```

- [ ] **Step 2: Compilar**

Run: `dotnet build CFDI.BuildPdf.sln -c Release` → 0 errores.

- [ ] **Step 3: Commit**

```bash
git add CFDI.BuildPdf/Models/CfdiFacturaViewModel.cs
git commit -m "feat(models): CfdiFacturaViewModel para factura base"
```

---

## Task 5: Extraer mapeo de conceptos/impuestos a `BaseCfdiMapper`

**Files:**
- Modify: `CFDI.BuildPdf/Mappers/Common/BaseCfdiMapper.cs`
- Modify: `CFDI.BuildPdf/Mappers/CartaPorte/CartaPorteMapper.cs`

- [ ] **Step 1: Añadir helpers `protected` a `BaseCfdiMapper<TModel>`**

Añadir dentro de la clase `BaseCfdiMapper<TModel>` (p.ej. antes de `#region Helpers compartidos`) estos métodos. Son el mapeo genérico movido desde `CartaPorteMapper` (conceptos + resumen de impuestos con fallback):

```csharp
        /// <summary>Mapea los Conceptos del comprobante con sus impuestos a nivel concepto.</summary>
        protected List<ConceptoViewModel> MapConceptos(XElement comprobante)
        {
            return comprobante
                .Element(Cfdi + "Conceptos")
                ?.Elements(Cfdi + "Concepto")
                .Select(c => new ConceptoViewModel
                {
                    ClaveProductoServicio = c.Attribute("ClaveProdServ")?.Value,
                    NumeroIdentificacion = c.Attribute("NoIdentificacion")?.Value,
                    Descripcion = c.Attribute("Descripcion")?.Value,
                    Cantidad = decimal.Parse(c.Attribute("Cantidad")?.Value ?? "0", CultureInfo.InvariantCulture),
                    ClaveUnidad = c.Attribute("ClaveUnidad")?.Value,
                    Unidad = c.Attribute("Unidad")?.Value,
                    ValorUnitario = decimal.Parse(c.Attribute("ValorUnitario")?.Value ?? "0", CultureInfo.InvariantCulture),
                    Importe = decimal.Parse(c.Attribute("Importe")?.Value ?? "0", CultureInfo.InvariantCulture),
                    Descuento = decimal.Parse(c.Attribute("Descuento")?.Value ?? "0", CultureInfo.InvariantCulture),
                    ObjetoImpuesto = c.Attribute("ObjetoImp")?.Value,
                    Traslados = c.Element(Cfdi + "Impuestos")?.Element(Cfdi + "Traslados")?.Elements(Cfdi + "Traslado")
                        .Select(t => new ImpuestoConceptoViewModel
                        {
                            Impuesto = t.Attribute("Impuesto")?.Value,
                            TipoFactor = t.Attribute("TipoFactor")?.Value,
                            TasaOCuota = decimal.Parse(t.Attribute("TasaOCuota")?.Value ?? "0", CultureInfo.InvariantCulture),
                            Base = decimal.Parse(t.Attribute("Base")?.Value ?? "0", CultureInfo.InvariantCulture),
                            Importe = decimal.Parse(t.Attribute("Importe")?.Value ?? "0", CultureInfo.InvariantCulture)
                        }).ToList() ?? new List<ImpuestoConceptoViewModel>(),
                    Retenciones = c.Element(Cfdi + "Impuestos")?.Element(Cfdi + "Retenciones")?.Elements(Cfdi + "Retencion")
                        .Select(r => new ImpuestoConceptoViewModel
                        {
                            Impuesto = r.Attribute("Impuesto")?.Value,
                            TipoFactor = r.Attribute("TipoFactor")?.Value,
                            TasaOCuota = decimal.Parse(r.Attribute("TasaOCuota")?.Value ?? "0", CultureInfo.InvariantCulture),
                            Base = decimal.Parse(r.Attribute("Base")?.Value ?? "0", CultureInfo.InvariantCulture),
                            Importe = decimal.Parse(r.Attribute("Importe")?.Value ?? "0", CultureInfo.InvariantCulture)
                        }).ToList() ?? new List<ImpuestoConceptoViewModel>()
                }).ToList() ?? new List<ConceptoViewModel>();
        }

        /// <summary>
        /// Mapea el resumen de impuestos a nivel comprobante (totales + desglose agrupado),
        /// con fallback que agrega desde los conceptos cuando no hay nodo global.
        /// </summary>
        protected void MapResumenImpuestos(
            XElement comprobante,
            List<ConceptoViewModel> conceptos,
            out decimal totalTrasladados,
            out decimal totalRetenidos,
            out List<ImpuestoConceptoViewModel> trasladosResumen,
            out List<RetencionImpuestoViewModel> retencionesResumen)
        {
            var impuestosNode = comprobante.Element(Cfdi + "Impuestos");
            totalTrasladados = ParseDecimalAttr(impuestosNode?.Attribute("TotalImpuestosTrasladados"));
            totalRetenidos = ParseDecimalAttr(impuestosNode?.Attribute("TotalImpuestosRetenidos"));

            trasladosResumen = impuestosNode?.Element(Cfdi + "Traslados")?.Elements(Cfdi + "Traslado")
                .Select(t => new ImpuestoConceptoViewModel
                {
                    Impuesto = t.Attribute("Impuesto")?.Value,
                    TipoFactor = t.Attribute("TipoFactor")?.Value,
                    TasaOCuota = ParseDecimalAttr(t.Attribute("TasaOCuota")),
                    Base = ParseDecimalAttr(t.Attribute("Base")),
                    Importe = ParseDecimalAttr(t.Attribute("Importe"))
                }).ToList() ?? new List<ImpuestoConceptoViewModel>();

            retencionesResumen = impuestosNode?.Element(Cfdi + "Retenciones")?.Elements(Cfdi + "Retencion")
                .Select(r => new RetencionImpuestoViewModel
                {
                    Impuesto = r.Attribute("Impuesto")?.Value,
                    Importe = ParseDecimalAttr(r.Attribute("Importe"))
                }).ToList() ?? new List<RetencionImpuestoViewModel>();

            if (trasladosResumen.Count == 0)
            {
                trasladosResumen = conceptos
                    .SelectMany(c => c.Traslados)
                    .GroupBy(t => new { t.Impuesto, t.TipoFactor, t.TasaOCuota })
                    .Select(g => new ImpuestoConceptoViewModel
                    {
                        Impuesto = g.Key.Impuesto,
                        TipoFactor = g.Key.TipoFactor,
                        TasaOCuota = g.Key.TasaOCuota,
                        Base = g.Sum(x => x.Base),
                        Importe = g.Sum(x => x.Importe)
                    }).ToList();

                if (totalTrasladados == 0)
                    totalTrasladados = trasladosResumen.Sum(t => t.Importe);
            }

            if (retencionesResumen.Count == 0)
            {
                retencionesResumen = conceptos
                    .SelectMany(c => c.Retenciones)
                    .GroupBy(r => r.Impuesto)
                    .Select(g => new RetencionImpuestoViewModel
                    {
                        Impuesto = g.Key,
                        Importe = g.Sum(x => x.Importe)
                    }).ToList();

                if (totalRetenidos == 0)
                    totalRetenidos = retencionesResumen.Sum(r => r.Importe);
            }
        }

        /// <summary>Parsea un atributo decimal con InvariantCulture; 0 si ausente/ inválido.</summary>
        protected static decimal ParseDecimalAttr(XAttribute? attribute)
        {
            if (attribute == null || string.IsNullOrWhiteSpace(attribute.Value))
                return 0m;
            return decimal.TryParse(attribute.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0m;
        }
```

Asegurar `using System.Collections.Generic;` y `using System.Linq;` en `BaseCfdiMapper.cs` (ya están).

- [ ] **Step 2: Refactorizar `CartaPorteMapper.MapComplemento`** para usar los helpers

En `CFDI.BuildPdf/Mappers/CartaPorte/CartaPorteMapper.cs`, reemplazar el bloque de mapeo de Conceptos + Impuestos globales + fallbacks (líneas 35-134 del archivo actual) por:

```csharp
            // Conceptos con impuestos a nivel concepto (mapeo compartido)
            model.Conceptos = MapConceptos(comprobante);

            // Resumen de impuestos a nivel comprobante (mapeo compartido, con fallback)
            MapResumenImpuestos(comprobante, model.Conceptos,
                out var totalTras, out var totalRet,
                out var trasResumen, out var retResumen);
            model.TotalImpuestosTrasladados = totalTras;
            model.TotalImpuestosRetenidos = totalRet;
            model.TrasladosResumen = trasResumen;
            model.RetencionesResumen = retResumen;
```

Eliminar de `CartaPorteMapper.cs` el método privado `ParseDecimalAttr` (líneas 273-280) que quedó duplicado (ahora es `protected` en la base). Mantener `CondicionesPago`, `Addenda` y el mapeo de Carta Porte tal cual.

- [ ] **Step 3: Compilar y correr golden (regresión de mapeo)**

Run: `dotnet build CFDI.BuildPdf.sln -c Release` → 0 errores.
Run: `dotnet test CFDI.BuildPdf.sln -c Release --filter "Category=Golden"`
Expected: PASS. Los golden de ViewModel de Carta Porte (`CartaPorte.viewmodel.json`, `CartaPorteRetenciones.viewmodel.json`) **no cambian** → el mapeo extraído es idéntico.

- [ ] **Step 4: Commit**

```bash
git add CFDI.BuildPdf/Mappers/Common/BaseCfdiMapper.cs CFDI.BuildPdf/Mappers/CartaPorte/CartaPorteMapper.cs
git commit -m "refactor(mappers): extraer mapeo de conceptos/impuestos a BaseCfdiMapper"
```

---

## Task 6: `FacturaMapper` + fixtures + golden ViewModel (I y E)

**Files:**
- Create: `CFDI.BuildPdf.Tests/TestData/cfdi_factura_ingreso.xml`
- Create: `CFDI.BuildPdf.Tests/TestData/cfdi_factura_egreso.xml`
- Modify: `CFDI.BuildPdf.Tests/Helpers/TestXmlLoader.cs`
- Create: `CFDI.BuildPdf/Mappers/Factura/FacturaMapper.cs`
- Modify: `CFDI.BuildPdf.Tests/Golden/ViewModelSnapshotTests.cs`
- (genera) `Snapshots/Factura.viewmodel.json`, `FacturaEgreso.viewmodel.json`

- [ ] **Step 1: Crear fixture Ingreso** `CFDI.BuildPdf.Tests/TestData/cfdi_factura_ingreso.xml`

(Datos sintéticos; aritmética: SubTotal 1000+500=1500; Trasladados 160+80=240; Retenidos 50; Total 1500+240-50=1690.)

```xml
<?xml version="1.0" encoding="UTF-8"?>
<cfdi:Comprobante xmlns:cfdi="http://www.sat.gob.mx/cfd/4" Version="4.0" Serie="A" Folio="1001" Fecha="2026-01-15T10:30:00" FormaPago="03" MetodoPago="PUE" Moneda="MXN" TipoDeComprobante="I" Exportacion="01" LugarExpedicion="64000" CondicionesDePago="CONTADO" SubTotal="1500.00" Total="1690.00" Sello="SELLO_EMISOR_DEMO" NoCertificado="00001000000500000001" Certificado="">
  <cfdi:Emisor Rfc="AAA010101AAA" Nombre="EMISOR DEMO SA DE CV" RegimenFiscal="601"/>
  <cfdi:Receptor Rfc="XAXX010101000" Nombre="CLIENTE DEMO" DomicilioFiscalReceptor="64000" RegimenFiscalReceptor="601" UsoCFDI="G03"/>
  <cfdi:Conceptos>
    <cfdi:Concepto ClaveProdServ="01010101" NoIdentificacion="P001" Cantidad="1" ClaveUnidad="H87" Unidad="Pieza" Descripcion="Producto A" ValorUnitario="1000.00" Importe="1000.00" Descuento="0.00" ObjetoImp="02">
      <cfdi:Impuestos>
        <cfdi:Traslados>
          <cfdi:Traslado Base="1000.00" Impuesto="002" TipoFactor="Tasa" TasaOCuota="0.160000" Importe="160.00"/>
        </cfdi:Traslados>
      </cfdi:Impuestos>
    </cfdi:Concepto>
    <cfdi:Concepto ClaveProdServ="01010101" NoIdentificacion="P002" Cantidad="2" ClaveUnidad="H87" Unidad="Pieza" Descripcion="Producto B" ValorUnitario="250.00" Importe="500.00" Descuento="0.00" ObjetoImp="02">
      <cfdi:Impuestos>
        <cfdi:Traslados>
          <cfdi:Traslado Base="500.00" Impuesto="002" TipoFactor="Tasa" TasaOCuota="0.160000" Importe="80.00"/>
        </cfdi:Traslados>
        <cfdi:Retenciones>
          <cfdi:Retencion Base="500.00" Impuesto="001" TipoFactor="Tasa" TasaOCuota="0.100000" Importe="50.00"/>
        </cfdi:Retenciones>
      </cfdi:Impuestos>
    </cfdi:Concepto>
  </cfdi:Conceptos>
  <cfdi:Impuestos TotalImpuestosRetenidos="50.00" TotalImpuestosTrasladados="240.00">
    <cfdi:Retenciones>
      <cfdi:Retencion Impuesto="001" Importe="50.00"/>
    </cfdi:Retenciones>
    <cfdi:Traslados>
      <cfdi:Traslado Base="1500.00" Impuesto="002" TipoFactor="Tasa" TasaOCuota="0.160000" Importe="240.00"/>
    </cfdi:Traslados>
  </cfdi:Impuestos>
  <cfdi:Complemento>
    <tfd:TimbreFiscalDigital xmlns:tfd="http://www.sat.gob.mx/TimbreFiscalDigital" Version="1.1" UUID="11111111-1111-1111-1111-111111111111" FechaTimbrado="2026-01-15T10:31:00" RfcProvCertif="PPD101129EA3" SelloCFD="SELLO_EMISOR_DEMO" NoCertificadoSAT="00001000000500000002" SelloSAT="SELLO_SAT_DEMO"/>
  </cfdi:Complemento>
</cfdi:Comprobante>
```

- [ ] **Step 2: Crear fixture Egreso** `CFDI.BuildPdf.Tests/TestData/cfdi_factura_egreso.xml`

(Nota de crédito; aritmética: SubTotal 800; Trasladados 128; Total 928.)

```xml
<?xml version="1.0" encoding="UTF-8"?>
<cfdi:Comprobante xmlns:cfdi="http://www.sat.gob.mx/cfd/4" Version="4.0" Serie="NC" Folio="55" Fecha="2026-02-20T09:00:00" FormaPago="01" MetodoPago="PUE" Moneda="MXN" TipoDeComprobante="E" Exportacion="01" LugarExpedicion="64000" CondicionesDePago="CONTADO" SubTotal="800.00" Total="928.00" Sello="SELLO_EMISOR_DEMO" NoCertificado="00001000000500000001" Certificado="">
  <cfdi:Emisor Rfc="AAA010101AAA" Nombre="EMISOR DEMO SA DE CV" RegimenFiscal="601"/>
  <cfdi:Receptor Rfc="XAXX010101000" Nombre="CLIENTE DEMO" DomicilioFiscalReceptor="64000" RegimenFiscalReceptor="601" UsoCFDI="G02"/>
  <cfdi:Conceptos>
    <cfdi:Concepto ClaveProdServ="84111506" NoIdentificacion="DEV01" Cantidad="1" ClaveUnidad="ACT" Unidad="Actividad" Descripcion="Devolucion parcial" ValorUnitario="800.00" Importe="800.00" Descuento="0.00" ObjetoImp="02">
      <cfdi:Impuestos>
        <cfdi:Traslados>
          <cfdi:Traslado Base="800.00" Impuesto="002" TipoFactor="Tasa" TasaOCuota="0.160000" Importe="128.00"/>
        </cfdi:Traslados>
      </cfdi:Impuestos>
    </cfdi:Concepto>
  </cfdi:Conceptos>
  <cfdi:Impuestos TotalImpuestosTrasladados="128.00">
    <cfdi:Traslados>
      <cfdi:Traslado Base="800.00" Impuesto="002" TipoFactor="Tasa" TasaOCuota="0.160000" Importe="128.00"/>
    </cfdi:Traslados>
  </cfdi:Impuestos>
  <cfdi:Complemento>
    <tfd:TimbreFiscalDigital xmlns:tfd="http://www.sat.gob.mx/TimbreFiscalDigital" Version="1.1" UUID="22222222-2222-2222-2222-222222222222" FechaTimbrado="2026-02-20T09:01:00" RfcProvCertif="PPD101129EA3" SelloCFD="SELLO_EMISOR_DEMO" NoCertificadoSAT="00001000000500000002" SelloSAT="SELLO_SAT_DEMO"/>
  </cfdi:Complemento>
</cfdi:Comprobante>
```

- [ ] **Step 3: Añadir loaders a `TestXmlLoader.cs`** (después de `LoadNominaIncapacidades`)

```csharp
        public static XDocument LoadFacturaIngreso()
            => Load("CFDI.BuildPdf.Tests.TestData.cfdi_factura_ingreso.xml");

        public static XDocument LoadFacturaEgreso()
            => Load("CFDI.BuildPdf.Tests.TestData.cfdi_factura_egreso.xml");
```

- [ ] **Step 4: Escribir los golden tests** en `ViewModelSnapshotTests.cs` (añadir; requiere `using CFDI.BuildPdf.Mappers.Factura;`)

```csharp
        [Fact]
        [Trait("Category", "Golden")]
        public void Factura_ViewModel_CoincideConBaseline()
        {
            var xdoc = TestXmlLoader.LoadFacturaIngreso();
            var mapper = new FacturaMapper(new FakeQrGenerator());

            var model = mapper.Map(xdoc);
            var json = JsonSerializer.Serialize(model, JsonOpts);

            Snapshot.Match(json, "Factura.viewmodel.json");
        }

        [Fact]
        [Trait("Category", "Golden")]
        public void FacturaEgreso_ViewModel_CoincideConBaseline()
        {
            var xdoc = TestXmlLoader.LoadFacturaEgreso();
            var mapper = new FacturaMapper(new FakeQrGenerator());

            var model = mapper.Map(xdoc);
            var json = JsonSerializer.Serialize(model, JsonOpts);

            Snapshot.Match(json, "FacturaEgreso.viewmodel.json");
        }
```

- [ ] **Step 5: Ejecutar — debe fallar a compilar (no existe `FacturaMapper`)**

Run: `dotnet build CFDI.BuildPdf.sln -c Release`
Expected: FAIL — `FacturaMapper` no existe.

- [ ] **Step 6: Crear `FacturaMapper`** `CFDI.BuildPdf/Mappers/Factura/FacturaMapper.cs`

```csharp
using System.Xml.Linq;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Mappers.Common;
using CFDI.BuildPdf.Models;
using Microsoft.Extensions.Logging;

namespace CFDI.BuildPdf.Mappers.Factura
{
    /// <summary>
    /// Mapper de factura CFDI 4.0 base (Ingreso/Egreso) sin complemento.
    /// Reutiliza el mapeo común de <see cref="BaseCfdiMapper{TModel}"/> y los helpers
    /// compartidos de conceptos/impuestos.
    /// </summary>
    internal class FacturaMapper : BaseCfdiMapper<CfdiFacturaViewModel>
    {
        public FacturaMapper(IQrGenerator qrGenerator, ILogger<FacturaMapper>? logger = null)
            : base(qrGenerator, logger) { }

        protected override CfdiFacturaViewModel CreateModel() => new();

        protected override void MapComplemento(XDocument xdoc, CfdiFacturaViewModel model)
        {
            var comprobante = xdoc.Root;

            model.CondicionesPago = comprobante?.Attribute("CondicionesDePago")?.Value;

            model.Conceptos = MapConceptos(comprobante);

            MapResumenImpuestos(comprobante, model.Conceptos,
                out var totalTras, out var totalRet,
                out var trasResumen, out var retResumen);
            model.TotalImpuestosTrasladados = totalTras;
            model.TotalImpuestosRetenidos = totalRet;
            model.TrasladosResumen = trasResumen;
            model.RetencionesResumen = retResumen;
        }
    }
}
```

- [ ] **Step 7: Ejecutar — crea baselines (falla la 1ª vez)**

Run: `dotnet test CFDI.BuildPdf.sln -c Release --filter "FullyQualifiedName~Factura"`
Expected: FAIL — crea `Factura.viewmodel.json` y `FacturaEgreso.viewmodel.json`.

- [ ] **Step 8: Revisar baselines**

Verificar en `Snapshots/Factura.viewmodel.json`: `TipoComprobante: "I"`, `SubTotal: 1500`, `Total: 1690`, `TotalImpuestosTrasladados: 240`, `TotalImpuestosRetenidos: 50`, 2 conceptos, `QRCodeBase64: "FAKE_QR_BASE64"`. En `FacturaEgreso.viewmodel.json`: `TipoComprobante: "E"`, `Total: 928`, `TotalImpuestosTrasladados: 128`.

- [ ] **Step 9: Re-ejecutar — verde**

Run: `dotnet test CFDI.BuildPdf.sln -c Release --filter "FullyQualifiedName~Factura"`
Expected: PASS (2/2).

- [ ] **Step 10: Commit**

```bash
git add CFDI.BuildPdf.Tests/TestData/cfdi_factura_ingreso.xml CFDI.BuildPdf.Tests/TestData/cfdi_factura_egreso.xml CFDI.BuildPdf.Tests/Helpers/TestXmlLoader.cs CFDI.BuildPdf/Mappers/Factura/FacturaMapper.cs CFDI.BuildPdf.Tests/Golden/ViewModelSnapshotTests.cs CFDI.BuildPdf.Tests/Golden/Snapshots/Factura.viewmodel.json CFDI.BuildPdf.Tests/Golden/Snapshots/FacturaEgreso.viewmodel.json
git commit -m "feat(mappers): FacturaMapper + golden ViewModel (Ingreso y Egreso)"
```

---

## Task 7: Extraer secciones de render a `ComprobanteSections` y reusar en Carta Porte

**Files:**
- Create: `CFDI.BuildPdf/PdfBuilders/Common/ComprobanteSections.cs`
- Modify: `CFDI.BuildPdf/PdfBuilders/CartaPorte/CartaPorteDocumentBuilder.cs`

- [ ] **Step 1: Crear `ComprobanteSections`** moviendo las secciones genéricas desde `CartaPorteDocumentBuilder`

Crear `CFDI.BuildPdf/PdfBuilders/Common/ComprobanteSections.cs` (namespace `CFDI.BuildPdf.PdfBuilders.Common`, `internal static class ComprobanteSections`). Mover **verbatim** desde `CartaPorteDocumentBuilder.cs` estos métodos, cambiando la firma para no depender de `CfdiCartaPorteViewModel`:

1. `LabelValueSpans(TextDescriptor t, string label, string? value)` — sin cambios (privado→`internal`).
2. `ComposeClienteYEmision` — firma nueva: `public static void ComposeClienteYEmision(ColumnDescriptor col, CfdiViewModelBase model)`. El cuerpo (líneas 100-172 del builder actual) usa solo campos de `CfdiViewModelBase` → sin cambios internos.
3. `ComposeFormaPago` — firma nueva: `public static void ComposeFormaPago(ColumnDescriptor col, CfdiViewModelBase model, string? condicionesPago)`. En el cuerpo (líneas 184-206), cambiar `model.CondicionesPago` por `condicionesPago`.
4. `ComposeConceptos` — firma nueva: `public static void ComposeConceptos(ColumnDescriptor col, System.Collections.Generic.IReadOnlyList<ConceptoViewModel> conceptos)`. En el cuerpo (líneas 208-330), cambiar `model.Conceptos` por `conceptos` (3 usos: guarda inicial, `useZebra`, `foreach`).
5. `ComposeTotales` — firma nueva: `public static void ComposeTotales(ColumnDescriptor col, CfdiViewModelBase model, System.Collections.Generic.IReadOnlyList<ImpuestoConceptoViewModel> trasladosResumen, System.Collections.Generic.IReadOnlyList<RetencionImpuestoViewModel> retencionesResumen, decimal totalTrasladados, decimal totalRetenidos)`. Mover también `ComposePanelTotales` y `TotalRow` (privados→`private static` dentro de `ComprobanteSections`), con `ComposePanelTotales(IContainer container, CfdiViewModelBase model, IReadOnlyList<ImpuestoConceptoViewModel> trasladosResumen, IReadOnlyList<RetencionImpuestoViewModel> retencionesResumen, decimal totalTrasladados, decimal totalRetenidos)`. En sus cuerpos (líneas 332-430), cambiar:
   - `model.TrasladosResumen` → `trasladosResumen`
   - `model.RetencionesResumen` → `retencionesResumen`
   - `model.TotalImpuestosTrasladados` → `totalTrasladados`
   - `model.TotalImpuestosRetenidos` → `totalRetenidos`
   - `model.SubTotal`, `model.Total`, `model.CantidadConLetra` quedan igual (son de `CfdiViewModelBase`).

Usings necesarios en el nuevo archivo: `System.Collections.Generic`, `System.Globalization`, `System.Linq`, `CFDI.BuildPdf.Catalogs`, `CFDI.BuildPdf.Models`, `QuestPDF.Fluent`, `QuestPDF.Infrastructure`. Las llamadas a formatters/headers siguen vía `CfdiPdfSections.*` (mismo namespace `Common`).

- [ ] **Step 2: Refactorizar `CartaPorteDocumentBuilder`** para usar `ComprobanteSections`

En `CartaPorteDocumentBuilder.cs`:
- Borrar los métodos privados ahora movidos: `ComposeClienteYEmision`, `LabelValueSpans`, `ComposeFormaPago`, `ComposeConceptos`, `ComposeTotales`, `ComposePanelTotales`, `TotalRow`.
- En el `page.Content().Column(col => { ... })` (líneas 47-67), reemplazar las 4 llamadas por las compartidas:

```csharp
                        col.Item().Element(c => CfdiPdfSections.ComposeEncabezado(c, model, _logger));
                        ComprobanteSections.ComposeClienteYEmision(col, model);
                        ComprobanteSections.ComposeFormaPago(col, model, model.CondicionesPago);
                        ComprobanteSections.ComposeConceptos(col, model.Conceptos);
                        ComprobanteSections.ComposeTotales(col, model, model.TrasladosResumen, model.RetencionesResumen, model.TotalImpuestosTrasladados, model.TotalImpuestosRetenidos);

                        if (options.MostrarAddenda)
                            ComposeAddenda(col, model);
                        // … resto de secciones Carta Porte sin cambios …
```

- Añadir `using CFDI.BuildPdf.PdfBuilders.Common;` (probablemente ya presente por `CfdiPdfSections`).

- [ ] **Step 3: Compilar y correr la red de seguridad (texto idéntico)**

Run: `dotnet build CFDI.BuildPdf.sln -c Release` → 0 errores.
Run: `dotnet test CFDI.BuildPdf.sln -c Release --filter "Category=Golden"`
Expected: PASS. En particular `PdfTextRegressionTests` (Task 1) debe seguir verde → el PDF de Carta Porte quedó **idéntico** tras la extracción.

- [ ] **Step 4: Commit**

```bash
git add CFDI.BuildPdf/PdfBuilders/Common/ComprobanteSections.cs CFDI.BuildPdf/PdfBuilders/CartaPorte/CartaPorteDocumentBuilder.cs
git commit -m "refactor(render): extraer secciones de comprobante a ComprobanteSections (reuso CP/factura)"
```

---

## Task 8: `FacturaDocumentBuilder` + smoke directo

**Files:**
- Create: `CFDI.BuildPdf/PdfBuilders/Factura/FacturaDocumentBuilder.cs`
- Modify: `CFDI.BuildPdf.Tests/Golden/PdfSmokeTests.cs`

- [ ] **Step 1: Escribir un smoke directo del builder** (sin pasar por el handler todavía) en `PdfSmokeTests.cs` (añadir; requiere `using CFDI.BuildPdf.Mappers.Factura; using CFDI.BuildPdf.PdfBuilders.Factura; using CFDI.BuildPdf.Abstractions;`)

```csharp
        [Fact]
        [Trait("Category", "Golden")]
        public void Factura_Builder_GeneraPdfValidoConContenido()
        {
            var xdoc = TestXmlLoader.LoadFacturaIngreso();
            var mapper = new FacturaMapper(new FakeQrGenerator());
            var model = mapper.Map(xdoc);

            var builder = new FacturaDocumentBuilder();
            var pdfBytes = builder.Build(model, new CfdiPdfOptions());

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

(Nota: la fachada `CfdiPdf` fija la licencia Community de QuestPDF; aquí se construye el builder directo, así que el smoke debe correr en un proceso donde la licencia ya esté fijada por otros tests. Para aislamiento, el builder no requiere licencia para `GeneratePdf` en Community si ya fue fijada; los tests corren sin paralelismo (`AssemblyConfig`). Si fallara por licencia, anteponer `QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;` al inicio del test.)

- [ ] **Step 2: Ejecutar — falla a compilar (no existe `FacturaDocumentBuilder`)**

Run: `dotnet build CFDI.BuildPdf.sln -c Release`
Expected: FAIL — `FacturaDocumentBuilder` no existe.

- [ ] **Step 3: Crear `FacturaDocumentBuilder`** `CFDI.BuildPdf/PdfBuilders/Factura/FacturaDocumentBuilder.cs`

```csharp
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Models;
using CFDI.BuildPdf.PdfBuilders.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CFDI.BuildPdf.PdfBuilders.Factura
{
    /// <summary>
    /// Construye el PDF de una factura CFDI 4.0 base (Ingreso/Egreso) sin complemento,
    /// reutilizando las secciones compartidas de comprobante.
    /// </summary>
    internal class FacturaDocumentBuilder : IPdfDocumentBuilder<CfdiFacturaViewModel>
    {
        private readonly ILogger<FacturaDocumentBuilder> _logger;

        public FacturaDocumentBuilder(ILogger<FacturaDocumentBuilder>? logger = null)
        {
            _logger = logger ?? NullLogger<FacturaDocumentBuilder>.Instance;
        }

        public byte[] Build(CfdiFacturaViewModel model, CfdiPdfOptions options)
        {
            var pageSize = options.Orientacion == PdfOrientation.Landscape
                ? PageSizes.Letter.Landscape()
                : PageSizes.Letter;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(pageSize);
                    page.MarginTop(0.7f, Unit.Centimetre);
                    page.MarginBottom(1.2f, Unit.Centimetre);
                    page.MarginHorizontal(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(PdfStyleConstants.FontSizeDefault).FontFamily(PdfStyleConstants.FontFamily));

                    page.Content().Column(col =>
                    {
                        col.Item().Element(c => CfdiPdfSections.ComposeEncabezado(c, model, _logger));
                        ComprobanteSections.ComposeClienteYEmision(col, model);
                        ComprobanteSections.ComposeFormaPago(col, model, model.CondicionesPago);
                        ComprobanteSections.ComposeConceptos(col, model.Conceptos);
                        ComprobanteSections.ComposeTotales(col, model, model.TrasladosResumen, model.RetencionesResumen, model.TotalImpuestosTrasladados, model.TotalImpuestosRetenidos);
                        col.Item().Element(c => CfdiPdfSections.ComposeFooterFiscal(c, model));
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(PdfStyleConstants.FontSizeSmall));
                        text.Span("ESTE DOCUMENTO ES UNA REPRESENTACIÓN IMPRESA DE UN CFDI");
                        text.Span("    Página ");
                        text.CurrentPageNumber();
                        text.Span(" de ");
                        text.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
```

- [ ] **Step 4: Compilar y correr el smoke**

Run: `dotnet build CFDI.BuildPdf.sln -c Release` → 0 errores.
Run: `dotnet test CFDI.BuildPdf.sln -c Release --filter "FullyQualifiedName~Factura_Builder"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add CFDI.BuildPdf/PdfBuilders/Factura/FacturaDocumentBuilder.cs CFDI.BuildPdf.Tests/Golden/PdfSmokeTests.cs
git commit -m "feat(render): FacturaDocumentBuilder + smoke"
```

---

## Task 9: Handler de factura + composición + despacho end-to-end + mensaje

**Files:**
- Create: `CFDI.BuildPdf/Complements/FacturaComplementHandler.cs`
- Modify: `CFDI.BuildPdf/Configuration/CfdiPdfFactory.cs`
- Modify: `CFDI.BuildPdf/Services/CfdiPdfGenerator.cs` (mensaje)
- Modify: `CFDI.BuildPdf.Tests/ComplementDispatchTests.cs`
- Modify: `CFDI.BuildPdf.Tests/Golden/PdfSmokeTests.cs`

- [ ] **Step 1: Escribir los tests de despacho end-to-end** en `ComplementDispatchTests.cs` (añadir)

```csharp
        [Theory]
        [InlineData("I")]
        [InlineData("E")]
        public async Task FacturaBaseIngresoEgreso_GeneraPdf(string tipo)
        {
            var xml =
                $"<cfdi:Comprobante xmlns:cfdi=\"http://www.sat.gob.mx/cfd/4\" Version=\"4.0\" " +
                $"TipoDeComprobante=\"{tipo}\" SubTotal=\"100\" Total=\"116\" Moneda=\"MXN\">" +
                "<cfdi:Emisor Rfc=\"AAA010101AAA\" Nombre=\"E\" RegimenFiscal=\"601\"/>" +
                "<cfdi:Receptor Rfc=\"XAXX010101000\" Nombre=\"R\" RegimenFiscalReceptor=\"601\" UsoCFDI=\"G03\"/>" +
                "<cfdi:Conceptos><cfdi:Concepto ClaveProdServ=\"01010101\" Cantidad=\"1\" ClaveUnidad=\"H87\" Descripcion=\"X\" ValorUnitario=\"100\" Importe=\"100\" ObjetoImp=\"02\"/></cfdi:Conceptos>" +
                "</cfdi:Comprobante>";

            var pdf = await CfdiPdf.DesdeXmlStringAsync(xml);
            Assert.True(pdf.Length > 1000);
            Assert.Equal((byte)'%', pdf[0]);
        }

        [Theory]
        [InlineData("T")]
        [InlineData("P")]
        public async Task TipoNoSoportadoSinComplemento_Lanza(string tipo)
        {
            var xml =
                $"<cfdi:Comprobante xmlns:cfdi=\"http://www.sat.gob.mx/cfd/4\" Version=\"4.0\" " +
                $"TipoDeComprobante=\"{tipo}\" SubTotal=\"0\" Total=\"0\"></cfdi:Comprobante>";

            await Assert.ThrowsAsync<CfdiComplementoNoSoportadoException>(
                () => CfdiPdf.DesdeXmlStringAsync(xml));
        }
```

- [ ] **Step 2: Ejecutar — fallan (no hay handler de factura registrado)**

Run: `dotnet test CFDI.BuildPdf.sln -c Release --filter "FullyQualifiedName~FacturaBaseIngresoEgreso"`
Expected: FAIL — lanza `CfdiComplementoNoSoportadoException` (aún no registrado el handler).

- [ ] **Step 3: Crear `FacturaComplementHandler`** `CFDI.BuildPdf/Complements/FacturaComplementHandler.cs`

```csharp
using System;
using System.Xml.Linq;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Models;

namespace CFDI.BuildPdf.Complements
{
    /// <summary>
    /// Handler de factura CFDI 4.0 base (sin complemento Carta Porte ni Nómina).
    /// Aplica a comprobantes de tipo Ingreso (I) y Egreso (E). Prioridad mínima:
    /// solo se elige cuando ningún handler de complemento aplica.
    /// </summary>
    internal sealed class FacturaComplementHandler : CfdiHandlerBase<CfdiFacturaViewModel>
    {
        public FacturaComplementHandler(
            ICfdiModelMapper<CfdiFacturaViewModel> mapper,
            IPdfDocumentBuilder<CfdiFacturaViewModel> builder)
            : base(mapper, builder) { }

        public override int Priority => int.MinValue;

        public override bool CanHandle(XDocument xdoc)
        {
            var tipo = xdoc.Root?.Attribute("TipoDeComprobante")?.Value;
            return string.Equals(tipo, "I", StringComparison.Ordinal)
                || string.Equals(tipo, "E", StringComparison.Ordinal);
        }
    }
}
```

- [ ] **Step 4: Registrar en `CfdiPdfFactory`**

En `CFDI.BuildPdf/Configuration/CfdiPdfFactory.cs`, añadir usings:
```csharp
using CFDI.BuildPdf.Mappers.Factura;
using CFDI.BuildPdf.PdfBuilders.Factura;
```
y agregar al arreglo `handlers` (al final, tras Nómina):
```csharp
                new FacturaComplementHandler(
                    new FacturaMapper(qrGenerator, loggerFactory?.CreateLogger<FacturaMapper>()),
                    new FacturaDocumentBuilder())
```

- [ ] **Step 5: Actualizar el mensaje de no-soportado** en `CfdiPdfGenerator.GenerarPdfInterno`

Reemplazar el mensaje de la excepción:
```csharp
            if (handler is null)
                throw new CfdiComplementoNoSoportadoException(
                    "Tipo de CFDI no soportado. La librería soporta factura base de Ingreso (I) y Egreso (E), Carta Porte 3.1 y Nómina 1.2.");
```

- [ ] **Step 6: Añadir smoke end-to-end de factura vía fachada** en `PdfSmokeTests.cs`

```csharp
        [Fact]
        [Trait("Category", "Golden")]
        public async Task Factura_GeneraPdfValidoConContenido()
        {
            var xml = TestXmlLoader.LoadFacturaIngreso().ToString();

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

- [ ] **Step 7: Compilar y correr toda la suite**

Run: `dotnet build CFDI.BuildPdf.sln -c Release` → 0 errores.
Run: `dotnet test CFDI.BuildPdf.sln -c Release`
Expected: PASS (todos: golden CP/Nómina/Factura, smoke, dispatch I/E y rechazo T/P, regresión texto CP).

- [ ] **Step 8: Commit**

```bash
git add CFDI.BuildPdf/Complements/FacturaComplementHandler.cs CFDI.BuildPdf/Configuration/CfdiPdfFactory.cs CFDI.BuildPdf/Services/CfdiPdfGenerator.cs CFDI.BuildPdf.Tests/ComplementDispatchTests.cs CFDI.BuildPdf.Tests/Golden/PdfSmokeTests.cs
git commit -m "feat: handler de factura base (Ingreso/Egreso) + despacho end-to-end"
```

---

## Task 10: Docs y versión 3.1.0

**Files:**
- Modify: `CFDI.BuildPdf/CFDI.BuildPdf.csproj`
- Modify: `CHANGELOG.md`
- Modify: `README.md`

- [ ] **Step 1: Bump de versión en `CFDI.BuildPdf.csproj`**

Cambiar `<Version>3.0.0</Version>` por `<Version>3.1.0</Version>` y actualizar `<PackageReleaseNotes>`:
```xml
		<PackageReleaseNotes>v3.1.0: soporte para factura base (Ingreso/Egreso) sin complemento. Despacho de handlers por predicado CanHandle. Sin breaking changes.</PackageReleaseNotes>
```

- [ ] **Step 2: Entrada en `CHANGELOG.md`** (arriba, bajo un encabezado `## [3.1.0]`)

```markdown
## [3.1.0]
### Agregado
- Generación de PDF para **facturas base CFDI 4.0** (TipoDeComprobante Ingreso `I` y Egreso `E`) sin complemento Carta Porte ni Nómina.

### Cambiado
- Despacho interno de handlers por predicado `CanHandle(XDocument)` (sin impacto en la API pública).
- Secciones de render y mapeo de conceptos/impuestos extraídas a componentes compartidos (reuso entre factura y Carta Porte).
```

- [ ] **Step 3: Nota en `README.md`** — actualizar la lista de tipos soportados para incluir "Factura base (Ingreso/Egreso) sin complemento" junto a Carta Porte 3.1 y Nómina 1.2.

- [ ] **Step 4: Compilar y correr toda la suite**

Run: `dotnet build CFDI.BuildPdf.sln -c Release` → 0 errores.
Run: `dotnet test CFDI.BuildPdf.sln -c Release` → PASS.

- [ ] **Step 5: Commit**

```bash
git add CFDI.BuildPdf/CFDI.BuildPdf.csproj CHANGELOG.md README.md
git commit -m "docs: factura base + bump 3.1.0"
```

---

## Task 11: Validación manual en ConsoleDemo (gate previo a publicar)

No publicar a NuGet hasta que el usuario revise los PDFs (mismo gate que en v3).

**Files:** (sin cambios de librería)

- [ ] **Step 1: Generar PDFs de los 3 XML de `Directas`**

Adaptar/usar `CFDI.BuildPdf.ConsoleDemo` para generar el PDF de cada XML en `CFDI.BuildPdf.ConsoleDemo/test/Directas/` (FOAP-7968, FOAP-8008, FOAP-8021) a una carpeta de salida temporal.
Run: `dotnet run --project CFDI.BuildPdf.ConsoleDemo -c Release` (con la lógica apuntando a `test/Directas`).
Expected: 3 PDFs generados sin excepción.

- [ ] **Step 2: Revisión del usuario**

El usuario abre los 3 PDFs y confirma que el layout (encabezado, cliente, forma de pago, conceptos con impuestos, totales, footer fiscal) es correcto. Si hay ajustes de layout, se vuelven tareas nuevas.

- [ ] **Step 3:** (Sin commit; los XML de `Directas` están gitignored.)

---

## Self-review (planner)

**1. Cobertura del spec:**
- Despacho `CanHandle` → Task 2. ✓
- Alcance I/E + rechazo T/P → Task 9 (handler + tests). ✓
- Modelos genéricos compartidos → Task 3; `CfdiFacturaViewModel` → Task 4. ✓
- Extracción de mapeo → Task 5; `FacturaMapper` → Task 6. ✓
- Extracción de render + reuso CP → Task 7; `FacturaDocumentBuilder` → Task 8. ✓
- Composition root → Task 9. ✓
- Mensaje de error → Task 9. ✓
- Pruebas: golden I/E (Task 6), smoke (Task 8/9), despacho (Task 9), regresión PDF CP (Task 1/7). ✓
- Superficie pública sin cambios; 3.1.0 → Task 10. ✓
- Validación ConsoleDemo → Task 11. ✓
- Decisión: base intermedia NO se introduce (documentado arriba). ✓

**2. Placeholders:** ninguno; todo paso con código tiene código. Las secciones movidas verbatim referencian rangos de líneas exactos del archivo fuente existente + las transformaciones de firma puntuales.

**3. Consistencia de tipos:** `CfdiFacturaViewModel` (props), `FacturaMapper`, `FacturaDocumentBuilder`, `FacturaComplementHandler`, `ComprobanteSections.{ComposeClienteYEmision, ComposeFormaPago, ComposeConceptos, ComposeTotales}`, `MapConceptos`/`MapResumenImpuestos`/`ParseDecimalAttr`, `IComplementNamespacesProvider`, `CfdiHandlerBase` — nombres consistentes entre tareas.
