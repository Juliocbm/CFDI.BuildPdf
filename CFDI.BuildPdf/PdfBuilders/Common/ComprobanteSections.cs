using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CFDI.BuildPdf.Catalogs;
using CFDI.BuildPdf.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace CFDI.BuildPdf.PdfBuilders.Common
{
    /// <summary>
    /// Secciones genéricas de un comprobante (cliente/emisión, forma de pago, conceptos
    /// y totales) reutilizadas entre los builders de Carta Porte y Factura.
    /// </summary>
    internal static class ComprobanteSections
    {
        internal static void LabelValueSpans(TextDescriptor t, string label, string? value)
        {
            t.Span(label + " ").Bold()
                .FontSize(PdfStyleConstants.FontSizeLabel)
                .FontColor(PdfStyleConstants.ColorText);
            t.Span(value ?? "")
                .FontSize(PdfStyleConstants.FontSizeLabel)
                .FontColor(PdfStyleConstants.ColorSecondaryText);
        }

        public static void ComposeClienteYEmision(ColumnDescriptor col, CfdiViewModelBase model)
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
                            cc.Item().Text(t => { LabelValueSpans(t, "Régimen Fiscal:", $"{model.ReceptorRegimenFiscal} - {SatCatalogos.NombreRegimenFiscal(model.ReceptorRegimenFiscal)}"); });
                            cc.Item().Text(t => { LabelValueSpans(t, "Uso del CFDI:", $"{model.UsoCFDI} - {SatCatalogos.NombreUsoCFDI(model.UsoCFDI)}"); });
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
                            cc.Item().Text(t => { LabelValueSpans(t, "Exportación:", $"{model.Exportacion} - {SatCatalogos.NombreExportacion(model.Exportacion)}"); });

                            if (!string.IsNullOrWhiteSpace(model.TipoRelacion) || (model.RelacionadosUuids?.Count > 0))
                            {
                                cc.Item().PaddingTop(4).BorderTop(0.5f).BorderColor(PdfStyleConstants.ColorBorderSoft)
                                    .PaddingTop(3).Text("CFDI RELACIONADOS").Bold()
                                    .FontSize(PdfStyleConstants.FontSizeLabel)
                                    .FontColor(PdfStyleConstants.ColorAccent);

                                cc.Item().Text(t => { LabelValueSpans(t, "Tipo Relación:", $"{model.TipoRelacion} - {SatCatalogos.NombreTipoRelacion(model.TipoRelacion)}"); });

                                foreach (var uuid in model.RelacionadosUuids ?? Enumerable.Empty<string>())
                                {
                                    cc.Item().Text(t => { LabelValueSpans(t, "UUID:", uuid); });
                                }
                            }
                        });
                    });
            });
        }

        public static void ComposeFormaPago(ColumnDescriptor col, CfdiViewModelBase model, string? condicionesPago)
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
                    c.Item().Text(t => { LabelValueSpans(t, "Forma de Pago:", $"{model.FormaPago} - {SatCatalogos.NombreFormaPago(model.FormaPago)}"); });
                    c.Item().Text(t => { LabelValueSpans(t, "Método de Pago:", $"{model.MetodoPago} - {SatCatalogos.NombreMetodoPago(model.MetodoPago)}"); });
                });

                table.Cell().Row(1).Column(2).PaddingLeft(6).Column(c =>
                {
                    c.Item().Text(t => { LabelValueSpans(t, "Tipo de Comprobante:", $"{model.TipoComprobante} - {SatCatalogos.NombreTipoComprobante(model.TipoComprobante)}"); });
                    c.Item().Text(t => { LabelValueSpans(t, "Condiciones de Pago:", condicionesPago); });
                });
            });
        }

        public static void ComposeConceptos(ColumnDescriptor col, IReadOnlyList<ConceptoViewModel> conceptos)
        {
            if (conceptos == null || !conceptos.Any()) return;

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

                bool useZebra = conceptos.Count >= 4;
                uint row = 2;
                foreach (var concepto in conceptos)
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
                    BodyCell(4).Text(SatCatalogos.NombreClaveUnidad(concepto.ClaveUnidad)).FontSize(PdfStyleConstants.FontSizeSmall);
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
                                    impTable.Cell().Row(ir).Column(2).Padding(1).Text(SatCatalogos.NombreImpuesto(t.Impuesto)).FontSize(PdfStyleConstants.FontSizeVerySmall);
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
                                    retTable.Cell().Row(rr).Column(2).Padding(1).Text(SatCatalogos.NombreImpuesto(r.Impuesto)).FontSize(PdfStyleConstants.FontSizeVerySmall);
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
                    BodyCell(10).AlignCenter().Text(SatCatalogos.NombreObjetoImp(concepto.ObjetoImpuesto)).FontSize(PdfStyleConstants.FontSizeSmall);

                    row++;
                }
            });
        }

        public static void ComposeTotales(ColumnDescriptor col, CfdiViewModelBase model, IReadOnlyList<ImpuestoConceptoViewModel> trasladosResumen, IReadOnlyList<RetencionImpuestoViewModel> retencionesResumen, decimal totalTrasladados, decimal totalRetenidos)
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
                table.Cell().Row(1).Column(2).Element(c => ComposePanelTotales(c, model, trasladosResumen, retencionesResumen, totalTrasladados, totalRetenidos));
            });
        }

        private static void ComposePanelTotales(IContainer container, CfdiViewModelBase model, IReadOnlyList<ImpuestoConceptoViewModel> trasladosResumen, IReadOnlyList<RetencionImpuestoViewModel> retencionesResumen, decimal totalTrasladados, decimal totalRetenidos)
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
                    if (trasladosResumen.Any() || totalTrasladados > 0)
                    {
                        inner.Item().PaddingTop(3).Text("IMPUESTOS TRASLADADOS")
                            .Bold().FontSize(PdfStyleConstants.FontSizeSmall)
                            .FontColor(PdfStyleConstants.ColorAccent);

                        foreach (var t in trasladosResumen)
                        {
                            var etiqueta = $"   {SatCatalogos.NombreImpuesto(t.Impuesto)} {CfdiPdfSections.FormatTasaOCuota(t.TasaOCuota, t.TipoFactor)}";
                            TotalRow(inner, etiqueta, CfdiPdfSections.FormatCurrency(t.Importe));
                        }

                        TotalRow(inner, "TOTAL TRASLADADOS", CfdiPdfSections.FormatCurrency(totalTrasladados), bold: true);
                    }

                    // Impuestos Retenidos
                    if (retencionesResumen.Any() || totalRetenidos > 0)
                    {
                        inner.Item().PaddingTop(3).Text("IMPUESTOS RETENIDOS")
                            .Bold().FontSize(PdfStyleConstants.FontSizeSmall)
                            .FontColor(PdfStyleConstants.ColorAccent);

                        foreach (var r in retencionesResumen)
                        {
                            var etiqueta = $"   {SatCatalogos.NombreImpuesto(r.Impuesto)}";
                            TotalRow(inner, etiqueta, CfdiPdfSections.FormatCurrency(r.Importe));
                        }

                        TotalRow(inner, "TOTAL RETENIDOS", CfdiPdfSections.FormatCurrency(totalRetenidos), bold: true);
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
    }
}
