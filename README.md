
# 📦 CFDI.BuildPdf

[![NuGet](https://img.shields.io/nuget/v/CFDI.BuildPdf.svg?style=flat-square)](https://www.nuget.org/packages/CFDI.BuildPdf/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/CFDI.BuildPdf.svg?style=flat-square)](https://www.nuget.org/packages/CFDI.BuildPdf/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)](https://opensource.org/licenses/MIT)
[![GitHub stars](https://img.shields.io/github/stars/Juliocbm/CFDI.BuildPdf?style=flat-square)](https://github.com/Juliocbm/CFDI.BuildPdf/stargazers)

## Descripción general

**CFDI.BuildPdf** es una librería .NET 6+ que genera una representación impresa en PDF a partir de un XML **CFDI 4.0**. Detecta automáticamente el tipo de complemento y soporta actualmente:

- **Complemento Carta Porte 3.1**
- **Complemento Nómina 1.2**

Es cross-platform (Windows, Linux, macOS, contenedores) y **no tiene dependencias nativas**: usa [QuestPDF](https://www.questpdf.com/) como motor de renderizado y [QRCoder](https://github.com/codebude/QRCoder) para el código QR del timbre fiscal.

## 📥 Instalación

```bash
dotnet add package CFDI.BuildPdf
```

## 🚀 Características

- ✔️ Soporte para **CFDI 4.0** con complementos **Carta Porte 3.1** y **Nómina 1.2**.
- ✔️ Detección automática del tipo de complemento.
- ✔️ Múltiples formatos de entrada: ruta de archivo, `string`, `byte[]` y `Stream`.
- ✔️ Escritura directa a archivo o `Stream` de salida (ideal para respuestas HTTP).
- ✔️ Opciones configurables: mostrar/ocultar mercancías, condiciones del contrato, addenda; logotipo en Base64; orientación portrait/landscape.
- ✔️ Inyección de dependencias con `Microsoft.Extensions.DependencyInjection`.
- ✔️ Integración opcional con `ILogger` para diagnóstico.
- ✔️ Excepciones de dominio claras (`CfdiXmlInvalidoException`, `CfdiComplementoNoSoportadoException`).

## 📁 Estructura del PDF generado

- Datos del emisor y receptor.
- UUID, fecha de certificación y certificados.
- Totales e impuestos del CFDI.
- Addenda genérica (si aplica).
- Complemento Carta Porte: ubicaciones, mercancías (detalle o resumen), autotransporte, seguros, remolques, figuras de transporte, página de condiciones del contrato.
- Complemento Nómina: datos del empleado, percepciones, deducciones, otros pagos, incapacidades, totales.
- QR y sellos digitales.

## ⚠️ Licencia QuestPDF (léeme antes de producción)

Esta librería usa **QuestPDF**, que tiene licencia dual:

- **Community** (gratuita) — apta para la mayoría de proyectos open-source y empresas que cumplan los criterios de elegibilidad vigentes del proveedor.
- **Professional / Enterprise** (de pago) — requerida por QuestPDF para el resto de organizaciones.

**El consumidor de esta librería es responsable de elegir y adquirir la licencia correcta.** Consulta los términos vigentes en [https://www.questpdf.com/license/](https://www.questpdf.com/license/).

Por defecto, `CFDI.BuildPdf` declara `Community`. Para cambiarlo:

### Con la fachada estática

```csharp
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Service;

// Llamar una sola vez al iniciar el proceso, antes de generar cualquier PDF
CfdiPdf.ConfigureQuestPdfLicense(CfdiPdfLicenseType.Professional);
```

### Con inyección de dependencias

```csharp
builder.Services.AddCfdiPdfServices(
    configure: opts => opts.MostrarMercancias = true,
    licenseType: CfdiPdfLicenseType.Professional);
```

## 📚 Requisitos

- .NET 6.0 o superior.
- Sin dependencias nativas (no requiere `wkhtmltopdf` ni binarios de sistema).

## 🔵 Uso básico (fachada estática)

### Desde una ruta de archivo

```csharp
using CFDI.BuildPdf.Service;

var pdfBytes = await CfdiPdf.DesdeRutaAsync(@"C:\facturas\cfdi.xml");
await File.WriteAllBytesAsync("cfdi_output.pdf", pdfBytes);
```

### Desde un string XML

```csharp
var pdfBytes = await CfdiPdf.DesdeXmlStringAsync(xmlString);
```

### Desde bytes

```csharp
var pdfBytes = await CfdiPdf.DesdeXmlBytesAsync(xmlBytes);
```

### Desde un Stream (por ejemplo, `IFormFile` en ASP.NET Core)

```csharp
await using var stream = archivoXml.OpenReadStream();
var pdfBytes = await CfdiPdf.DesdeStreamAsync(stream);
```

### Guardar directamente a archivo

```csharp
await CfdiPdf.GuardarDesdeRutaAsync(
    rutaXml: @"C:\facturas\cfdi.xml",
    rutaPdfDestino: @"C:\facturas\cfdi.pdf");
```

### Escribir en un Stream de salida (respuesta HTTP)

```csharp
[HttpPost("generar-pdf")]
public async Task<IActionResult> GenerarPdf(IFormFile xml)
{
    Response.ContentType = "application/pdf";
    await using var xmlStream = xml.OpenReadStream();
    await CfdiPdf.EscribirEnStreamAsync(xmlStream, Response.Body);
    return new EmptyResult();
}
```

## ⚙️ Opciones de generación

```csharp
var options = new CfdiPdfOptions
{
    MostrarMercancias = true,            // Carta Porte: mostrar detalle de mercancías
    MostrarCondicionesContrato = true,   // Carta Porte: incluir página de condiciones
    MostrarAddenda = true,               // Incluir sección de addenda
    LogoBase64 = logoBase64,             // Logo de la empresa (opcional)
    Orientacion = PdfOrientation.Portrait
};

var pdfBytes = await CfdiPdf.DesdeRutaAsync(rutaXml, options);
```

## 💉 Uso con inyección de dependencias

### Registro de servicios

```csharp
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Configuration;

builder.Services.AddCfdiPdfServices(
    configure: opts =>
    {
        opts.MostrarMercancias = true;
        opts.MostrarCondicionesContrato = true;
    },
    licenseType: CfdiPdfLicenseType.Community);
```

### Consumo desde un servicio

```csharp
using CFDI.BuildPdf.Abstractions;

public class FacturaService
{
    private readonly ICfdiPdfGenerator _generator;

    public FacturaService(ICfdiPdfGenerator generator)
    {
        _generator = generator;
    }

    public Task<byte[]> GenerarPdfAsync(string rutaXml)
        => _generator.GenerarDesdeRutaAsync(rutaXml);
}
```

## 🛑 Manejo de errores

Los métodos públicos lanzan excepciones de dominio específicas para facilitar el diagnóstico:

| Excepción | Cuándo ocurre |
|---|---|
| `ArgumentNullException` / `ArgumentException` | Inputs nulos, vacíos o inválidos. |
| `FileNotFoundException` | La ruta del XML no existe. |
| `CfdiXmlInvalidoException` | El contenido no es un XML bien formado. |
| `CfdiComplementoNoSoportadoException` | El CFDI no contiene Carta Porte 3.1 ni Nómina 1.2. |
| `CfdiPdfException` | Clase base para cualquier error de dominio de la librería. |

```csharp
try
{
    var pdf = await CfdiPdf.DesdeRutaAsync(ruta);
}
catch (CfdiXmlInvalidoException ex)
{
    logger.LogWarning(ex, "XML no válido: {Mensaje}", ex.Message);
}
catch (CfdiComplementoNoSoportadoException ex)
{
    logger.LogWarning(ex, "Complemento no soportado: {Mensaje}", ex.Message);
}
```

## 📝 Licencia

Este proyecto está bajo licencia [MIT](LICENSE). Recuerda que **QuestPDF** (dependencia interna) tiene su propia licencia dual — ver sección arriba.

## 👤 Autor

- [@Juliocbm](https://github.com/Juliocbm)
