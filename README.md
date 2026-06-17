
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

> **v3.0.0** cambia namespaces y requiere .NET 8. Si migras desde v2.x, consulta [MIGRATION.md](MIGRATION.md).

## 📥 Instalación

```bash
dotnet add package CFDI.BuildPdf
```

## 🚀 Características

- ✔️ Soporte para **CFDI 4.0** con complementos **Carta Porte 3.1** y **Nómina 1.2**.
- ✔️ Detección automática del tipo de complemento.
- ✔️ **Traducción automática de claves SAT** (`c_FormaPago`, `c_RegimenFiscal`, `c_UsoCFDI`, `c_TipoDeComprobante`, `c_TipoRelacion`, etc.): el PDF muestra `clave - descripción` legible en lugar de códigos crudos. Ver [CATALOGOS_SAT_MAPEADOS.md](CATALOGOS_SAT_MAPEADOS.md).
- ✔️ **Catálogos del Complemento Nómina 1.2** traducidos: `c_TipoContrato`, `c_TipoRegimen`, `c_PeriodicidadPago`, `c_RiesgoPuesto`, `c_Estado`, `c_TipoPercepcion`, `c_TipoDeduccion`, `c_TipoOtroPago`, `c_TipoIncapacidad`, `c_TipoHoras`.
- ✔️ Soporte para `<cfdi:CfdiRelacionados>`: render condicional de los UUIDs relacionados con su `TipoRelacion` descrito.
- ✔️ Identificación del **PAC timbrador** por RFC (Buzón E, InvoiceOne, SAT pruebas; ampliable).
- ✔️ Múltiples formatos de entrada: ruta de archivo, `string`, `byte[]` y `Stream`.
- ✔️ Escritura directa a archivo o `Stream` de salida (ideal para respuestas HTTP).
- ✔️ Opciones configurables: mostrar/ocultar mercancías, condiciones del contrato, addenda; logotipo en Base64; orientación portrait/landscape.
- ✔️ Inyección de dependencias con `Microsoft.Extensions.DependencyInjection`.
- ✔️ Integración opcional con `ILogger` para diagnóstico.
- ✔️ Excepciones de dominio claras (`CfdiXmlInvalidoException`, `CfdiComplementoNoSoportadoException`).

## 📁 Estructura del PDF generado

- Datos del emisor y receptor (con `Régimen Fiscal` traducido).
- **Datos de Emisión**: fecha, serie/folio, moneda, tipo de cambio, lugar de expedición, `Exportación` (traducida) y sub-bloque **CFDI Relacionados** cuando el XML lo incluye.
- **Forma / Método de Pago**: con `FormaPago`, `MetodoPago` y `TipoDeComprobante` traducidos.
- Conceptos facturados: clave producto/servicio, `ClaveUnidad` traducida, descripción, importes formateados (`N2` es-MX), descuentos y `ObjetoImp` descrito.
- Totales e impuestos (`c_Impuesto`: 001 ISR, 002 IVA, 003 IEPS).
- Addenda genérica (opcional).
- **Complemento Carta Porte**: ubicaciones, mercancías (detalle o resumen), autotransporte (`c_TipoPermiso`, `c_ConfigAutotransporte` descritos), seguros, remolques (`c_SubTipoRem` descrito), figuras de transporte (`c_FiguraTransporte` descrito), página de condiciones del contrato.
- **Complemento Nómina**: datos del empleado, percepciones, deducciones, otros pagos, incapacidades, totales.
- Bloque fiscal: UUID, fechas, certificados, **PAC que timbró** identificado por RFC.
- QR y sellos digitales (sello CFDI, sello SAT, cadena original del complemento de certificación).

## ⚠️ Licencia QuestPDF (léeme antes de producción)

Esta librería usa **QuestPDF**, que tiene licencia dual:

- **Community** (gratuita) — apta para la mayoría de proyectos open-source y empresas que cumplan los criterios de elegibilidad vigentes del proveedor.
- **Professional / Enterprise** (de pago) — requerida por QuestPDF para el resto de organizaciones.

**El consumidor de esta librería es responsable de elegir y adquirir la licencia correcta.** Consulta los términos vigentes en [https://www.questpdf.com/license/](https://www.questpdf.com/license/).

Por defecto, `CFDI.BuildPdf` declara `Community`. Para cambiarlo:

### Con la fachada estática

```csharp
using CFDI.BuildPdf;

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
using CFDI.BuildPdf;

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

## 🗂️ Traducción automática de catálogos SAT

El PDF traduce las claves del SAT a su descripción legible en tiempo de render — no necesitas pre-procesar el XML. Ejemplos del output:

| Campo | Valor en el XML | Valor en el PDF |
|---|---|---|
| Régimen Fiscal | `624` | `624 - Coordinados` |
| Forma de Pago | `99` | `99 - Por definir` |
| Método de Pago | `PPD` | `PPD - Pago en parcialidades o diferido` |
| Uso del CFDI | `G03` | `G03 - Gastos en general` |
| Tipo de Comprobante | `I` | `I - Ingreso` |
| Exportación | `01` | `01 - No aplica` |
| Tipo Relación | `04` | `04 - Sustitución de los CFDI previos` |
| Objeto Imp. | `02` | `Sí objeto de impuesto` |
| Clave Unidad | `E48` | `Unidad de Servicio` |
| PAC que timbró | `SST060807KU0` | `Buzón E (SST060807KU0)` |
| Tipo Nómina (contrato) | `01` | `01 - Contrato de trabajo por tiempo indeterminado` |
| Periodicidad Pago (Nómina) | `04` | `04 - Quincenal` |
| Tipo Percepción (Nómina) | `001` | `001 - Sueldos, Salarios Rayas y Jornales` |
| Clave Entidad Federativa | `SIN` | `SIN - Sinaloa` |

Si una clave no está en el catálogo embebido se renderiza tal cual (fallback seguro, sin excepción). Catálogos soportados (23 helpers / 36 campos): `c_ClaveUnidad`, `c_Impuesto`, `c_ObjetoImp`, `c_UsoCFDI`, `c_RegimenFiscal`, `c_FormaPago`, `c_MetodoPago`, `c_Exportacion`, `c_TipoDeComprobante`, `c_TipoRelacion`, `c_CveTransporte`, `c_TipoPermiso`, `c_ConfigAutotransporte`, `c_SubTipoRem`, `c_FiguraTransporte`, `c_TipoContrato`, `c_TipoRegimen`, `c_PeriodicidadPago`, `c_RiesgoPuesto`, `c_Estado`, `c_TipoPercepcion`, `c_TipoDeduccion`, `c_TipoOtroPago`, `c_TipoIncapacidad`, `c_TipoHoras`. Inventario completo en [CATALOGOS_SAT_MAPEADOS.md](CATALOGOS_SAT_MAPEADOS.md).

### Agregar un PAC propio al diccionario

El RFC del PAC se traduce a nombre comercial para mostrarlo en el bloque fiscal. Si tu PAC no está en el diccionario embebido, el PDF mostrará `"PAC no identificado"`. Envía un PR agregando la entrada en `PdfBuilders/Common/CfdiPdfSections.cs` (diccionario `PacsConocidos`) o ábrenos un issue con el RFC y nombre comercial.

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
using CFDI.BuildPdf;
using Microsoft.Extensions.DependencyInjection;

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
using CFDI.BuildPdf;

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
