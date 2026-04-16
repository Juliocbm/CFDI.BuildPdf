# CFDI.BuildPdf — Changelog

Todas las versiones notables de este proyecto se documentan en este archivo.
El formato sigue [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/)
y este proyecto usa [Versionado Semántico](https://semver.org/lang/es/).

## [2.0.5] - 2026-04-16

### Added
- Soporte para `<cfdi:CfdiRelacionados>`: se renderiza el `TipoRelacion` con su descripción del catálogo `c_TipoRelacion` y la lista de UUIDs relacionados.
  - En **Carta Porte**: aparece como sub-bloque "CFDI RELACIONADOS" dentro de **Datos de Emisión** (un UUID por línea).
  - En **Nómina**: aparece como fila adicional en **Datos del Comprobante** (UUIDs concatenados con coma).
  - Render condicional: el bloque sólo se muestra cuando el nodo existe en el XML, sin afectar facturas sin relacionados.
- Helper `CfdiPdfSections.NombreTipoRelacion` con las 7 entradas del catálogo SAT `c_TipoRelacion`.

### Changed
- `CfdiViewModelBase` expone dos propiedades nuevas (`TipoRelacion`, `RelacionadosUuids`). Aditivo, no rompe consumidores existentes.

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
