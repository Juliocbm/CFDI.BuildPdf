# Guía de migración v3.x → v4.0.0

v4.0.0 **no cambia la API pública**: tu código compila igual. Es versión mayor porque cambia el contenido del PDF en un caso:

**La librería ya no trae nombres de PAC.** Antes traía un catálogo fijo de 3 (Buzón E, InvoiceOne, SAT pruebas); los PACs cambian de RFC, de nombre o de autorización fuera del ciclo de versiones de la librería, así que ese dato ahora lo aporta quien la usa. Sin configuración, el campo **PAC QUE TIMBRÓ** muestra solo el RFC del timbre (por ejemplo `SST060807KU0` en lugar de `Buzón E (SST060807KU0)`).

Para conservar los nombres, configúralos en `NombresPac`:

```csharp
var options = new CfdiPdfOptions();
options.NombresPac["SST060807KU0"] = "Buzón E";
options.NombresPac["SED1102088J7"] = "InvoiceOne";
```

o, con inyección de dependencias, una sola vez desde `appsettings.json`:

```json
"CfdiPdf": { "NombresPac": { "SST060807KU0": "Buzón E", "SED1102088J7": "InvoiceOne" } }
```

```csharp
builder.Services.Configure<CfdiPdfOptions>(builder.Configuration.GetSection("CfdiPdf"));
builder.Services.AddCfdiPdfServices();
```

Otros cambios de 4.0.0 que no requieren acción (ver [CHANGELOG.md](CHANGELOG.md)):
- Las opciones de `AddCfdiPdfServices(configure: ...)` ahora sí se aplican. Si las configurabas esperando un efecto, a partir de esta versión lo tienen.
- Paginación: los títulos ya no quedan solos al pie, las tablas repiten encabezados y las filas no se parten. En documentos de varias hojas el reparto del contenido puede cambiar.

---

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
El PDF generado es **idéntico** a v2.0.8: mismos mapeos, mismos catálogos SAT, mismo layout (verificado con pruebas golden y comparando byte a byte el texto contra el paquete v2.0.8). v3 es un refactor de arquitectura/superficie, no de salida.

## Soporte de v2.x
La rama `release/2.x` recibe parches críticos. v3 es opt-in.
