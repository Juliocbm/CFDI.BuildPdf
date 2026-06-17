using System;
using System.Globalization;
using CFDI.BuildPdf.Catalogs;
using CFDI.BuildPdf.Models;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CFDI.BuildPdf.PdfBuilders.Common
{
    /// <summary>
    /// Secciones compartidas de PDF reutilizadas entre los builders de CartaPorte y Nómina.
    /// </summary>
    internal static class CfdiPdfSections
    {
        /// <summary>
        /// Renderiza el footer fiscal: QR + sellos digitales.
        /// </summary>
        public static void ComposeFooterFiscal(IContainer container, CfdiViewModelBase model)
        {
            container.Column(col =>
            {
                col.Item().PaddingTop(10).Element(c => SectionTitle(c, "INFORMACIÓN FISCAL DIGITAL"));

                col.Item().PaddingTop(4).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3);
                        c.RelativeColumn(7);
                    });

                    table.Cell().Row(1).Column(1)
                        .AlignCenter().AlignMiddle()
                        .MinHeight(120)
                        .Element(cell =>
                        {
                            if (!string.IsNullOrEmpty(model.QRCodeBase64))
                            {
                                var qrBytes = Convert.FromBase64String(model.QRCodeBase64);
                                cell.AlignCenter().Width(110).Height(110).Image(qrBytes);
                            }
                        });

                    table.Cell().Row(1).Column(2).PaddingLeft(6).Column(right =>
                    {
                        right.Item().Text("SELLO DIGITAL DEL CFDI").Bold()
                            .FontSize(PdfStyleConstants.FontSizeSmall)
                            .FontColor(PdfStyleConstants.ColorAccent);
                        right.Item().PaddingTop(1).Text(model.SelloEmisor ?? "")
                            .FontSize(PdfStyleConstants.FontSizeVerySmall)
                            .FontColor(PdfStyleConstants.ColorSecondaryText);

                        right.Item().PaddingTop(4).Text("CADENA ORIGINAL DEL COMPLEMENTO DE CERTIFICACIÓN DIGITAL DEL SAT").Bold()
                            .FontSize(PdfStyleConstants.FontSizeSmall)
                            .FontColor(PdfStyleConstants.ColorAccent);
                        right.Item().PaddingTop(1).Text(model.CadenaOriginalSAT ?? "")
                            .FontSize(PdfStyleConstants.FontSizeVerySmall)
                            .FontColor(PdfStyleConstants.ColorSecondaryText);

                        right.Item().PaddingTop(4).Text("SELLO DIGITAL DEL SAT").Bold()
                            .FontSize(PdfStyleConstants.FontSizeSmall)
                            .FontColor(PdfStyleConstants.ColorAccent);
                        right.Item().PaddingTop(1).Text(model.SelloSAT ?? "")
                            .FontSize(PdfStyleConstants.FontSizeVerySmall)
                            .FontColor(PdfStyleConstants.ColorSecondaryText);
                    });
                });
            });
        }

        /// <summary>
        /// Renderiza una fila de tabla con encabezado (th) y valor (td).
        /// </summary>
        public static void HeaderValueRow(TableDescriptor table, uint row, uint startCol, string header, string? value)
        {
            table.Cell().Row(row).Column(startCol)
                .Border(0.5f).BorderColor(PdfStyleConstants.ColorBorderSoft)
                .Background(PdfStyleConstants.ColorSectionBg)
                .Padding(3).Text(header).Bold()
                .FontSize(PdfStyleConstants.FontSizeLabel)
                .FontColor(PdfStyleConstants.ColorText);

            table.Cell().Row(row).Column(startCol + 1)
                .Border(0.5f).BorderColor(PdfStyleConstants.ColorBorderSoft)
                .Padding(3).Text(value ?? "")
                .FontSize(PdfStyleConstants.FontSizeLabel)
                .FontColor(PdfStyleConstants.ColorText);
        }

        /// <summary>
        /// Renderiza el título de una sección como banner oscuro full-width con texto blanco.
        /// </summary>
        public static void SectionTitle(IContainer container, string title)
        {
            container.PaddingTop(8).Background(PdfStyleConstants.ColorHeaderBg)
                .PaddingVertical(3).PaddingHorizontal(6)
                .Text(title.ToUpperInvariant())
                .Bold()
                .FontSize(PdfStyleConstants.FontSizeSectionTitle)
                .FontColor(PdfStyleConstants.ColorHeaderText);
        }

        /// <summary>
        /// Renderiza una celda de encabezado de tabla (fondo oscuro, texto blanco bold uppercase).
        /// </summary>
        public static void TableHeaderCell(IContainer cell, string text)
        {
            cell.Background(PdfStyleConstants.ColorHeaderBg)
                .BorderColor(PdfStyleConstants.ColorHeaderBg)
                .PaddingVertical(2).PaddingHorizontal(2)
                .Text(text.ToUpperInvariant()).Bold()
                .FontSize(PdfStyleConstants.FontSizeVerySmall)
                .FontColor(PdfStyleConstants.ColorHeaderText);
        }

        /// <summary>
        /// Renderiza una celda de cuerpo de tabla con borde sutil.
        /// </summary>
        public static IContainer TableBodyCell(IContainer cell)
        {
            return cell.Border(0.3f).BorderColor(PdfStyleConstants.ColorBorderSoft).Padding(2);
        }

        /// <summary>
        /// Formatea un decimal como moneda MXN.
        /// </summary>
        public static string FormatCurrency(decimal value)
        {
            return value.ToString("C2", CultureInfo.GetCultureInfo("es-MX"));
        }

        /// <summary>
        /// Formatea un decimal a 6 posiciones.
        /// </summary>
        public static string Format6(decimal value)
        {
            return value.ToString("F6", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Formatea un decimal a 2 posiciones con separador de miles (es-MX).
        /// </summary>
        public static string Format2(decimal value)
        {
            return value.ToString("N2", CultureInfo.GetCultureInfo("es-MX"));
        }

        /// <summary>
        /// Formatea una tasa o cuota como porcentaje (0.160000 → "16.00%").
        /// Para TipoFactor "Cuota" devuelve el valor con 6 decimales sin símbolo.
        /// </summary>
        public static string FormatTasaOCuota(decimal tasaOCuota, string? tipoFactor)
        {
            if (string.Equals(tipoFactor, "Cuota", StringComparison.OrdinalIgnoreCase))
                return tasaOCuota.ToString("F6", CultureInfo.InvariantCulture);

            return (tasaOCuota * 100m).ToString("F2", CultureInfo.InvariantCulture) + "%";
        }

        /// <summary>
        /// Renderiza el encabezado fiscal compartido: logo + datos del emisor + datos de certificación.
        /// Reutilizable en CartaPorte y Nómina porque todos los campos provienen de CfdiViewModelBase.
        /// </summary>
        public static void ComposeEncabezado(IContainer container, CfdiViewModelBase model, ILogger logger)
        {
            container.BorderBottom(1f).BorderColor(PdfStyleConstants.ColorBorder).PaddingBottom(6)
                .Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(30); // Logo
                    c.RelativeColumn(35); // Emisor
                    c.RelativeColumn(35); // Datos fiscales
                });

                // Logo
                table.Cell().Row(1).Column(1).AlignLeft().AlignMiddle()
                    .Element(cell =>
                    {
                        if (!string.IsNullOrEmpty(model.LogoBase64))
                        {
                            if (TryDecodeLogo(model.LogoBase64, logger, out var logoBytes))
                                cell.MaxWidth(150).MaxHeight(70).Image(logoBytes!);
                        }
                    });

                // Emisor: nombre + RFC + régimen + lugar
                table.Cell().Row(1).Column(2).PaddingHorizontal(6).AlignMiddle().Column(c =>
                {
                    c.Item().Text(model.EmisorNombre ?? "")
                        .Bold()
                        .FontSize(PdfStyleConstants.FontSizeEmisorName)
                        .FontColor(PdfStyleConstants.ColorAccent);
                    c.Item().PaddingTop(2).Text(t =>
                    {
                        t.Span("RFC: ").Bold().FontSize(PdfStyleConstants.FontSizeLabel).FontColor(PdfStyleConstants.ColorText);
                        t.Span(model.EmisorRFC ?? "").FontSize(PdfStyleConstants.FontSizeLabel).FontColor(PdfStyleConstants.ColorText);
                    });
                    c.Item().PaddingTop(1).Text(t =>
                    {
                        t.Span("RÉGIMEN FISCAL: ").Bold().FontSize(PdfStyleConstants.FontSizeLabel).FontColor(PdfStyleConstants.ColorText);
                        t.Span($"{model.EmisorRegimenFiscal} - {SatCatalogos.NombreRegimenFiscal(model.EmisorRegimenFiscal)}").FontSize(PdfStyleConstants.FontSizeLabel).FontColor(PdfStyleConstants.ColorText);
                    });
                    c.Item().PaddingTop(1).Text(t =>
                    {
                        t.Span("LUGAR DE EXPEDICIÓN: ").Bold().FontSize(PdfStyleConstants.FontSizeLabel).FontColor(PdfStyleConstants.ColorText);
                        t.Span(model.LugarExpedicion ?? "").FontSize(PdfStyleConstants.FontSizeLabel).FontColor(PdfStyleConstants.ColorText);
                    });
                });

                // Datos fiscales a la derecha
                table.Cell().Row(1).Column(3).AlignMiddle().Column(c =>
                {
                    c.Item().Text(t =>
                    {
                        t.Span("UUID: ").Bold()
                            .FontSize(PdfStyleConstants.FontSizeSmall)
                            .FontColor(PdfStyleConstants.ColorAccent);
                        t.Span(model.UUID ?? "")
                            .FontSize(PdfStyleConstants.FontSizeVerySmall)
                            .FontColor(PdfStyleConstants.ColorText);
                    });
                    FiscalRow(c, "FECHA CERTIFICACIÓN:", model.FechaCertificacion.ToString("dd/MM/yyyy HH:mm:ss"));
                    FiscalRow(c, "NO. CERTIFICADO SAT:", model.NoCertificadoSAT);
                    FiscalRow(c, "NO. CERTIFICADO EMISOR:", model.NoCertificadoEmisor);
                    FiscalRow(c, "PAC QUE TIMBRÓ:", $"{SatCatalogos.NombrePac(model.RfcProvCertif)} ({model.RfcProvCertif})");
                    FiscalRow(c, "VERSIÓN CFDI:", model.Version);
                });
            });
        }

        private static void FiscalRow(ColumnDescriptor col, string label, string? value)
        {
            col.Item().Text(t =>
            {
                t.Span(label + " ").Bold()
                    .FontSize(PdfStyleConstants.FontSizeSmall)
                    .FontColor(PdfStyleConstants.ColorAccent);
                t.Span(value ?? "")
                    .FontSize(PdfStyleConstants.FontSizeSmall)
                    .FontColor(PdfStyleConstants.ColorText);
            });
        }

        /// <summary>
        /// Intenta decodificar una cadena Base64 de logo. Devuelve true y los bytes si tiene éxito;
        /// false (con logoBytes = null) si la cadena no es Base64 válido, registrando una advertencia.
        /// </summary>
        internal static bool TryDecodeLogo(string logoBase64, ILogger logger, out byte[]? logoBytes)
        {
            try
            {
                logoBytes = Convert.FromBase64String(logoBase64);
                return true;
            }
            catch (FormatException ex)
            {
                logger.LogWarning(ex, "No se pudo decodificar el logo en Base64 proporcionado por opciones; se omitirá del PDF.");
                logoBytes = null;
                return false;
            }
        }
    }
}
