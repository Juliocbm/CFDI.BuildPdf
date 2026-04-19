# CFDI.BuildPdf — Changelog

Todas las versiones notables de este proyecto se documentan en este archivo.
El formato sigue [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/)
y este proyecto usa [Versionado Semántico](https://semver.org/lang/es/).

## [2.0.6] - 2026-04-18

### Added
- Catálogo `c_TipoDeComprobante` → helper `NombreTipoComprobante` con las 5 claves del SAT: `I` (Ingreso), `E` (Egreso), `T` (Traslado), `P` (Pago), `N` (Nómina).
- Campo **Tipo de Comprobante** ahora se muestra como `clave - descripción` tanto en Carta Porte (bloque Forma/Método de Pago) como en Nómina (Datos del Comprobante), consistente con el resto de catálogos.

## [2.0.5] - 2026-04-16

### Added
- Soporte para `<cfdi:CfdiRelacionados>`: se renderiza el `TipoRelacion` con su descripción del catálogo `c_TipoRelacion` y la lista de UUIDs relacionados.
  - En **Carta Porte**: aparece como sub-bloque "CFDI RELACIONADOS" dentro de **Datos de Emisión** (un UUID por línea).
  - En **Nómina**: aparece como fila adicional en **Datos del Comprobante** (UUIDs concatenados con coma).
  - Render condicional: el bloque sólo se muestra cuando el nodo existe en el XML, sin afectar facturas sin relacionados.
- Helper `CfdiPdfSections.NombreTipoRelacion` con las 7 entradas del catálogo SAT `c_TipoRelacion`.

### Changed
- `CfdiViewModelBase` expone dos propiedades nuevas (`TipoRelacion`, `RelacionadosUuids`). Aditivo, no rompe consumidores existentes.

## [2.0.4] - 2026-04-15

### Added
- Catálogo `c_UsoCFDI` → helper `NombreUsoCFDI` (G01–G03, I01–I08, D01–D10, S01, CP01, CN01). Aplicado en el bloque Cliente (Carta Porte) y Datos del Comprobante (Nómina).

### Fixed
- El campo UUID (Folio Fiscal) ya no parte el valor a mitad de renglón.

## [2.0.3] - 2026-04-14

### Added
- Catálogos SAT añadidos al template con formato `clave - descripción`:
  - `c_FormaPago` → `NombreFormaPago` (01–31, 99).
  - `c_MetodoPago` → `NombreMetodoPago` (PUE, PPD).
  - `c_RegimenFiscal` → `NombreRegimenFiscal` (601, 603–626) — aplicado al emisor y receptor en Carta Porte y Nómina.
  - `c_Exportacion` → `NombreExportacion` (01–04).
- Identificador del PAC timbrador: helper `NombrePac` + diccionario `PacsConocidos` (RFC → nombre comercial). Incluye Buzón E (`SST060807KU0`), InvoiceOne (`SED1102088J7`), SAT pruebas (`SAT970701NN3`) y fallback visible `"PAC no identificado"`.
- Propiedad `RfcProvCertif` en `CfdiViewModelBase` (se lee del TFD).

## [2.0.2] - 2026-04-13

### Fixed
- Re-empaquetado en Release tras detectar que 2.0.1 se subió sin los cambios compilados (el `dotnet pack --no-build` tomó binarios Debug obsoletos). A partir de esta versión el flujo exige `dotnet build -c Release` explícito antes de empacar.

## [2.0.1] - 2026-04-12

### Changed
- Reducción de `FontSize` en títulos y cabeceras de tabla (se percibían demasiado grandes frente al PDF del proveedor).
- Nueva paleta con más contraste (banners `#2C3E50`, accent `#1F4E79`, bordes principales `#999999`).
- Alineación numérica a la derecha en columnas de montos (Valor Unitario, Importe, Descuento, Total).

### Fixed
- Wrap de cabeceras que partía palabras a la mitad (`CANTI/DAD`, `DESCUEN/TO`).
- Wrap de valores numéricos (`5424.800000` → `5,424.80` con formato `N2` es-MX).
- Traducción `c_ObjetoImp`: se mostraba como `"01"` literal; ahora muestra la descripción (`"Sí objeto de impuesto"`).
- Formato de fechas ISO → `dd/MM/yyyy HH:mm:ss`.

## [2.0.0] - 2026-04-11

### Added
- Arquitectura SOLID completa con inyección de dependencias vía `Microsoft.Extensions.DependencyInjection`.
- Soporte para **Complemento Nómina 1.2** con layout dedicado.
- Detección automática del tipo de complemento (Carta Porte / Nómina) mediante `ICfdiTypeDetector`.
- Nuevos métodos: `DesdeStreamAsync`, `GuardarDesdeRutaAsync`, `EscribirEnStreamAsync`.
- Configuración centralizada vía `CfdiPdfOptions`.
- Extensión `AddCfdiPdfServices` para registrar los servicios en un contenedor DI.
- Integración con `Microsoft.Extensions.Logging.Abstractions` (opcional) para diagnóstico en mappers y builders.
- Validación explícita de inputs con excepciones de dominio claras.
- Método `CfdiPdf.ConfigureQuestPdfLicense` para cambiar el tipo de licencia QuestPDF antes del primer uso.
- Tests unitarios con xUnit (mappers, detector, QR URL builder, number-to-words).

### Changed
- **BREAKING:** Motor de PDF migrado de `wkhtmltopdf` (dependencia nativa) a **QuestPDF 2024.3.5** (100 % gestionado, cross-platform).
- **BREAKING:** Los métodos de `CfdiPdf` ahora reciben `CfdiPdfOptions` en lugar de parámetros sueltos (`bool mostrarMercancias`, `string logoBase64`).
- Reorganización interna en capas: `Abstractions`, `Services`, `Mappers`, `PdfBuilders`, `Models`, `Configuration`.
- Mappers y builders ahora son `internal`; solo las abstracciones son públicas.

### Removed
- Dependencia de `wkhtmltopdf` y binarios nativos asociados.
- API anterior basada en templates Razor (`.cshtml`).

### Security
- `XDocument.Load` aprovecha el comportamiento seguro por defecto de .NET 6+ (XmlResolver deshabilitado), mitigando XXE.
- Se eliminaron catches silenciosos; los errores recuperables ahora se registran vía `ILogger` con contexto.

## [1.0.4] - 2025-04-30

### Changed
- Se agrega README.md al paquete NuGet.

## [1.0.3] - 2025-04-30

### Fixed
- Conversión de cantidad decimal (`Total`) a cantidad en letra.
