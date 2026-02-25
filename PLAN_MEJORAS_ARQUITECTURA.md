# Plan de Mejoras — Arquitectura, SOLID y Robustez

## Estado actual

La librería funciona como un pipeline lineal: **XML → Mapper → ViewModel → Razor HTML → wkhtmltopdf → byte[] PDF**.  
Toda la lógica vive en clases estáticas o concretas sin abstracciones, lo que genera alto acoplamiento, baja testabilidad y dificulta la extensión a nuevos complementos CFDI.

---

## 1. Violaciones SOLID identificadas

### 1.1 Single Responsibility (SRP)

| Clase | Problema |
|---|---|
| `XmlToModelMapper` (612 líneas) | Concentra el mapeo de Carta Porte, Nómina, Addenda, QR URL y helpers de parseo en una sola clase estática. Cada complemento nuevo agranda esta clase sin límite. |
| `PdfService` | Orquesta el pipeline completo: carga la DLL nativa, inicializa Razor, inicializa DinkToPdf, mapea XML, renderiza HTML y convierte a PDF. Son al menos 4 responsabilidades. |
| `NumberToWordsConverter` | Ubicado en `Mappers/` cuando es un helper de formato; confunde la responsabilidad del namespace. |

**Acción**: Extraer cada responsabilidad a su propia clase/interfaz.

### 1.2 Open/Closed (OCP)

| Problema | Detalle |
|---|---|
| Agregar un complemento nuevo (ej. Pagos 2.0) requiere modificar `XmlToModelMapper`, `PdfService`, `CfdiPdf` y crear templates. | No existe un mecanismo de extensión; se debe tocar código existente en múltiples archivos. |
| `MapAddenda` tiene lógica hardcodeada para `buzone.com.mx`. | Cada nuevo proveedor de addenda requiere modificar el método. |

**Acción**: Implementar un patrón Strategy/Registry para complementos y addendas.

### 1.3 Liskov Substitution (LSP)

| Problema | Detalle |
|---|---|
| No hay abstracciones (interfaces/clases base) que permitan sustituir implementaciones. | `PdfService` es una clase concreta instanciada directamente. No se puede reemplazar el motor de PDF, el motor de templates ni el generador de QR. |

**Acción**: Definir interfaces para cada servicio sustituible.

### 1.4 Interface Segregation (ISP)

| Problema | Detalle |
|---|---|
| `CfdiPdf` expone 6 métodos estáticos que mezclan dos dominios (Carta Porte y Nómina) en una sola superficie. | Un consumidor que solo necesita Nómina recibe también la API de Carta Porte. |

**Acción**: Segregar contratos por dominio funcional.

### 1.5 Dependency Inversion (DIP)

| Problema | Detalle |
|---|---|
| `PdfService` depende directamente de `RazorLightEngineBuilder`, `SynchronizedConverter`, `NativeLibraryLoader`, `XmlToModelMapper` y `QrGeneratorService` — todas implementaciones concretas. | Imposible hacer unit testing, mocking o reemplazar componentes. |
| `CfdiPdf` instancia `PdfService` con `new` como singleton estático. | Acoplamiento total; no compatible con DI containers. |

**Acción**: Invertir dependencias con interfaces e inyección de dependencias.

---

## 2. Problemas de robustez

| ID | Ubicación | Problema | Severidad |
|---|---|---|---|
| R-01 | `XmlToModelMapper.Map()` L117 | `mercanciasNode` se accede **antes** de validar `cartaPorte != null`, causando `NullReferenceException` si el XML no tiene Carta Porte. | **Alta** |
| R-02 | `XmlToModelMapper.Map()` L36,37 | `DateTime.Parse()` sin `CultureInfo.InvariantCulture` — falla en culturas con formato de fecha diferente. | **Alta** |
| R-03 | `XmlToModelMapper.Map()` L67-84 | `decimal.Parse()` sin `CultureInfo.InvariantCulture` en todo el mapper de Carta Porte (sí lo usa en Nómina). Inconsistente. | **Alta** |
| R-04 | `PdfService.ConvertHtmlToPdf()` | El archivo temporal HTML no se elimina si `_pdfConverter.Convert()` lanza excepción (no hay `try/finally`). | **Media** |
| R-05 | `NativeLibraryLoader` | `Console.WriteLine` hardcodeado — inapropiado para una librería; contamina stdout del consumidor. | **Media** |
| R-06 | `XmlToModelMapper.MapAddenda()` L263 | `catch` vacío — traga la excepción silenciosamente sin log ni contexto. | **Media** |
| R-07 | `NumberToWordsConverter` | Solo soporta hasta millones (~999,999,999). Cantidades mayores producen texto incorrecto. | **Baja** |
| R-08 | `CfdiPdf` | La instancia estática de `PdfService` no es thread-safe para inicialización concurrente del engine Razor. | **Media** |

---

## 3. Problemas de mantenibilidad

| ID | Problema | Detalle |
|---|---|---|
| M-01 | `XmlNamespaceHelper` no se usa | Código muerto. Los namespaces están hardcodeados en `XmlToModelMapper`. |
| M-02 | Clases marker vacías en `Templates/` | `TemplateFacturaCartaPorte.cs` y `TemplateFacturaNomina.cs` son clases vacías usadas solo como anchor de assembly para `RazorLight`. Patrón frágil. |
| M-03 | Código comentado en `PdfService` (L35-59) | Método anterior comentado que agrega ruido. |
| M-04 | Múltiples ViewModels en un solo archivo | `CfdiCartaPorteViewModel.cs` contiene 11 clases. Dificulta navegación y claridad. |
| M-05 | Estilos CSS duplicados | Cada template `.cshtml` tiene sus propios estilos inline/embebidos sin reutilización. |
| M-06 | Namespace inconsistente en API pública | La fachada `CfdiPdf` vive en `CFDI.BuildPdf.Service` pero el consumidor importa `CFDI.BuildPdf` según el README. Funciona por el `using` pero la convención es confusa. |

---

## 4. Problemas de escalabilidad (para nuevos complementos)

| ID | Problema | Impacto |
|---|---|---|
| E-01 | Sin patrón de registro de complementos | Cada complemento nuevo requiere: nuevo ViewModel, nuevo mapper, nuevo template, nuevas sobrecargas en `PdfService` y `CfdiPdf`. Son 5+ archivos a tocar. |
| E-02 | Sin detección automática de tipo de CFDI | El consumidor debe saber de antemano si el XML es Carta Porte o Nómina y llamar al método correcto. |
| E-03 | `libwkhtmltox.dll` embebida (30 MB) solo Windows | Bloquea uso en Linux/Docker/macOS. Agregar soporte cross-platform con la arquitectura actual es invasivo. |

---

## 5. Plan de refactorización

### Fase 1 — Correcciones críticas de robustez (sin cambiar arquitectura)

> Objetivo: Estabilizar el código existente antes de refactorizar.

- [ ] **R-01**: Mover `mercanciasNode` dentro del bloque `if (cartaPorte != null)`.
- [ ] **R-02/R-03**: Unificar todos los `DateTime.Parse` y `decimal.Parse` con `CultureInfo.InvariantCulture`.
- [ ] **R-04**: Envolver la conversión HTML→PDF en `try/finally` para limpiar archivo temporal.
- [ ] **R-05**: Eliminar `Console.WriteLine` del `NativeLibraryLoader`.
- [ ] **R-06**: Agregar logging o re-throw con contexto en el catch de `MapAddenda`.
- [ ] **M-03**: Eliminar código comentado en `PdfService`.
- [ ] **M-01**: Eliminar `XmlNamespaceHelper` o integrarlo donde corresponda.

### Fase 2 — Definición de abstracciones e interfaces

> Objetivo: Establecer contratos que permitan desacoplar, testear y extender.

Interfaces propuestas:

```
Abstractions/
├── ICfdiXmlParser.cs          → Parsea XML a XDocument con validación
├── ICfdiModelMapper<TModel>.cs → Mapea XDocument a un ViewModel específico
├── IHtmlRenderer.cs           → Renderiza un ViewModel a HTML usando templates
├── IPdfConverter.cs           → Convierte HTML string/file a byte[] PDF
├── IQrGenerator.cs            → Genera QR en Base64 desde URL
├── ICfdiTypeDetector.cs       → Detecta el tipo de complemento del XML
└── ICfdiPdfGenerator.cs       → Contrato público de alto nivel
```

```csharp
/// <summary>
/// Contrato para mapear un XDocument a un ViewModel de complemento CFDI.
/// </summary>
public interface ICfdiModelMapper<TModel> where TModel : class
{
    /// <summary>
    /// Determina si este mapper puede procesar el XDocument dado.
    /// </summary>
    bool CanMap(XDocument xdoc);

    /// <summary>
    /// Mapea el XDocument al ViewModel correspondiente.
    /// </summary>
    TModel Map(XDocument xdoc);
}
```

```csharp
/// <summary>
/// Contrato para convertir HTML a PDF en bytes.
/// </summary>
public interface IPdfConverter
{
    byte[] ConvertHtml(string html);
}
```

### Fase 3 — Reestructuración de carpetas y separación de responsabilidades

> Objetivo: Organizar el código por responsabilidad y dominio.

```
CFDI.BuildPdf/
├── Abstractions/                    ← Interfaces y contratos
│   ├── ICfdiModelMapper.cs
│   ├── IHtmlRenderer.cs
│   ├── IPdfConverter.cs
│   ├── IQrGenerator.cs
│   └── ICfdiPdfGenerator.cs
│
├── Configuration/                   ← Opciones y registro de servicios
│   ├── CfdiPdfOptions.cs           ← Opciones configurables (mostrarMercancias, etc.)
│   └── ServiceCollectionExtensions.cs ← .AddCfdiPdfServices() para DI
│
├── Mappers/                         ← Un mapper por complemento
│   ├── CartaPorte/
│   │   └── CartaPorteMapper.cs
│   ├── Nomina/
│   │   └── NominaMapper.cs
│   └── Common/
│       ├── NumberToWordsConverter.cs
│       └── QrUrlBuilder.cs          ← Extraído de XmlToModelMapper
│
├── Models/                          ← Un archivo por ViewModel principal
│   ├── CartaPorte/
│   │   ├── CfdiCartaPorteViewModel.cs
│   │   ├── CartaPorteViewModel.cs
│   │   ├── UbicacionViewModel.cs
│   │   ├── MercanciaViewModel.cs
│   │   └── ...
│   └── Nomina/
│       ├── CfdiNominaViewModel.cs
│       ├── NominaViewModel.cs
│       ├── PercepcionesNominaViewModel.cs
│       └── ...
│
├── Rendering/                       ← Motor de renderizado HTML
│   ├── RazorHtmlRenderer.cs         ← Implementación con RazorLight
│   └── Templates/                   ← .cshtml embebidos
│       ├── _Shared.css              ← Estilos compartidos
│       ├── CartaPorte.cshtml
│       ├── CondicionesContrato.cshtml
│       └── Nomina.cshtml
│
├── Pdf/                             ← Motor de conversión PDF
│   ├── DinkToPdfConverter.cs        ← Implementación actual (IPdfConverter)
│   └── NativeLibraryLoader.cs
│
├── Helpers/
│   └── QrGeneratorService.cs        ← Implementa IQrGenerator
│
├── Services/
│   ├── CfdiPdfGenerator.cs          ← Orquestador (implementa ICfdiPdfGenerator)
│   └── CfdiTypeDetector.cs          ← Detecta complemento del XML
│
└── CfdiPdf.cs                       ← Fachada estática (backward-compatible)
```

### Fase 4 — Aplicar patrones de diseño

| Patrón | Dónde | Para qué |
|---|---|---|
| **Strategy** | `ICfdiModelMapper<T>` | Cada complemento tiene su propia estrategia de mapeo. Se selecciona en runtime según el XML. |
| **Factory Method** | `CfdiTypeDetector` + Mapper registry | Detecta el tipo de complemento y resuelve el mapper/template correcto. |
| **Facade** | `CfdiPdf` (estática) | Mantiene la API pública simple y backward-compatible mientras la implementación interna cambia. |
| **Template Method** | Base class para mappers | Lógica común de parseo CFDI (emisor, receptor, sellos, QR) en clase base; cada complemento solo implementa su parte específica. |
| **Options Pattern** | `CfdiPdfOptions` | Configuración centralizada en lugar de parámetros dispersos. |

#### Template Method — Ejemplo conceptual

```csharp
/// <summary>
/// Clase base que encapsula el mapeo común de cualquier CFDI 4.0.
/// Las subclases implementan el mapeo específico del complemento.
/// </summary>
public abstract class BaseCfdiMapper<TModel> : ICfdiModelMapper<TModel> 
    where TModel : class
{
    public abstract bool CanMap(XDocument xdoc);
    
    public TModel Map(XDocument xdoc)
    {
        var model = CreateModel();
        MapComprobanteBase(xdoc, model);  // Emisor, Receptor, Sellos, QR
        MapComplemento(xdoc, model);       // Específico de cada complemento
        return model;
    }

    protected abstract TModel CreateModel();
    protected abstract void MapComplemento(XDocument xdoc, TModel model);
    
    // Lógica común reutilizada por todos los complementos
    protected void MapComprobanteBase(XDocument xdoc, TModel model) { ... }
}
```

### Fase 5 — Inyección de dependencias y compatibilidad con DI

> Objetivo: Permitir que la librería funcione tanto con DI (ASP.NET Core) como standalone.

```csharp
/// <summary>
/// Registro de servicios para integración con Microsoft.Extensions.DependencyInjection.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCfdiPdfServices(
        this IServiceCollection services,
        Action<CfdiPdfOptions>? configure = null)
    {
        if (configure != null)
            services.Configure(configure);

        services.AddSingleton<IPdfConverter, DinkToPdfConverter>();
        services.AddSingleton<IHtmlRenderer, RazorHtmlRenderer>();
        services.AddSingleton<IQrGenerator, QrGeneratorService>();
        services.AddTransient<ICfdiModelMapper<CfdiCartaPorteViewModel>, CartaPorteMapper>();
        services.AddTransient<ICfdiModelMapper<CfdiNominaViewModel>, NominaMapper>();
        services.AddTransient<ICfdiPdfGenerator, CfdiPdfGenerator>();

        return services;
    }
}
```

La fachada estática `CfdiPdf` se mantiene para backward-compatibility, construyendo internamente las dependencias.

### Fase 6 — Testing

> Objetivo: Garantizar que el refactor no rompa funcionalidad y establecer red de seguridad.

- [ ] Crear proyecto `CFDI.BuildPdf.Tests` (xUnit).
- [ ] Tests unitarios para cada mapper con XMLs de ejemplo embebidos.
- [ ] Tests unitarios para `NumberToWordsConverter`.
- [ ] Tests unitarios para `QrUrlBuilder`.
- [ ] Tests de integración para el pipeline completo (XML → PDF bytes).
- [ ] Validar que los PDF generados post-refactor sean idénticos a los actuales.

---

## 6. Orden de ejecución recomendado

| Orden | Fase | Riesgo | Esfuerzo |
|---|---|---|---|
| 1 | Fase 1 — Correcciones de robustez | Bajo | Bajo |
| 2 | Fase 6 — Tests (sobre código actual) | Bajo | Medio |
| 3 | Fase 2 — Interfaces | Bajo | Bajo |
| 4 | Fase 3 — Reestructuración | Medio | Medio |
| 5 | Fase 4 — Patrones | Medio | Medio |
| 6 | Fase 5 — DI | Bajo | Bajo |

> Se recomienda ejecutar Fase 1 y 2 primero para tener una red de seguridad antes de reestructurar.

---

## 7. Criterios de éxito

- Todas las interfaces definidas y ninguna dependencia directa entre implementaciones concretas en el orquestador.
- Agregar un nuevo complemento CFDI requiere solo: 1 ViewModel, 1 Mapper, 1 Template. Sin tocar código existente (OCP).
- Cobertura de tests unitarios >= 80% en Mappers y Helpers.
- La API pública (`CfdiPdf`) permanece backward-compatible.
- Zero `Console.WriteLine` en producción.
- Zero `catch` vacíos.
