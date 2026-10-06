using System;
using System.Globalization;
using System.Text;
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
        private const string LeyendaRepresentacionImpresa = "ESTE DOCUMENTO ES UNA REPRESENTACIÓN IMPRESA DE UN CFDI";

        /// <summary>
        /// Máximo de renglones del texto libre del pie; el excedente se corta con "…" para que un
        /// texto muy largo no haga crecer el pie (que se repite en cada hoja) hasta romper el layout.
        /// </summary>
        internal const int MaxRenglonesTextoPiePagina = 2;

        /// <summary>
        /// Una fila de tabla solo se parte entre hojas si quedan al menos estos puntos; si no, pasa completa a
        /// la siguiente. Con <c>EnsureSpace</c> (y no <c>ShowEntire</c>) una fila anómala más alta que una hoja
        /// se sigue partiendo en lugar de lanzar <c>DocumentLayoutException</c>.
        /// </summary>
        internal const float MinAlturaParaPartirFila = 120f;

        /// <summary>
        /// Igual que <see cref="MinAlturaParaPartirFila"/> para bloques chicos que se leen como unidad
        /// (tablas clave-valor, totales, QR + sellos): en la práctica nunca se parten.
        /// </summary>
        internal const float MinAlturaParaPartirBloque = 250f;

        /// <summary>
        /// Renderiza una sección: título + contenido. El título va en el <c>Before</c> de un Decoration, así
        /// nunca queda solo al pie de una hoja (si el contenido no cabe, la sección completa pasa a la siguiente)
        /// y se repite arriba del contenido cuando éste continúa en otra hoja.
        /// </summary>
        /// <param name="col">Columna del documento donde se agrega la sección.</param>
        /// <param name="titulo">Título de la sección (se pinta en mayúsculas).</param>
        /// <param name="contenido">Contenido de la sección (normalmente una tabla).</param>
        /// <param name="indivisible">El contenido es un bloque chico que no debe partirse entre hojas.</param>
        public static void Seccion(ColumnDescriptor col, string titulo, Action<IContainer> contenido, bool indivisible = false)
        {
            col.Item().Decoration(d =>
            {
                d.Before().Element(c => SectionTitle(c, titulo));
                var cuerpo = indivisible ? d.Content().EnsureSpace(MinAlturaParaPartirBloque) : d.Content();
                cuerpo.Element(contenido);
            });
        }

        /// <summary>
        /// Celda de una fila de datos que no se parte entre hojas (ver <see cref="MinAlturaParaPartirFila"/>).
        /// Al aplicarse a todas las celdas de la fila, la fila completa pasa a la hoja siguiente.
        /// </summary>
        public static IContainer SinPartir(this IContainer cell) => cell.EnsureSpace(MinAlturaParaPartirFila);

        /// <summary>
        /// Encabezados de una tabla de datos en <c>table.Header</c>: se repiten en cada hoja donde la tabla
        /// continúa y nunca quedan solos al pie sin al menos una fila debajo.
        /// </summary>
        public static void EncabezadosTabla(TableDescriptor table, params string[] encabezados)
        {
            table.Header(header =>
            {
                foreach (var encabezado in encabezados)
                    header.Cell().Element(c => TableHeaderCell(c, encabezado));
            });
        }

        /// <summary>
        /// Renderiza el pie de página de las hojas del comprobante: el texto libre de
        /// <see cref="CfdiPdfOptions.TextoPiePagina"/> (si viene) arriba de la línea de paginado.
        /// </summary>
        /// <param name="container">Contenedor del pie de página.</param>
        /// <param name="textoPiePagina">Texto libre del consumidor; null o vacío no agrega nada.</param>
        /// <param name="incluirLeyenda">Antepone al paginado la leyenda de representación impresa del CFDI.</param>
        public static void ComposePiePagina(IContainer container, string? textoPiePagina, bool incluirLeyenda)
        {
            container.Column(col =>
            {
                col.Item().Element(c => ComposeTextoPiePagina(c, textoPiePagina));

                col.Item().AlignCenter().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(PdfStyleConstants.FontSizeSmall));
                    if (incluirLeyenda)
                    {
                        text.Span(LeyendaRepresentacionImpresa);
                        text.Span("    Página ");
                    }
                    else
                    {
                        text.Span("Página ");
                    }
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                });
            });
        }

        /// <summary>
        /// Renderiza solo el texto libre del pie, centrado y a lo más <see cref="MaxRenglonesTextoPiePagina"/> renglones.
        /// No dibuja nada (altura cero) si el texto es nulo o queda vacío tras normalizarlo.
        /// </summary>
        public static void ComposeTextoPiePagina(IContainer container, string? textoPiePagina)
        {
            var texto = NormalizarTextoPiePagina(textoPiePagina);
            if (texto is null)
                return;

            container.PaddingBottom(1).Text(text =>
            {
                text.AlignCenter();
                text.ClampLines(MaxRenglonesTextoPiePagina);
                text.DefaultTextStyle(x => x.FontSize(PdfStyleConstants.FontSizeSmall));
                text.Span(texto);
            });
        }

        /// <summary>
        /// Unifica los saltos de línea a '\n', reemplaza por espacio cualquier otro carácter de control
        /// (tabuladores, nulos, etc., que el PDF pintaría como cajas), recorta cada renglón y descarta
        /// los renglones vacíos para que no consuman el tope de renglones. Devuelve null si no queda texto.
        /// </summary>
        internal static string? NormalizarTextoPiePagina(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return null;

            var sb = new StringBuilder(texto.Length);
            foreach (var c in texto.Replace("\r\n", "\n").Replace('\r', '\n'))
                sb.Append(c != '\n' && char.IsControl(c) ? ' ' : c);

            var renglones = sb.ToString()
                .Split('\n')
                .Select(r => r.Trim())
                .Where(r => r.Length > 0);

            var resultado = string.Join("\n", renglones);
            return resultado.Length == 0 ? null : resultado;
        }

        /// <summary>
        /// Renderiza el footer fiscal: QR + sellos digitales.
        /// </summary>
        public static void ComposeFooterFiscal(IContainer container, CfdiViewModelBase model)
        {
            // Título, QR y sellos se leen como unidad: el bloque pasa completo a la hoja siguiente si no cabe.
            container.EnsureSpace(MinAlturaParaPartirBloque).Column(col =>
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
            table.Cell().Row(row).Column(startCol).SinPartir()
                .Border(0.5f).BorderColor(PdfStyleConstants.ColorBorderSoft)
                .Background(PdfStyleConstants.ColorSectionBg)
                .Padding(3).Text(header).Bold()
                .FontSize(PdfStyleConstants.FontSizeLabel)
                .FontColor(PdfStyleConstants.ColorText);

            table.Cell().Row(row).Column(startCol + 1).SinPartir()
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
        public static void ComposeEncabezado(IContainer container, CfdiViewModelBase model, CfdiPdfOptions options, ILogger logger)
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
                    FiscalRow(c, "PAC QUE TIMBRÓ:", PacTimbrador.Texto(model.RfcProvCertif, options.NombresPac));
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
