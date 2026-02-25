using System;
using System.Globalization;
using CFDI.BuildPdf.Models;
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
                col.Item().PaddingTop(10).Text("Información Fiscal Digital")
                    .Bold().FontSize(PdfStyleConstants.FontSizeTitle);

                col.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3);
                        c.RelativeColumn(7);
                    });

                    table.Cell().Row(1).Column(1)
                        .AlignCenter().AlignMiddle()
                        .MinHeight(130)
                        .Element(cell =>
                        {
                            if (!string.IsNullOrEmpty(model.QRCodeBase64))
                            {
                                var qrBytes = Convert.FromBase64String(model.QRCodeBase64);
                                cell.AlignCenter().Width(130).Height(130).Image(qrBytes);
                            }
                        });

                    table.Cell().Row(1).Column(2).PaddingLeft(5).Column(right =>
                    {
                        right.Item().Text("Sello Digital del Emisor:").Bold().FontSize(PdfStyleConstants.FontSizeSmall);
                        right.Item().Text(model.SelloEmisor ?? "").FontSize(PdfStyleConstants.FontSizeVerySmall);

                        right.Item().PaddingTop(3).Text("Cadena Original del Complemento de Certificación Digital del SAT:").Bold().FontSize(PdfStyleConstants.FontSizeSmall);
                        right.Item().Text(model.CadenaOriginalSAT ?? "").FontSize(PdfStyleConstants.FontSizeVerySmall);

                        right.Item().PaddingTop(3).Text("Sello Digital del SAT:").Bold().FontSize(PdfStyleConstants.FontSizeSmall);
                        right.Item().Text(model.SelloSAT ?? "").FontSize(PdfStyleConstants.FontSizeVerySmall);
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
                .Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder)
                .Background(PdfStyleConstants.ColorHeaderBg)
                .Padding(3).Text(header).Bold().FontSize(PdfStyleConstants.FontSizeDefault);

            table.Cell().Row(row).Column(startCol + 1)
                .Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder)
                .Padding(3).Text(value ?? "").FontSize(PdfStyleConstants.FontSizeDefault);
        }

        /// <summary>
        /// Renderiza la sección de título de una sección del PDF.
        /// </summary>
        public static void SectionTitle(IContainer container, string title)
        {
            container.PaddingTop(8).Text(title)
                .Bold().FontSize(PdfStyleConstants.FontSizeTitle)
                .FontColor(PdfStyleConstants.ColorText);
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
    }
}
