using System;
using System.Globalization;
using System.Linq;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Models;
using CFDI.BuildPdf.PdfBuilders.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CFDI.BuildPdf.PdfBuilders.CartaPorte
{
    /// <summary>
    /// Construye el PDF de CFDI con complemento Carta Porte 3.1 usando QuestPDF.
    /// Replica el layout de TemplateFacturaCartaPorte.cshtml + TemplateCondicionesContrato.cshtml.
    /// </summary>
    internal class CartaPorteDocumentBuilder : IPdfDocumentBuilder<CfdiCartaPorteViewModel>
    {
        private readonly ILogger<CartaPorteDocumentBuilder> _logger;

        public CartaPorteDocumentBuilder(ILogger<CartaPorteDocumentBuilder>? logger = null)
        {
            _logger = logger ?? NullLogger<CartaPorteDocumentBuilder>.Instance;
        }

        /// <inheritdoc />
        public byte[] Build(CfdiCartaPorteViewModel model, CfdiPdfOptions options)
        {
            var pageSize = options.Orientacion == PdfOrientation.Landscape
                ? PageSizes.Letter.Landscape()
                : PageSizes.Letter;

            var document = Document.Create(container =>
            {
                // Página principal: CFDI Carta Porte
                container.Page(page =>
                {
                    page.Size(pageSize);
                    page.MarginTop(0.7f, Unit.Centimetre);
                    page.MarginBottom(1.2f, Unit.Centimetre);
                    page.MarginHorizontal(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(PdfStyleConstants.FontSizeDefault).FontFamily(PdfStyleConstants.FontFamily));

                    page.Content().Column(col =>
                    {
                        ComposeEncabezado(col, model);
                        ComposeLogoYCertificados(col, model);
                        ComposeClienteYEmision(col, model);
                        ComposeFormaPago(col, model);
                        ComposeConceptos(col, model);
                        ComposeTotales(col, model);

                        if (options.MostrarAddenda)
                            ComposeAddenda(col, model);

                        ComposeIdCCP(col, model);
                        ComposeComplementoCartaPorte(col, model);
                        ComposeUbicaciones(col, model);
                        ComposeMercancias(col, model, options);
                        ComposeAutotransporte(col, model);
                        ComposeSeguros(col, model);
                        ComposeRemolque(col, model);
                        ComposeFigurasTransporte(col, model);
                        col.Item().Element(c => CfdiPdfSections.ComposeFooterFiscal(c, model));
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(PdfStyleConstants.FontSizeSmall));
                        text.Span("ESTE DOCUMENTO ES UNA REPRESENTACIÓN IMPRESA DE UN CFDI");
                        text.Span("    Página ");
                        text.CurrentPageNumber();
                        text.Span(" de ");
                        text.TotalPages();
                    });
                });

                // Página de Condiciones del Contrato (opcional)
                if (options.MostrarCondicionesContrato)
                {
                    container.Page(page =>
                    {
                        page.Size(pageSize);
                        page.Margin(2, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(PdfStyleConstants.FontSizeDefault).FontFamily(PdfStyleConstants.FontFamily));

                        page.Content().Column(col =>
                        {
                            ComposeCondicionesContrato(col, model);
                        });
                    });
                }
            });

            return document.GeneratePdf();
        }

        private static void ComposeEncabezado(ColumnDescriptor col, CfdiCartaPorteViewModel model)
        {
            // Bloque unificado: la renderización real ocurre en ComposeLogoYCertificados.
            // Se mantiene el método como punto de extensión futuro.
        }

        private void ComposeLogoYCertificados(ColumnDescriptor col, CfdiCartaPorteViewModel model)
        {
            col.Item().BorderBottom(1f).BorderColor(PdfStyleConstants.ColorBorder).PaddingBottom(6)
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
                            if (CfdiPdfSections.TryDecodeLogo(model.LogoBase64, _logger, out var logoBytes))
                                cell.MaxWidth(150).MaxHeight(70).Image(logoBytes!);
                        }
                    });

                // Emisor: nombre + RFC + régimen
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
                        t.Span($"{model.EmisorRegimenFiscal} - {CfdiPdfSections.NombreRegimenFiscal(model.EmisorRegimenFiscal)}").FontSize(PdfStyleConstants.FontSizeLabel).FontColor(PdfStyleConstants.ColorText);
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
                    FiscalRow(c, "PAC QUE TIMBRÓ:", $"{CfdiPdfSections.NombrePac(model.RfcProvCertif)} ({model.RfcProvCertif})");
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

        private static void ComposeClienteYEmision(ColumnDescriptor col, CfdiCartaPorteViewModel model)
        {
            col.Item().PaddingTop(8).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn();
                    c.ConstantColumn(6);
                    c.RelativeColumn();
                });

                // Cliente
                table.Cell().Row(1).Column(1).Border(0.75f).BorderColor(PdfStyleConstants.ColorBorder)
                    .Column(c =>
                    {
                        c.Item().Background(PdfStyleConstants.ColorHeaderBg)
                            .PaddingVertical(3).PaddingHorizontal(6)
                            .Text("CLIENTE").Bold()
                            .FontSize(PdfStyleConstants.FontSizeSectionTitle)
                            .FontColor(PdfStyleConstants.ColorHeaderText);

                        c.Item().Padding(6).Column(cc =>
                        {
                            cc.Item().Text(model.ReceptorNombre ?? "").Bold()
                                .FontSize(PdfStyleConstants.FontSizeDefault)
                                .FontColor(PdfStyleConstants.ColorText);
                            cc.Item().PaddingTop(2).Text(t => { LabelValueSpans(t, "RFC:", model.ReceptorRFC); });
                            cc.Item().Text(t => { LabelValueSpans(t, "Domicilio Fiscal:", model.ReceptorDomicilioFiscal); });
                            cc.Item().Text(t => { LabelValueSpans(t, "Régimen Fiscal:", $"{model.ReceptorRegimenFiscal} - {CfdiPdfSections.NombreRegimenFiscal(model.ReceptorRegimenFiscal)}"); });
                            cc.Item().Text(t => { LabelValueSpans(t, "Uso del CFDI:", $"{model.UsoCFDI} - {CfdiPdfSections.NombreUsoCFDI(model.UsoCFDI)}"); });
                        });
                    });

                // separador
                table.Cell().Row(1).Column(2);

                // Emisión
                table.Cell().Row(1).Column(3).Border(0.75f).BorderColor(PdfStyleConstants.ColorBorder)
                    .Column(c =>
                    {
                        c.Item().Background(PdfStyleConstants.ColorHeaderBg)
                            .PaddingVertical(3).PaddingHorizontal(6)
                            .Text("DATOS DE EMISIÓN").Bold()
                            .FontSize(PdfStyleConstants.FontSizeSectionTitle)
                            .FontColor(PdfStyleConstants.ColorHeaderText);

                        c.Item().Padding(6).Column(cc =>
                        {
                            cc.Item().Text(t => { LabelValueSpans(t, "Fecha y Hora:", model.FechaEmision.ToString("dd/MM/yyyy HH:mm:ss")); });
                            cc.Item().Text(t => { LabelValueSpans(t, "Serie y Folio:", model.Folio); });
                            cc.Item().Text(t => { LabelValueSpans(t, "Moneda:", model.Moneda); });
                            cc.Item().Text(t => { LabelValueSpans(t, "Tipo de Cambio:", model.TipoCambio); });
                            cc.Item().Text(t => { LabelValueSpans(t, "Lugar de Expedición:", model.LugarExpedicion); });
                            cc.Item().Text(t => { LabelValueSpans(t, "Exportación:", $"{model.Exportacion} - {CfdiPdfSections.NombreExportacion(model.Exportacion)}"); });

                            if (!string.IsNullOrWhiteSpace(model.TipoRelacion) || (model.RelacionadosUuids?.Count > 0))
                            {
                                cc.Item().PaddingTop(4).BorderTop(0.5f).BorderColor(PdfStyleConstants.ColorBorderSoft)
                                    .PaddingTop(3).Text("CFDI RELACIONADOS").Bold()
                                    .FontSize(PdfStyleConstants.FontSizeLabel)
                                    .FontColor(PdfStyleConstants.ColorAccent);

                                cc.Item().Text(t => { LabelValueSpans(t, "Tipo Relación:", $"{model.TipoRelacion} - {CfdiPdfSections.NombreTipoRelacion(model.TipoRelacion)}"); });

                                foreach (var uuid in model.RelacionadosUuids ?? Enumerable.Empty<string>())
                                {
                                    cc.Item().Text(t => { LabelValueSpans(t, "UUID:", uuid); });
                                }
                            }
                        });
                    });
            });
        }

        private static void LabelValueSpans(TextDescriptor t, string label, string? value)
        {
            t.Span(label + " ").Bold()
                .FontSize(PdfStyleConstants.FontSizeLabel)
                .FontColor(PdfStyleConstants.ColorText);
            t.Span(value ?? "")
                .FontSize(PdfStyleConstants.FontSizeLabel)
                .FontColor(PdfStyleConstants.ColorSecondaryText);
        }

        private static void ComposeFormaPago(ColumnDescriptor col, CfdiCartaPorteViewModel model)
        {
            col.Item().PaddingTop(6).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn();
                    c.RelativeColumn();
                });

                table.Cell().Row(1).Column(1).PaddingRight(6).Column(c =>
                {
                    c.Item().Text(t => { LabelValueSpans(t, "Forma de Pago:", $"{model.FormaPago} - {CfdiPdfSections.NombreFormaPago(model.FormaPago)}"); });
                    c.Item().Text(t => { LabelValueSpans(t, "Método de Pago:", $"{model.MetodoPago} - {CfdiPdfSections.NombreMetodoPago(model.MetodoPago)}"); });
                });

                table.Cell().Row(1).Column(2).PaddingLeft(6).Column(c =>
                {
                    c.Item().Text(t => { LabelValueSpans(t, "Tipo de Comprobante:", $"{model.TipoComprobante} - {CfdiPdfSections.NombreTipoComprobante(model.TipoComprobante)}"); });
                    c.Item().Text(t => { LabelValueSpans(t, "Condiciones de Pago:", model.CondicionesPago); });
                });
            });
        }

        private static void ComposeConceptos(ColumnDescriptor col, CfdiCartaPorteViewModel model)
        {
            if (model.Conceptos == null || !model.Conceptos.Any()) return;

            col.Item().Element(c => CfdiPdfSections.SectionTitle(c, "Conceptos Facturados"));

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(8);  // ClaveProdServ
                    c.RelativeColumn(6);  // NoIdent
                    c.RelativeColumn(6);  // Cantidad
                    c.RelativeColumn(6);  // ClaveUnidad
                    c.RelativeColumn(6);  // Unidad
                    c.RelativeColumn(22); // Descripcion
                    c.RelativeColumn(8);  // PrecioUnit
                    c.RelativeColumn(7);  // Importe
                    c.RelativeColumn(9);  // Descuento
                    c.RelativeColumn(10); // ObjetoImp
                });

                // Header con fondo oscuro y texto blanco
                var headers = new[] { "Clave Prod/Serv", "No. Ident.", "Cantidad", "Clave Unidad", "Unidad", "Descripción", "Precio Unitario", "Importe", "Descuento", "Objeto Imp." };
                for (uint i = 0; i < headers.Length; i++)
                {
                    table.Cell().Row(1).Column(i + 1)
                        .Element(c => CfdiPdfSections.TableHeaderCell(c, headers[i]));
                }

                bool useZebra = model.Conceptos.Count >= 4;
                uint row = 2;
                foreach (var concepto in model.Conceptos)
                {
                    var r = row;
                    bool zebra = useZebra && (r % 2 == 0);
                    IContainer BodyCell(uint column)
                    {
                        var cell = table.Cell().Row(r).Column(column)
                            .Border(0.3f).BorderColor(PdfStyleConstants.ColorBorderSoft);
                        if (zebra) cell = cell.Background(PdfStyleConstants.ColorZebra);
                        return cell.Padding(2);
                    }

                    BodyCell(1).Text(concepto.ClaveProductoServicio ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                    BodyCell(2).Text(concepto.NumeroIdentificacion ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                    BodyCell(3).AlignCenter().Text(concepto.Cantidad.ToString(CultureInfo.InvariantCulture)).FontSize(PdfStyleConstants.FontSizeSmall);
                    BodyCell(4).Text(CfdiPdfSections.NombreClaveUnidad(concepto.ClaveUnidad)).FontSize(PdfStyleConstants.FontSizeSmall);
                    BodyCell(5).Text(concepto.Unidad ?? "").FontSize(PdfStyleConstants.FontSizeSmall);

                    // Descripción + traslados
                    BodyCell(6).Column(descCol =>
                    {
                        descCol.Item().Text(concepto.Descripcion ?? "").FontSize(PdfStyleConstants.FontSizeSmall);

                        if (concepto.Traslados?.Any() == true)
                        {
                            descCol.Item().PaddingTop(3).Text("IMPUESTOS TRASLADADOS").Bold()
                                .FontSize(PdfStyleConstants.FontSizeVerySmall)
                                .FontColor(PdfStyleConstants.ColorAccent);
                            descCol.Item().Table(impTable =>
                            {
                                impTable.ColumnsDefinition(ic =>
                                {
                                    ic.RelativeColumn(); ic.RelativeColumn(); ic.RelativeColumn(); ic.RelativeColumn(); ic.RelativeColumn();
                                });
                                var impHeaders = new[] { "Factor", "Impuesto", "Tasa/Cuota", "Base", "Importe" };
                                for (uint ih = 0; ih < impHeaders.Length; ih++)
                                    impTable.Cell().Row(1).Column(ih + 1).Padding(1).Text(impHeaders[ih]).Bold().FontSize(PdfStyleConstants.FontSizeVerySmall);

                                uint impRow = 2;
                                foreach (var t in concepto.Traslados)
                                {
                                    var ir = impRow;
                                    impTable.Cell().Row(ir).Column(1).Padding(1).Text(t.TipoFactor ?? "").FontSize(PdfStyleConstants.FontSizeVerySmall);
                                    impTable.Cell().Row(ir).Column(2).Padding(1).Text(CfdiPdfSections.NombreImpuesto(t.Impuesto)).FontSize(PdfStyleConstants.FontSizeVerySmall);
                                    impTable.Cell().Row(ir).Column(3).Padding(1).AlignRight().Text(CfdiPdfSections.FormatTasaOCuota(t.TasaOCuota, t.TipoFactor)).FontSize(PdfStyleConstants.FontSizeVerySmall);
                                    impTable.Cell().Row(ir).Column(4).Padding(1).AlignRight().Text(CfdiPdfSections.Format2(t.Base)).FontSize(PdfStyleConstants.FontSizeVerySmall);
                                    impTable.Cell().Row(ir).Column(5).Padding(1).AlignRight().Text(CfdiPdfSections.Format2(t.Importe)).FontSize(PdfStyleConstants.FontSizeVerySmall);
                                    impRow++;
                                }
                            });
                        }

                        if (concepto.Retenciones?.Any() == true)
                        {
                            descCol.Item().PaddingTop(3).Text("IMPUESTOS RETENIDOS").Bold()
                                .FontSize(PdfStyleConstants.FontSizeVerySmall)
                                .FontColor(PdfStyleConstants.ColorAccent);
                            descCol.Item().Table(retTable =>
                            {
                                retTable.ColumnsDefinition(ic =>
                                {
                                    ic.RelativeColumn(); ic.RelativeColumn(); ic.RelativeColumn(); ic.RelativeColumn(); ic.RelativeColumn();
                                });
                                var retHeaders = new[] { "Factor", "Impuesto", "Tasa/Cuota", "Base", "Importe" };
                                for (uint ih = 0; ih < retHeaders.Length; ih++)
                                    retTable.Cell().Row(1).Column(ih + 1).Padding(1).Text(retHeaders[ih]).Bold().FontSize(PdfStyleConstants.FontSizeVerySmall);

                                uint retRow = 2;
                                foreach (var r in concepto.Retenciones)
                                {
                                    var rr = retRow;
                                    retTable.Cell().Row(rr).Column(1).Padding(1).Text(r.TipoFactor ?? "").FontSize(PdfStyleConstants.FontSizeVerySmall);
                                    retTable.Cell().Row(rr).Column(2).Padding(1).Text(CfdiPdfSections.NombreImpuesto(r.Impuesto)).FontSize(PdfStyleConstants.FontSizeVerySmall);
                                    retTable.Cell().Row(rr).Column(3).Padding(1).AlignRight().Text(CfdiPdfSections.FormatTasaOCuota(r.TasaOCuota, r.TipoFactor)).FontSize(PdfStyleConstants.FontSizeVerySmall);
                                    retTable.Cell().Row(rr).Column(4).Padding(1).AlignRight().Text(CfdiPdfSections.Format2(r.Base)).FontSize(PdfStyleConstants.FontSizeVerySmall);
                                    retTable.Cell().Row(rr).Column(5).Padding(1).AlignRight().Text(CfdiPdfSections.Format2(r.Importe)).FontSize(PdfStyleConstants.FontSizeVerySmall);
                                    retRow++;
                                }
                            });
                        }
                    });

                    BodyCell(7).AlignRight().Text(CfdiPdfSections.Format2(concepto.ValorUnitario)).FontSize(PdfStyleConstants.FontSizeSmall);
                    BodyCell(8).AlignRight().Text(CfdiPdfSections.Format2(concepto.Importe)).FontSize(PdfStyleConstants.FontSizeSmall);
                    BodyCell(9).AlignRight().Text(concepto.Descuento != 0 ? CfdiPdfSections.Format2(concepto.Descuento) : "").FontSize(PdfStyleConstants.FontSizeSmall);
                    BodyCell(10).AlignCenter().Text(CfdiPdfSections.NombreObjetoImp(concepto.ObjetoImpuesto)).FontSize(PdfStyleConstants.FontSizeSmall);

                    row++;
                }
            });
        }

        private static void ComposeTotales(ColumnDescriptor col, CfdiCartaPorteViewModel model)
        {
            // Layout 2 columnas: izquierda "Cantidad con letra"; derecha panel de totales desglosado.
            col.Item().PaddingTop(6).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(6);
                    c.RelativeColumn(4);
                });

                // Columna izquierda: etiqueta + cantidad en letra
                table.Cell().Row(1).Column(1).PaddingRight(8).Column(left =>
                {
                    left.Item().Text("CANTIDAD CON LETRA").Bold().FontSize(PdfStyleConstants.FontSizeSmall);
                    left.Item().PaddingTop(2).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder)
                        .Padding(4).Text(model.CantidadConLetra ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                });

                // Columna derecha: panel de totales
                table.Cell().Row(1).Column(2).Element(c => ComposePanelTotales(c, model));
            });
        }

        private static void ComposePanelTotales(IContainer container, CfdiCartaPorteViewModel model)
        {
            container.Border(0.75f).BorderColor(PdfStyleConstants.ColorBorder).Column(panel =>
            {
                panel.Item().Background(PdfStyleConstants.ColorHeaderBg)
                    .PaddingVertical(3).PaddingHorizontal(6)
                    .Text("RESUMEN DE TOTALES").Bold()
                    .FontSize(PdfStyleConstants.FontSizeSectionTitle)
                    .FontColor(PdfStyleConstants.ColorHeaderText);

                panel.Item().Padding(6).Column(inner =>
                {
                    // Subtotal
                    TotalRow(inner, "SUBTOTAL", CfdiPdfSections.FormatCurrency(model.SubTotal), bold: true);

                    // Impuestos Trasladados
                    if (model.TrasladosResumen.Any() || model.TotalImpuestosTrasladados > 0)
                    {
                        inner.Item().PaddingTop(3).Text("IMPUESTOS TRASLADADOS")
                            .Bold().FontSize(PdfStyleConstants.FontSizeSmall)
                            .FontColor(PdfStyleConstants.ColorAccent);

                        foreach (var t in model.TrasladosResumen)
                        {
                            var etiqueta = $"   {CfdiPdfSections.NombreImpuesto(t.Impuesto)} {CfdiPdfSections.FormatTasaOCuota(t.TasaOCuota, t.TipoFactor)}";
                            TotalRow(inner, etiqueta, CfdiPdfSections.FormatCurrency(t.Importe));
                        }

                        TotalRow(inner, "TOTAL TRASLADADOS", CfdiPdfSections.FormatCurrency(model.TotalImpuestosTrasladados), bold: true);
                    }

                    // Impuestos Retenidos
                    if (model.RetencionesResumen.Any() || model.TotalImpuestosRetenidos > 0)
                    {
                        inner.Item().PaddingTop(3).Text("IMPUESTOS RETENIDOS")
                            .Bold().FontSize(PdfStyleConstants.FontSizeSmall)
                            .FontColor(PdfStyleConstants.ColorAccent);

                        foreach (var r in model.RetencionesResumen)
                        {
                            var etiqueta = $"   {CfdiPdfSections.NombreImpuesto(r.Impuesto)}";
                            TotalRow(inner, etiqueta, CfdiPdfSections.FormatCurrency(r.Importe));
                        }

                        TotalRow(inner, "TOTAL RETENIDOS", CfdiPdfSections.FormatCurrency(model.TotalImpuestosRetenidos), bold: true);
                    }

                    // Total final
                    inner.Item().PaddingTop(4).BorderTop(1.5f).BorderColor(PdfStyleConstants.ColorBorder).PaddingTop(3)
                        .Row(row =>
                        {
                            row.RelativeItem(5).Text("TOTAL").Bold()
                                .FontSize(PdfStyleConstants.FontSizeTitle)
                                .FontColor(PdfStyleConstants.ColorAccent);
                            row.RelativeItem(5).AlignRight().Text(CfdiPdfSections.FormatCurrency(model.Total)).Bold()
                                .FontSize(PdfStyleConstants.FontSizeTitle)
                                .FontColor(PdfStyleConstants.ColorAccent);
                        });
                });
            });
        }

        private static void TotalRow(ColumnDescriptor col, string label, string value, bool bold = false)
        {
            col.Item().Row(row =>
            {
                var labelCell = row.RelativeItem(6).Text(label).FontSize(PdfStyleConstants.FontSizeSmall);
                var valueCell = row.RelativeItem(4).AlignRight().Text(value).FontSize(PdfStyleConstants.FontSizeSmall);
                if (bold)
                {
                    labelCell.Bold();
                    valueCell.Bold();
                }
            });
        }

        private static void ComposeAddenda(ColumnDescriptor col, CfdiCartaPorteViewModel model)
        {
            if (model.Addenda == null) return;

            col.Item().Element(c => CfdiPdfSections.SectionTitle(c, "Addenda Genérica"));

            if (model.Addenda.IsParserGenerico && model.Addenda.Secciones?.Any() == true)
            {
                foreach (var seccion in model.Addenda.Secciones.Where(s => s.Campos.Any(c => !string.IsNullOrWhiteSpace(c.Value))))
                {
                    col.Item().Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder).Table(table =>
                    {
                        table.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(7); });
                        uint r = 1;
                        foreach (var campo in seccion.Campos.Where(c => !string.IsNullOrWhiteSpace(c.Value)))
                        {
                            table.Cell().Row(r).Column(1).Padding(2).Text(campo.Key ?? "").Bold().FontSize(PdfStyleConstants.FontSizeSmall);
                            table.Cell().Row(r).Column(2).Padding(2).Text(campo.Value ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                            r++;
                        }
                    });
                }
            }
            else if (!string.IsNullOrEmpty(model.Addenda.XmlRaw))
            {
                col.Item().Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder)
                    .Padding(3).Text(model.Addenda.XmlRaw).FontSize(PdfStyleConstants.FontSizeVerySmall);
            }
        }

        private static void ComposeIdCCP(ColumnDescriptor col, CfdiCartaPorteViewModel model)
        {
            if (string.IsNullOrEmpty(model.CartaPorte?.IdCCP)) return;

            col.Item().PaddingTop(6).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorderSoft)
                .PaddingVertical(3).PaddingHorizontal(6).AlignCenter().Text(t =>
                {
                    t.Span("ID CCP: ")
                        .Bold().FontSize(PdfStyleConstants.FontSizeSmall)
                        .FontColor(PdfStyleConstants.ColorAccent);
                    t.Span(model.CartaPorte!.IdCCP)
                        .FontSize(PdfStyleConstants.FontSizeSmall)
                        .FontColor(PdfStyleConstants.ColorText);
                });
        }

        private static void ComposeComplementoCartaPorte(ColumnDescriptor col, CfdiCartaPorteViewModel model)
        {
            if (model.CartaPorte == null) return;
            var cp = model.CartaPorte;

            col.Item().Element(c => CfdiPdfSections.SectionTitle(c, "Complemento Carta Porte"));

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn();
                });

                CfdiPdfSections.HeaderValueRow(table, 1, 1, "Versión", cp.Version);
                CfdiPdfSections.HeaderValueRow(table, 1, 3, "Transporte Internacional", cp.TransporteInternacional);
                CfdiPdfSections.HeaderValueRow(table, 1, 5, "Vía de entrada/salida", CfdiPdfSections.NombreCveTransporte(cp.ViaEntradaSalida));

                CfdiPdfSections.HeaderValueRow(table, 2, 1, "Entrada/Salida", cp.EntradaSalidaMercancia);
                CfdiPdfSections.HeaderValueRow(table, 2, 3, "País Origen/Destino", cp.PaisOrigenDestino);
                CfdiPdfSections.HeaderValueRow(table, 2, 5, "Distancia Recorrida", cp.DistanciaRecorrida.ToString(CultureInfo.InvariantCulture));
            });
        }

        private static void ComposeUbicaciones(ColumnDescriptor col, CfdiCartaPorteViewModel model)
        {
            if (model.CartaPorte?.Ubicaciones == null || !model.CartaPorte.Ubicaciones.Any()) return;

            col.Item().Element(c => CfdiPdfSections.SectionTitle(c, "Ubicaciones"));

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(8); c.RelativeColumn(8); c.RelativeColumn(9);
                    c.RelativeColumn(15); c.RelativeColumn(12); c.RelativeColumn(8);
                    c.RelativeColumn(8); c.RelativeColumn(8); c.RelativeColumn(6); c.RelativeColumn(5);
                });

                var headers = new[] { "Tipo", "ID Ubicación", "RFC", "Nombre", "Fecha/Hora", "C.P.", "Municipio", "Localidad", "Estado", "País" };
                for (uint i = 0; i < headers.Length; i++)
                    table.Cell().Row(1).Column(i + 1)
                        .Element(c => CfdiPdfSections.TableHeaderCell(c, headers[i]));

                uint row = 2;
                foreach (var u in model.CartaPorte.Ubicaciones)
                {
                    var r = row;
                    IContainer BCell(uint column) => table.Cell().Row(r).Column(column)
                        .Border(0.3f).BorderColor(PdfStyleConstants.ColorBorderSoft).Padding(2);

                    BCell(1).Text(u.TipoUbicacion ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                    BCell(2).Text(u.IDUbicacion ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                    BCell(3).Text(u.RFCRemitenteDestinatario ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                    BCell(4).Text(u.NombreRemitenteDestinatario ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                    BCell(5).Text(u.FechaHoraSalidaLlegada?.ToString("dd/MM/yyyy HH:mm") ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                    BCell(6).Text(u.CodigoPostal ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                    BCell(7).Text(u.Municipio ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                    BCell(8).Text(u.Localidad ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                    BCell(9).Text(u.Estado ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                    BCell(10).Text(u.Pais ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                    row++;
                }
            });
        }

        private static void ComposeMercancias(ColumnDescriptor col, CfdiCartaPorteViewModel model, CfdiPdfOptions options)
        {
            if (model.CartaPorte?.MercanciasDetalle == null) return;

            if (options.MostrarMercancias)
            {
                col.Item().Element(c => CfdiPdfSections.SectionTitle(c, "Mercancías"));
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(30); c.RelativeColumn(10); c.RelativeColumn(10); c.RelativeColumn(15); c.RelativeColumn(15);
                    });

                    var headers = new[] { "Descripción", "Cantidad", "Clave Unidad", "Peso en KG", "Valor Mercancía" };
                    for (uint i = 0; i < headers.Length; i++)
                        table.Cell().Row(1).Column(i + 1)
                            .Element(c => CfdiPdfSections.TableHeaderCell(c, headers[i]));

                    uint row = 2;
                    foreach (var m in model.CartaPorte.MercanciasDetalle)
                    {
                        var r = row;
                        IContainer BCell(uint column) => table.Cell().Row(r).Column(column)
                            .Border(0.3f).BorderColor(PdfStyleConstants.ColorBorderSoft).Padding(2);

                        BCell(1).Text(m.Descripcion ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                        BCell(2).AlignRight().Text(CfdiPdfSections.Format6(m.Cantidad)).FontSize(PdfStyleConstants.FontSizeSmall);
                        BCell(3).Text(CfdiPdfSections.NombreClaveUnidad(m.ClaveUnidad)).FontSize(PdfStyleConstants.FontSizeSmall);
                        BCell(4).AlignRight().Text(CfdiPdfSections.Format6(m.PesoEnKg)).FontSize(PdfStyleConstants.FontSizeSmall);
                        BCell(5).AlignRight().Text(CfdiPdfSections.Format6(m.ValorMercancia)).FontSize(PdfStyleConstants.FontSizeSmall);
                        row++;
                    }
                });
            }
            else
            {
                col.Item().Element(c => CfdiPdfSections.SectionTitle(c, "Resumen de Mercancías"));
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });

                    var headers = new[] { "No. Total Mercancías", "Peso Bruto Total (KG)", "Unidad de Peso" };
                    for (uint i = 0; i < headers.Length; i++)
                        table.Cell().Row(1).Column(i + 1)
                            .Element(c => CfdiPdfSections.TableHeaderCell(c, headers[i]));

                    IContainer SCell(uint column) => table.Cell().Row(2).Column(column)
                        .Border(0.3f).BorderColor(PdfStyleConstants.ColorBorderSoft).Padding(3);
                    SCell(1).AlignCenter().Text(model.CartaPorte.NumeroTotalMercancias.ToString()).FontSize(PdfStyleConstants.FontSizeDefault);
                    SCell(2).AlignRight().Text(CfdiPdfSections.Format6(model.CartaPorte.PesoBrutoTotal)).FontSize(PdfStyleConstants.FontSizeDefault);
                    SCell(3).AlignCenter().Text(model.CartaPorte.UnidadPeso ?? "").FontSize(PdfStyleConstants.FontSizeDefault);
                });
            }
        }

        private static void ComposeAutotransporte(ColumnDescriptor col, CfdiCartaPorteViewModel model)
        {
            if (model.CartaPorte?.Autotransporte == null) return;
            var at = model.CartaPorte.Autotransporte;

            col.Item().Element(c => CfdiPdfSections.SectionTitle(c, "Datos de Autotransporte"));
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });

                CfdiPdfSections.HeaderValueRow(table, 1, 1, "Permiso SCT", CfdiPdfSections.NombrePermisoSCT(at.PermisoSCT));
                CfdiPdfSections.HeaderValueRow(table, 1, 3, "Número Permiso SCT", at.NumeroPermisoSCT);
                CfdiPdfSections.HeaderValueRow(table, 2, 1, "Configuración Vehicular", CfdiPdfSections.NombreConfigVehicular(at.ConfigVehicular));
                CfdiPdfSections.HeaderValueRow(table, 2, 3, "Peso Bruto Vehicular", at.PesoBrutoVehicular.ToString(CultureInfo.InvariantCulture));
                CfdiPdfSections.HeaderValueRow(table, 3, 1, "Placa Vehículo", at.PlacaVM);
                CfdiPdfSections.HeaderValueRow(table, 3, 3, "Año Modelo Vehículo", at.AnioModeloVM.ToString());
            });
        }

        private static void ComposeSeguros(ColumnDescriptor col, CfdiCartaPorteViewModel model)
        {
            var seg = model.CartaPorte?.Seguro;
            if (seg == null) return;
            if (string.IsNullOrEmpty(seg.AseguradoraResponsabilidadCivil) &&
                string.IsNullOrEmpty(seg.AseguradoraCarga) &&
                string.IsNullOrEmpty(seg.AseguradoraMedAmbiente)) return;

            col.Item().Element(c => CfdiPdfSections.SectionTitle(c, "Datos del Seguro"));
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });
                uint r = 1;
                if (!string.IsNullOrEmpty(seg.AseguradoraResponsabilidadCivil))
                {
                    CfdiPdfSections.HeaderValueRow(table, r, 1, "Aseg. Resp. Civil", seg.AseguradoraResponsabilidadCivil);
                    CfdiPdfSections.HeaderValueRow(table, r, 3, "Póliza Resp. Civil", seg.PolizaResponsabilidadCivil);
                    r++;
                }
                if (!string.IsNullOrEmpty(seg.AseguradoraCarga))
                {
                    CfdiPdfSections.HeaderValueRow(table, r, 1, "Aseg. Carga", seg.AseguradoraCarga);
                    CfdiPdfSections.HeaderValueRow(table, r, 3, "Póliza Carga", seg.PolizaCarga);
                    r++;
                }
                if (!string.IsNullOrEmpty(seg.AseguradoraMedAmbiente))
                {
                    CfdiPdfSections.HeaderValueRow(table, r, 1, "Aseg. Medio Ambiente", seg.AseguradoraMedAmbiente);
                    CfdiPdfSections.HeaderValueRow(table, r, 3, "Póliza Medio Ambiente", seg.PolizaMedAmbiente);
                }
            });
        }

        private static void ComposeRemolque(ColumnDescriptor col, CfdiCartaPorteViewModel model)
        {
            if (model.CartaPorte?.Remolque == null) return;

            col.Item().Element(c => CfdiPdfSections.SectionTitle(c, "Datos del Remolque"));
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });
                CfdiPdfSections.HeaderValueRow(table, 1, 1, "SubTipo Remolque", CfdiPdfSections.NombreSubTipoRemolque(model.CartaPorte.Remolque.SubTipoRemolque));
                CfdiPdfSections.HeaderValueRow(table, 1, 3, "Placa", model.CartaPorte.Remolque.Placa);
            });
        }

        private static void ComposeFigurasTransporte(ColumnDescriptor col, CfdiCartaPorteViewModel model)
        {
            if (model.CartaPorte?.FigurasTransporte == null || !model.CartaPorte.FigurasTransporte.Any()) return;

            col.Item().Element(c => CfdiPdfSections.SectionTitle(c, "Figuras de Transporte"));
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });

                var headers = new[] { "Tipo Figura", "RFC Figura", "Nombre Figura", "Licencia" };
                for (uint i = 0; i < headers.Length; i++)
                    table.Cell().Row(1).Column(i + 1)
                        .Element(c => CfdiPdfSections.TableHeaderCell(c, headers[i]));

                uint row = 2;
                foreach (var f in model.CartaPorte.FigurasTransporte)
                {
                    var r = row;
                    IContainer BCell(uint column) => table.Cell().Row(r).Column(column)
                        .Border(0.3f).BorderColor(PdfStyleConstants.ColorBorderSoft).Padding(3);

                    BCell(1).Text(CfdiPdfSections.NombreTipoFigura(f.TipoFigura)).FontSize(PdfStyleConstants.FontSizeSmall);
                    BCell(2).Text(f.RFCFigura ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                    BCell(3).Text(f.NombreFigura ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                    BCell(4).Text(f.NumeroLicencia ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                    row++;
                }
            });
        }

        private static void ComposeCondicionesContrato(ColumnDescriptor col, CfdiCartaPorteViewModel model)
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span("RFC Emisor: ").Bold()
                        .FontSize(PdfStyleConstants.FontSizeLabel)
                        .FontColor(PdfStyleConstants.ColorAccent);
                    t.Span(model.EmisorRFC ?? "")
                        .FontSize(PdfStyleConstants.FontSizeLabel)
                        .FontColor(PdfStyleConstants.ColorText);
                });
                row.RelativeItem().AlignRight().Text(t =>
                {
                    t.Span("Folio Fiscal: ").Bold()
                        .FontSize(PdfStyleConstants.FontSizeLabel)
                        .FontColor(PdfStyleConstants.ColorAccent);
                    t.Span(model.UUID ?? "")
                        .FontSize(PdfStyleConstants.FontSizeLabel)
                        .FontColor(PdfStyleConstants.ColorText);
                });
            });

            col.Item().PaddingTop(8)
                .Background(PdfStyleConstants.ColorHeaderBg)
                .PaddingVertical(5).PaddingHorizontal(6)
                .AlignCenter()
                .Text("CONDICIONES DEL CONTRATO DE TRANSPORTE QUE AMPARA ESTA CARTA PORTE")
                .Bold()
                .FontSize(PdfStyleConstants.FontSizeSectionTitle)
                .FontColor(PdfStyleConstants.ColorHeaderText);

            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().PaddingRight(6).Text(TextoCondicionesCol1)
                    .FontSize(PdfStyleConstants.FontSizeSmall)
                    .FontColor(PdfStyleConstants.ColorSecondaryText)
                    .LineHeight(1.35f);
                row.RelativeItem().PaddingLeft(6).Text(TextoCondicionesCol2)
                    .FontSize(PdfStyleConstants.FontSizeSmall)
                    .FontColor(PdfStyleConstants.ColorSecondaryText)
                    .LineHeight(1.35f);
            });
        }

        private const string TextoCondicionesCol1 =
            "PRIMERA.- Para los efectos del presente contrato de transporte se denomina \"Transportista\" al que realiza el servicio de transportación y \"Expedidor\", \"Remitente\" o \"Usuario\" al usuario que contrate el servicio o remite la mercancía.\n" +
            "SEGUNDA.- El \"Expedidor\", \"Remitente\" o \"Usuario\" es responsable de que la información proporcionada al \"Transportista\" sea veraz y que la documentación que entregue para efectos del transporte sea la correcta.\n" +
            "TERCERA.- El \"Expedidor\", \"Remitente\" o \"Usuario\" debe declarar al \"Transportista\" el tipo de mercancía o efectos de que se trate, peso, medidas y/o número de la carga que entrega para su transporte y, en su caso, el valor de la misma. La carga que se entregue a granel podrá ser aforada en metros cúbicos con la conformidad del \"Expedidor\", \"Remitente\" o \"Usuario\".\n" +
            "CUARTA.- Para efectos del transporte, el \"Expedidor\", \"Remitente\" o \"Usuario\" deberá entregar al \"Transportista\" los documentos que las leyes y reglamentos exijan para llevar a cabo el servicio, en caso de no cumplirse con estos requisitos el \"Transportista\" está obligado a rehusar el transporte de las mercancías.\n" +
            "QUINTA.- Si por sospecha de falsedad en la declaración del contenido de un bulto el \"Transportista\" deseare proceder a su reconocimiento, podrá hacerlo ante testigos y con asistencia del \"Expedidor\", \"Remitente\" o \"Usuario\" o del consignatario. Si este último no concurriere, se solicitará la presencia de un inspector de la Secretaría de Comunicaciones y Transportes, y se levantará el acta correspondiente. El \"Transportista\" tendrá en todo caso, la obligación de dejar los bultos en el estado en que se encontraban antes del reconocimiento.\n" +
            "SEXTA.- El \"Transportista\" deberá recoger y entregar la carga precisamente en los domicilios que señale el \"Expedidor\", \"Remitente\" o \"Usuario\", ajustándose a los términos y condiciones convenidos. El \"Transportista\" sólo está obligado a llevar la carga al domicilio del consignatario para su entrega una sola vez. Si ésta no fuera recibida, se dejará aviso de que la mercancía queda a disposición del interesado en las bodegas que indique el \"Transportista\".\n" +
            "SÉPTIMA.- Si la carga no fuere retirada dentro de los 30 días hábiles siguientes a aquél en que hubiere sido puesta a disposición del consignatario, el \"Transportista\" podrá solicitar la venta en subasta pública con arreglo a lo que dispone el Código de Comercio.\n" +
            "OCTAVA.- El \"Transportista\" y el \"Expedidor\", \"Remitente\" o \"Usuario\" negociarán libremente el precio del servicio, tomando en cuenta su tipo, característica de los embarques, volumen, regularidad, clase de carga y sistema de pago.\n" +
            "NOVENA.- Si el \"Expedidor\", \"Remitente\" o \"Usuario\" desea que el \"Transportista\" asuma la responsabilidad por el valor de las mercancías o efectos que él declare y que cubra toda clase de riesgos, inclusive los derivados de caso fortuito o de fuerza mayor, las partes deberán convenir un cargo adicional, equivalente al valor de la prima del seguro que se contrate, el cual se deberá expresar en un CFDI con Complemento Carta Porte.";

        private const string TextoCondicionesCol2 =
            "DÉCIMA.- Cuando el importe del flete no incluya el cargo adicional, la responsabilidad del \"Transportista\" queda expresamente limitada a la cantidad equivalente a 15 Unidades de Medida y Actualización (UMAS) por tonelada o cuando se trate de embarques cuyo peso sea mayor de 200 kg, pero menor de 1000 kg; y 4 UMAS por remesa cuando se trate de embarques con peso hasta de 200 kg.\n" +
            "DÉCIMA PRIMERA.- El precio del transporte deberá pagarse en origen, salvo convenio entre las partes de pago en destino. Cuando el transporte se hubiere concertado \"Flete por Cobrar\", la entrega de las mercancías o efectos se hará contra el pago del flete y el \"Transportista\" tendrá derecho a retenerlos mientras no se le cubra el precio convenido.\n" +
            "DÉCIMA SEGUNDA.- Si al momento de la entrega resultare algún faltante o avería, el consignatario podrá formular su reclamación por escrito al \"Transportista\", dentro de las 24 horas siguientes.\n" +
            "DÉCIMA TERCERA.- El \"Transportista\" queda eximido de la obligación de recibir mercancías o efectos para su transporte, en los siguientes casos:\n" +
            "a) Cuando se trate de carga que por su naturaleza, peso, volumen, embalaje defectuoso o cualquier otra circunstancia no pueda transportarse sin destruirse o sin causar daño a los demás artículos o al material rodante, salvo que la empresa de que se trate tenga el equipo adecuado.\n" +
            "b) Las mercancías cuyo transporte haya sido prohibido por disposiciones legales o reglamentarias. Cuando tales disposiciones no prohíban precisamente el transporte de determinadas mercancías, pero sí ordenen la presentación de ciertos documentos para que puedan ser transportadas, el \"Expedidor\", \"Remitente\" o \"Usuario\" estará obligado a entregar al \"Transportista\" los documentos correspondientes.\n" +
            "DÉCIMA CUARTA.- Los casos no previstos en las presentes condiciones y las quejas derivadas de su aplicación se someterán por la vía administrativa a la Secretaría de Comunicaciones y Transportes.\n" +
            "DÉCIMA QUINTA.- Para el caso de que el \"Expedidor\", \"Remitente\" o \"Usuario\" contrate carro por entero, éste aceptará la responsabilidad solidaria para con el \"Transportista\" mediante la figura de la corresponsabilidad que contempla el artículo 10 del Reglamento Sobre el Peso, Dimensiones y Capacidad de los Vehículos de Autotransporte que Transitan en los Caminos y Puentes de Jurisdicción Federal, por lo que el \"Expedidor\", \"Remitente\" o \"Usuario\" queda obligado a verificar que la carga y el vehículo que la transporta cumplan con el peso y dimensiones máximas establecidos en la NOM-012-SCT-2-2017, o la que la sustituya.\n" +
            "Para el caso de incumplimiento e inobservancia a las disposiciones que regulan el peso y dimensiones, por parte del \"Expedidor\", \"Remitente\" o \"Usuario\", éste será corresponsable de las infracciones y multas que la Secretaría de Infraestructura, Comunicaciones y Transportes o la Guardia Nacional impongan al \"Transportista\", por cargar las unidades con exceso de peso.";

    }
}
