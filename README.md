
# 📦 CFDI.BuildPdf

[![NuGet](https://img.shields.io/nuget/v/CFDI.BuildPdf.svg?style=flat-square)](https://www.nuget.org/packages/CFDI.BuildPdf/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/CFDI.BuildPdf.svg?style=flat-square)](https://www.nuget.org/packages/CFDI.BuildPdf/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)](https://opensource.org/licenses/MIT)
[![GitHub stars](https://img.shields.io/github/stars/Juliocbm/CFDI.BuildPdf?style=flat-square)](https://github.com/Juliocbm/CFDI.BuildPdf/stargazers)

## Descripción general

**CFDI.BuildPdf** es una librería .NET 8+ que genera una representación impresa en PDF a partir de un XML **CFDI 4.0**. Detecta automáticamente el tipo de comprobante/complemento y soporta actualmente:

- **Factura base (Ingreso/Egreso) sin complemento**
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
- ✔️ **PAC que timbró**: RFC del timbre y, opcionalmente, su nombre comercial configurado por el consumidor (`NombresPac`).
- ✔️ Múltiples formatos de entrada: ruta de archivo, `string`, `byte[]` y `Stream`.
- ✔️ Escritura directa a archivo o `Stream` de salida (ideal para respuestas HTTP).
- ✔️ Opciones configurables: mostrar/ocultar mercancías, condiciones del contrato, addenda; logotipo en Base64; orientación portrait/landscape; texto libre en el pie de página (folio, leyenda, código).
- ✔️ Inyección de dependencias con `Microsoft.Extensions.DependencyInjection`.
- ✔️ Integración opcional con `ILogger` para diagnóstico.
- ✔️ Excepciones de dominio claras (`CfdiXmlInvalidoException`, `CfdiComplementoNoSoportadoException`).

## 📁 Estructura del PDF generado

> **Paginación:** los títulos de sección nunca quedan solos al pie de una hoja; cuando una tabla continúa en la hoja siguiente se repiten su título y sus encabezados, las filas no se parten entre hojas y los bloques chicos (datos clave-valor, totales, QR + sellos) se mantienen completos.

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

- .NET 8.0 o superior.
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
| PAC que timbró | `SST060807KU0` | `SST060807KU0`, o `Buzón E (SST060807KU0)` si se configura en `NombresPac` |
| Tipo Nómina (contrato) | `01` | `01 - Contrato de trabajo por tiempo indeterminado` |
| Periodicidad Pago (Nómina) | `04` | `04 - Quincenal` |
| Tipo Percepción (Nómina) | `001` | `001 - Sueldos, Salarios Rayas y Jornales` |
| Clave Entidad Federativa | `SIN` | `SIN - Sinaloa` |

Si una clave no está en el catálogo embebido se renderiza tal cual (fallback seguro, sin excepción). Catálogos soportados (23 helpers / 36 campos): `c_ClaveUnidad`, `c_Impuesto`, `c_ObjetoImp`, `c_UsoCFDI`, `c_RegimenFiscal`, `c_FormaPago`, `c_MetodoPago`, `c_Exportacion`, `c_TipoDeComprobante`, `c_TipoRelacion`, `c_CveTransporte`, `c_TipoPermiso`, `c_ConfigAutotransporte`, `c_SubTipoRem`, `c_FiguraTransporte`, `c_TipoContrato`, `c_TipoRegimen`, `c_PeriodicidadPago`, `c_RiesgoPuesto`, `c_Estado`, `c_TipoPercepcion`, `c_TipoDeduccion`, `c_TipoOtroPago`, `c_TipoIncapacidad`, `c_TipoHoras`. Inventario completo en [CATALOGOS_SAT_MAPEADOS.md](CATALOGOS_SAT_MAPEADOS.md).

### Nombre del PAC que timbró (configurable)

El campo **PAC QUE TIMBRÓ** imprime siempre el RFC del PAC, que viene del timbre (`RfcProvCertif`) y es el dato fiscal. El nombre comercial es opcional y lo aporta el consumidor con `NombresPac` (RFC → nombre; las claves no distinguen mayúsculas): con nombre se imprime `Nombre (RFC)`, sin nombre **solo el RFC**.

La librería **no trae nombres de PAC**: los PACs cambian de RFC, de nombre o pierden la autorización fuera del ciclo de versiones de una librería, así que ese dato vive en la configuración de quien la usa. Cambiar de PAC es editar configuración, no publicar otra versión.

```csharp
options.NombresPac["SST060807KU0"] = "Buzón E";
options.NombresPac["PPD101129EA3"] = "Mi PAC nuevo";
```

O desde `appsettings.json` (ver [inyección de dependencias](#-uso-con-inyección-de-dependencias)):

```json
"CfdiPdf": {
  "NombresPac": { "PPD101129EA3": "Mi PAC nuevo" }
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
    Orientacion = PdfOrientation.Portrait,
    TextoPiePagina = "FOLIO INTERNO: HG-123456", // Texto libre en el pie de todas las hojas (opcional)
    NombresPac = { ["PPD101129EA3"] = "Mi PAC" } // RFC → nombre del PAC (opcional; sin nombre se imprime el RFC)
};

var pdfBytes = await CfdiPdf.DesdeRutaAsync(rutaXml, options);
```

### Texto libre en el pie de página

`TextoPiePagina` imprime un texto propio (folio interno, leyenda, código de control, etc.) centrado en el pie de **todas las hojas**, arriba de la leyenda y el paginado. En Carta Porte también aparece en la hoja de condiciones del contrato.

- Admite saltos de línea (`\n`); se muestran **a lo más 2 renglones** y el excedente se corta con `…`, para que un texto largo nunca rompa el layout.
- Los renglones vacíos se descartan y los caracteres de control (tabuladores, etc.) se cambian por espacio.
- `null` o vacío (default): no se imprime nada y el PDF queda igual que sin la opción.

```
                 FOLIO INTERNO: HG-123456
ESTE DOCUMENTO ES UNA REPRESENTACIÓN IMPRESA DE UN CFDI    Página 1 de 2
```

Para probarlo sin integrar la librería, el demo de consola acepta `--pie`:

```bash
dotnet run --project CFDI.BuildPdf.ConsoleDemo -- ./cfdi.xml ./salida.pdf --pie "FOLIO INTERNO: HG-123456"
```

## 💉 Uso con inyección de dependencias

### Registro de servicios

```csharp
using CFDI.BuildPdf;
using Microsoft.Extensions.DependencyInjection;

builder.Services.AddCfdiPdfServices(
    configure: opts =>
    {
        opts.TextoPiePagina = "HG-F-CXC-02 REV.:02 24/09/2026";
        opts.NombresPac["SST060807KU0"] = "Buzón E";
    },
    licenseType: CfdiPdfLicenseType.Community);
```

Las opciones de `configure` son las **opciones por defecto**: se aplican en cada llamada a `ICfdiPdfGenerator` que no traiga las suyas. Si una llamada pasa su propio `CfdiPdfOptions`, se usa ése completo, con una excepción: `NombresPac` es un catálogo acumulable y **se combina** con el de DI (si un RFC está en ambos, gana el de la llamada). Así los nombres de PAC configurados una vez no se pierden cuando una llamada trae sus propias opciones.

También se pueden cargar desde `appsettings.json`, así un cambio de leyenda o de PAC es solo de configuración:

```csharp
builder.Services.Configure<CfdiPdfOptions>(builder.Configuration.GetSection("CfdiPdf"));
builder.Services.AddCfdiPdfServices();
```

```json
"CfdiPdf": {
  "TextoPiePagina": "HG-F-CXC-02 REV.:02 24/09/2026",
  "NombresPac": { "SST060807KU0": "Buzón E" }
}
```

> Antes de 4.0.0 las opciones de `configure` se registraban pero no se aplicaban.

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
