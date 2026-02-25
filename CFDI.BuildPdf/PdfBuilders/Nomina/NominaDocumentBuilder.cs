using System;
using System.Globalization;
using System.Linq;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Models;
using CFDI.BuildPdf.PdfBuilders.Common;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CFDI.BuildPdf.PdfBuilders.Nomina
{
    /// <summary>
    /// Construye el PDF de CFDI con complemento Nómina 1.2 usando QuestPDF.
    /// Replica el layout de TemplateFacturaNomina.cshtml.
    /// </summary>
    internal class NominaDocumentBuilder : IPdfDocumentBuilder<CfdiNominaViewModel>
    {
        private static readonly CultureInfo MxCulture = CultureInfo.GetCultureInfo("es-MX");

        /// <inheritdoc />
        public byte[] Build(CfdiNominaViewModel model, CfdiPdfOptions options)
        {
            var pageSize = options.Orientacion == PdfOrientation.Landscape
                ? PageSizes.Letter.Landscape()
                : PageSizes.Letter;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(pageSize);
                    page.MarginTop(1, Unit.Centimetre);
                    page.MarginBottom(1.2f, Unit.Centimetre);
                    page.MarginHorizontal(1, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(PdfStyleConstants.FontSizeDefault).FontFamily(PdfStyleConstants.FontFamily));

                    page.Content().Column(col =>
                    {
                        ComposeEncabezado(col, model);
                        ComposeDatosComprobante(col, model);
                        ComposeDatosEmpleado(col, model);
                        ComposeDetallesNomina(col, model);
                        ComposeConceptoPrincipal(col, model);
                        ComposePercepciones(col, model);
                        ComposeDeducciones(col, model);
                        ComposeOtrosPagos(col, model);
                        ComposeIncapacidades(col, model);
                        ComposeTotalesGenerales(col, model);
                        col.Item().Element(c => CfdiPdfSections.ComposeFooterFiscal(c, model));
                        col.Item().PaddingTop(10).AlignCenter()
                            .Text("Este documento es una representación impresa de un Comprobante Fiscal Digital por Internet de Nómina.")
                            .FontSize(PdfStyleConstants.FontSizeSmall);
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(PdfStyleConstants.FontSizeSmall));
                        text.Span("Página ");
                        text.CurrentPageNumber();
                        text.Span(" de ");
                        text.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        private static void ComposeEncabezado(ColumnDescriptor col, CfdiNominaViewModel model)
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(7); });

                // Logo
                table.Cell().Row(1).Column(1).Element(cell =>
                {
                    if (!string.IsNullOrEmpty(model.LogoBase64))
                    {
                        try
                        {
                            var logoBytes = Convert.FromBase64String(model.LogoBase64);
                            cell.Width(180).Image(logoBytes);
                        }
                        catch { /* Logo inválido, se omite */ }
                    }
                });

                // Datos emisor
                table.Cell().Row(1).Column(2).AlignRight().Column(c =>
                {
                    c.Item().AlignRight().Text(model.EmisorNombre ?? "").Bold();
                    c.Item().AlignRight().Text($"RFC: {model.EmisorRFC}");
                    c.Item().AlignRight().Text($"Régimen Fiscal: {model.EmisorRegimenFiscal}");
                    c.Item().AlignRight().Text($"Lugar de Expedición: {model.LugarExpedicion}");
                });
            });
        }

        private static void ComposeDatosComprobante(ColumnDescriptor col, CfdiNominaViewModel model)
        {
            col.Item().Element(c => CfdiPdfSections.SectionTitle(c, "Datos del Comprobante de Nómina"));

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });

                CfdiPdfSections.HeaderValueRow(table, 1, 1, "Folio Fiscal (UUID)", model.UUID);
                CfdiPdfSections.HeaderValueRow(table, 1, 3, "Fecha y Hora de Emisión", model.FechaEmision.ToString("yyyy-MM-dd HH:mm:ss"));
                CfdiPdfSections.HeaderValueRow(table, 2, 1, "Fecha y Hora de Certificación", model.FechaCertificacion.ToString("yyyy-MM-dd HH:mm:ss"));
                CfdiPdfSections.HeaderValueRow(table, 2, 3, "No. Certificado Emisor", model.NoCertificadoEmisor);
                CfdiPdfSections.HeaderValueRow(table, 3, 1, "No. Certificado SAT", model.NoCertificadoSAT);
                CfdiPdfSections.HeaderValueRow(table, 3, 3, "Versión CFDI", model.Version);

                var serieFolio = (!string.IsNullOrEmpty(model.Serie) ? model.Serie + "-" : "") + model.Folio;
                CfdiPdfSections.HeaderValueRow(table, 4, 1, "Serie y Folio", serieFolio);
                var tipoDesc = model.TipoComprobante == "N" ? "Nómina" : model.TipoComprobante;
                CfdiPdfSections.HeaderValueRow(table, 4, 3, "Tipo de Comprobante", $"{model.TipoComprobante} ({tipoDesc})");
            });
        }

        private static void ComposeDatosEmpleado(ColumnDescriptor col, CfdiNominaViewModel model)
        {
            col.Item().Element(c => CfdiPdfSections.SectionTitle(c, "Datos del Empleado"));

            var receptor = model.Nomina?.Receptor;

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });

                // Nombre en fila completa
                table.Cell().Row(1).Column(1)
                    .Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder)
                    .Background(PdfStyleConstants.ColorHeaderBg)
                    .Padding(3).Text("Nombre").Bold();
                table.Cell().Row(1).Column(2).ColumnSpan(3)
                    .Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder)
                    .Padding(3).Text(model.ReceptorNombre ?? "");

                CfdiPdfSections.HeaderValueRow(table, 2, 1, "RFC", model.ReceptorRFC);
                CfdiPdfSections.HeaderValueRow(table, 2, 3, "CURP", receptor?.Curp);
                CfdiPdfSections.HeaderValueRow(table, 3, 1, "Régimen Fiscal Receptor", model.ReceptorRegimenFiscal);
                CfdiPdfSections.HeaderValueRow(table, 3, 3, "Uso CFDI", model.UsoCFDI);
                CfdiPdfSections.HeaderValueRow(table, 4, 1, "No. Empleado", receptor?.NumEmpleado);
                CfdiPdfSections.HeaderValueRow(table, 4, 3, "No. Seguridad Social", receptor?.NumSeguridadSocial);
                CfdiPdfSections.HeaderValueRow(table, 5, 1, "Fecha Inicio Rel. Laboral", receptor?.FechaInicioRelLaboral?.ToString("yyyy-MM-dd"));
                CfdiPdfSections.HeaderValueRow(table, 5, 3, "Antigüedad", receptor?.Antiguedad);
                CfdiPdfSections.HeaderValueRow(table, 6, 1, "Tipo Contrato", receptor?.TipoContrato);
                CfdiPdfSections.HeaderValueRow(table, 6, 3, "Tipo Régimen", receptor?.TipoRegimen);
                CfdiPdfSections.HeaderValueRow(table, 7, 1, "Periodicidad Pago", receptor?.PeriodicidadPago);
                CfdiPdfSections.HeaderValueRow(table, 7, 3, "Puesto", receptor?.Puesto);
                CfdiPdfSections.HeaderValueRow(table, 8, 1, "Salario Diario Integrado", receptor?.SalarioDiarioIntegrado?.ToString("C", MxCulture));
                CfdiPdfSections.HeaderValueRow(table, 8, 3, "Riesgo Puesto", receptor?.RiesgoPuesto);
                CfdiPdfSections.HeaderValueRow(table, 9, 1, "Salario Base Cot. Apor.", receptor?.SalarioBaseCotApor?.ToString("C", MxCulture));
                CfdiPdfSections.HeaderValueRow(table, 9, 3, "Clave Entidad Federativa", receptor?.ClaveEntFed);
            });
        }

        private static void ComposeDetallesNomina(ColumnDescriptor col, CfdiNominaViewModel model)
        {
            if (model.Nomina == null) return;
            var nom = model.Nomina;

            col.Item().Element(c => CfdiPdfSections.SectionTitle(c, "Detalles del Pago de Nómina"));

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });

                CfdiPdfSections.HeaderValueRow(table, 1, 1, "Versión Complemento", nom.Version);
                var tipoNom = nom.TipoNomina == "O" ? "Ordinaria" : "Extraordinaria";
                CfdiPdfSections.HeaderValueRow(table, 1, 3, "Tipo Nómina", $"{nom.TipoNomina} ({tipoNom})");
                CfdiPdfSections.HeaderValueRow(table, 2, 1, "Fecha Pago", nom.FechaPago.ToString("yyyy-MM-dd"));
                CfdiPdfSections.HeaderValueRow(table, 2, 3, "Fecha Inicial Pago", nom.FechaInicialPago.ToString("yyyy-MM-dd"));
                CfdiPdfSections.HeaderValueRow(table, 3, 1, "Fecha Final Pago", nom.FechaFinalPago.ToString("yyyy-MM-dd"));
                CfdiPdfSections.HeaderValueRow(table, 3, 3, "Num Días Pagados", nom.NumDiasPagados.ToString("N2", CultureInfo.InvariantCulture));

                if (nom.Emisor?.RegistroPatronal != null)
                {
                    table.Cell().Row(4).Column(1)
                        .Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder)
                        .Background(PdfStyleConstants.ColorHeaderBg)
                        .Padding(3).Text("Registro Patronal Emisor").Bold();
                    table.Cell().Row(4).Column(2).ColumnSpan(3)
                        .Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder)
                        .Padding(3).Text(nom.Emisor.RegistroPatronal);
                }
            });
        }

        private static void ComposeConceptoPrincipal(ColumnDescriptor col, CfdiNominaViewModel model)
        {
            if (model.Conceptos == null || !model.Conceptos.Any()) return;

            col.Item().Element(c => CfdiPdfSections.SectionTitle(c, "Concepto Principal del Comprobante"));

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(8); c.RelativeColumn(6); c.RelativeColumn(4);
                    c.RelativeColumn(6); c.RelativeColumn(6); c.RelativeColumn(25);
                    c.RelativeColumn(10); c.RelativeColumn(10); c.RelativeColumn(10);
                });

                var headers = new[] { "Clave Prod/Serv", "No. Ident.", "Cant.", "Clave Unidad", "Unidad", "Descripción", "Valor Unitario", "Importe", "Descuento" };
                for (uint i = 0; i < headers.Length; i++)
                    table.Cell().Row(1).Column(i + 1).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder)
                        .Background(PdfStyleConstants.ColorHeaderBg).Padding(2).Text(headers[i]).Bold().FontSize(PdfStyleConstants.FontSizeSmall);

                uint row = 2;
                foreach (var c in model.Conceptos)
                {
                    var r = row;
                    table.Cell().Row(r).Column(1).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder).Padding(2).Text(c.ClaveProductoServicio ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                    table.Cell().Row(r).Column(2).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder).Padding(2).Text(c.NumeroIdentificacion ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                    table.Cell().Row(r).Column(3).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder).Padding(2).AlignRight().Text(c.Cantidad.ToString("N0", CultureInfo.InvariantCulture)).FontSize(PdfStyleConstants.FontSizeSmall);
                    table.Cell().Row(r).Column(4).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder).Padding(2).Text(c.ClaveUnidad ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                    table.Cell().Row(r).Column(5).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder).Padding(2).Text(c.Unidad ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                    table.Cell().Row(r).Column(6).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder).Padding(2).Text(c.Descripcion ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                    table.Cell().Row(r).Column(7).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder).Padding(2).AlignRight().Text(c.ValorUnitario.ToString("C", MxCulture)).FontSize(PdfStyleConstants.FontSizeSmall);
                    table.Cell().Row(r).Column(8).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder).Padding(2).AlignRight().Text(c.Importe.ToString("C", MxCulture)).FontSize(PdfStyleConstants.FontSizeSmall);
                    table.Cell().Row(r).Column(9).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder).Padding(2).AlignRight().Text(c.Descuento.ToString("C", MxCulture)).FontSize(PdfStyleConstants.FontSizeSmall);
                    row++;
                }
            });
        }

        private static void ComposePercepciones(ColumnDescriptor col, CfdiNominaViewModel model)
        {
            var perc = model.Nomina?.Percepciones;
            if (perc?.PercepcionesDetalle?.Any() != true) return;

            col.Item().Element(c => CfdiPdfSections.SectionTitle(c, "Percepciones"));

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(8); c.RelativeColumn(8); c.RelativeColumn(30);
                    c.RelativeColumn(15); c.RelativeColumn(15);
                });

                var headers = new[] { "Tipo", "Clave", "Concepto", "Importe Gravado", "Importe Exento" };
                for (uint i = 0; i < headers.Length; i++)
                    table.Cell().Row(1).Column(i + 1).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder)
                        .Background(PdfStyleConstants.ColorHeaderBg).Padding(2).Text(headers[i]).Bold().FontSize(PdfStyleConstants.FontSizeSmall);

                uint row = 2;
                foreach (var p in perc.PercepcionesDetalle)
                {
                    var r = row;
                    table.Cell().Row(r).Column(1).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder).Padding(2).Text(p.TipoPercepcion ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                    table.Cell().Row(r).Column(2).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder).Padding(2).Text(p.Clave ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                    table.Cell().Row(r).Column(3).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder).Padding(2).Text(p.Concepto ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                    table.Cell().Row(r).Column(4).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder).Padding(2).AlignRight().Text(p.ImporteGravado.ToString("C", MxCulture)).FontSize(PdfStyleConstants.FontSizeSmall);
                    table.Cell().Row(r).Column(5).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder).Padding(2).AlignRight().Text(p.ImporteExento.ToString("C", MxCulture)).FontSize(PdfStyleConstants.FontSizeSmall);
                    row++;

                    if (p.HorasExtra?.Any() == true)
                    {
                        table.Cell().Row(row).Column(1).ColumnSpan(5).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder)
                            .Padding(3).PaddingLeft(10).Column(heCol =>
                            {
                                heCol.Item().Text("Horas Extra:").Bold().FontSize(PdfStyleConstants.FontSizeSmall);
                                foreach (var he in p.HorasExtra)
                                {
                                    heCol.Item().Text($"Días: {he.Dias}, Tipo: {he.TipoHoras}, Horas: {he.HorasExtra}, Pagado: {he.ImportePagado.ToString("C", MxCulture)}")
                                        .FontSize(PdfStyleConstants.FontSizeSmall);
                                }
                            });
                        row++;
                    }
                }

                // Totales
                if (perc.TotalSueldos.HasValue)
                {
                    table.Cell().Row(row).Column(1).ColumnSpan(3).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder)
                        .Background(PdfStyleConstants.ColorHeaderBg).Padding(2).AlignRight().Text("Total Sueldos:").Bold().FontSize(PdfStyleConstants.FontSizeSmall);
                    table.Cell().Row(row).Column(4).ColumnSpan(2).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder)
                        .Padding(2).AlignRight().Text(perc.TotalSueldos.Value.ToString("C", MxCulture)).FontSize(PdfStyleConstants.FontSizeSmall);
                    row++;
                }

                table.Cell().Row(row).Column(1).ColumnSpan(3).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder)
                    .Background(PdfStyleConstants.ColorHeaderBg).Padding(2).AlignRight().Text("Total Gravado Percepciones:").Bold().FontSize(PdfStyleConstants.FontSizeSmall);
                table.Cell().Row(row).Column(4).ColumnSpan(2).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder)
                    .Padding(2).AlignRight().Text(perc.TotalGravado.ToString("C", MxCulture)).FontSize(PdfStyleConstants.FontSizeSmall);
                row++;

                table.Cell().Row(row).Column(1).ColumnSpan(3).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder)
                    .Background(PdfStyleConstants.ColorHeaderBg).Padding(2).AlignRight().Text("Total Exento Percepciones:").Bold().FontSize(PdfStyleConstants.FontSizeSmall);
                table.Cell().Row(row).Column(4).ColumnSpan(2).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder)
                    .Padding(2).AlignRight().Text(perc.TotalExento.ToString("C", MxCulture)).FontSize(PdfStyleConstants.FontSizeSmall);
            });
        }

        private static void ComposeDeducciones(ColumnDescriptor col, CfdiNominaViewModel model)
        {
            var ded = model.Nomina?.Deducciones;
            if (ded?.DeduccionesDetalle?.Any() != true) return;

            col.Item().Element(c => CfdiPdfSections.SectionTitle(c, "Deducciones"));

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(10); c.RelativeColumn(10); c.RelativeColumn(40); c.RelativeColumn(20);
                });

                var headers = new[] { "Tipo", "Clave", "Concepto", "Importe" };
                for (uint i = 0; i < headers.Length; i++)
                    table.Cell().Row(1).Column(i + 1).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder)
                        .Background(PdfStyleConstants.ColorHeaderBg).Padding(2).Text(headers[i]).Bold().FontSize(PdfStyleConstants.FontSizeSmall);

                uint row = 2;
                foreach (var d in ded.DeduccionesDetalle)
                {
                    var r = row;
                    table.Cell().Row(r).Column(1).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder).Padding(2).Text(d.TipoDeduccion ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                    table.Cell().Row(r).Column(2).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder).Padding(2).Text(d.Clave ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                    table.Cell().Row(r).Column(3).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder).Padding(2).Text(d.Concepto ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                    table.Cell().Row(r).Column(4).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder).Padding(2).AlignRight().Text(d.Importe.ToString("C", MxCulture)).FontSize(PdfStyleConstants.FontSizeSmall);
                    row++;
                }

                // Totales
                if (ded.TotalOtrasDeducciones.HasValue)
                {
                    table.Cell().Row(row).Column(1).ColumnSpan(3).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder)
                        .Background(PdfStyleConstants.ColorHeaderBg).Padding(2).AlignRight().Text("Total Otras Deducciones:").Bold().FontSize(PdfStyleConstants.FontSizeSmall);
                    table.Cell().Row(row).Column(4).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder)
                        .Padding(2).AlignRight().Text(ded.TotalOtrasDeducciones.Value.ToString("C", MxCulture)).FontSize(PdfStyleConstants.FontSizeSmall);
                    row++;
                }
                if (ded.TotalImpuestosRetenidos.HasValue)
                {
                    table.Cell().Row(row).Column(1).ColumnSpan(3).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder)
                        .Background(PdfStyleConstants.ColorHeaderBg).Padding(2).AlignRight().Text("Total Impuestos Retenidos:").Bold().FontSize(PdfStyleConstants.FontSizeSmall);
                    table.Cell().Row(row).Column(4).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder)
                        .Padding(2).AlignRight().Text(ded.TotalImpuestosRetenidos.Value.ToString("C", MxCulture)).FontSize(PdfStyleConstants.FontSizeSmall);
                }
            });
        }

        private static void ComposeOtrosPagos(ColumnDescriptor col, CfdiNominaViewModel model)
        {
            var op = model.Nomina?.OtrosPagos;
            if (op?.OtrosPagosDetalle?.Any() != true) return;

            col.Item().Element(c => CfdiPdfSections.SectionTitle(c, "Otros Pagos"));

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(10); c.RelativeColumn(10); c.RelativeColumn(40); c.RelativeColumn(20);
                });

                var headers = new[] { "Tipo", "Clave", "Concepto", "Importe" };
                for (uint i = 0; i < headers.Length; i++)
                    table.Cell().Row(1).Column(i + 1).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder)
                        .Background(PdfStyleConstants.ColorHeaderBg).Padding(2).Text(headers[i]).Bold().FontSize(PdfStyleConstants.FontSizeSmall);

                uint row = 2;
                foreach (var pago in op.OtrosPagosDetalle)
                {
                    var r = row;
                    table.Cell().Row(r).Column(1).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder).Padding(2).Text(pago.TipoOtroPago ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                    table.Cell().Row(r).Column(2).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder).Padding(2).Text(pago.Clave ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                    table.Cell().Row(r).Column(3).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder).Padding(2).Text(pago.Concepto ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                    table.Cell().Row(r).Column(4).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder).Padding(2).AlignRight().Text(pago.Importe.ToString("C", MxCulture)).FontSize(PdfStyleConstants.FontSizeSmall);
                    row++;

                    if (pago.SubsidioAlEmpleo != null)
                    {
                        table.Cell().Row(row).Column(1).ColumnSpan(4).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder)
                            .Padding(3).PaddingLeft(10).Column(subCol =>
                            {
                                subCol.Item().Text("Subsidio al Empleo:").Bold().FontSize(PdfStyleConstants.FontSizeSmall);
                                subCol.Item().Text($"Subsidio Causado: {pago.SubsidioAlEmpleo.SubsidioCausado.ToString("C", MxCulture)}")
                                    .FontSize(PdfStyleConstants.FontSizeSmall);
                            });
                        row++;
                    }
                }

                // Total otros pagos
                table.Cell().Row(row).Column(1).ColumnSpan(3).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder)
                    .Background(PdfStyleConstants.ColorHeaderBg).Padding(2).AlignRight().Text("Total Otros Pagos:").Bold().FontSize(PdfStyleConstants.FontSizeSmall);
                table.Cell().Row(row).Column(4).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder)
                    .Padding(2).AlignRight().Text(model.Nomina?.TotalOtrosPagos?.ToString("C", MxCulture) ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
            });
        }

        private static void ComposeIncapacidades(ColumnDescriptor col, CfdiNominaViewModel model)
        {
            if (model.Nomina?.Incapacidades?.Any() != true) return;

            col.Item().Element(c => CfdiPdfSections.SectionTitle(c, "Incapacidades"));

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });

                var headers = new[] { "Días Incapacidad", "Tipo Incapacidad", "Importe Monetario" };
                for (uint i = 0; i < headers.Length; i++)
                    table.Cell().Row(1).Column(i + 1).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder)
                        .Background(PdfStyleConstants.ColorHeaderBg).Padding(2).Text(headers[i]).Bold().FontSize(PdfStyleConstants.FontSizeSmall);

                uint row = 2;
                foreach (var inc in model.Nomina.Incapacidades)
                {
                    var r = row;
                    table.Cell().Row(r).Column(1).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder).Padding(2).AlignRight().Text(inc.DiasIncapacidad.ToString()).FontSize(PdfStyleConstants.FontSizeSmall);
                    table.Cell().Row(r).Column(2).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder).Padding(2).Text(inc.TipoIncapacidad ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                    table.Cell().Row(r).Column(3).Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder).Padding(2).AlignRight().Text(inc.ImporteMonetario?.ToString("C", MxCulture) ?? "").FontSize(PdfStyleConstants.FontSizeSmall);
                    row++;
                }
            });
        }

        private static void ComposeTotalesGenerales(ColumnDescriptor col, CfdiNominaViewModel model)
        {
            col.Item().Element(c => CfdiPdfSections.SectionTitle(c, "Totales Generales del Comprobante"));

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });

                CfdiPdfSections.HeaderValueRow(table, 1, 1, "Subtotal del Comprobante", model.SubTotal.ToString("C", MxCulture));
                CfdiPdfSections.HeaderValueRow(table, 1, 3, "Descuento del Comprobante", model.Descuento.ToString("C", MxCulture));

                table.Cell().Row(2).Column(1).ColumnSpan(3)
                    .Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder)
                    .Background(PdfStyleConstants.ColorHeaderBg)
                    .Padding(3).AlignRight().Text("TOTAL NETO PAGADO").Bold();
                table.Cell().Row(2).Column(4)
                    .Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder)
                    .Padding(3).AlignRight().Text(model.Total.ToString("C", MxCulture)).Bold();

                table.Cell().Row(3).Column(1)
                    .Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder)
                    .Background(PdfStyleConstants.ColorHeaderBg)
                    .Padding(3).Text("Cantidad con Letra").Bold();
                table.Cell().Row(3).Column(2).ColumnSpan(3)
                    .Border(0.5f).BorderColor(PdfStyleConstants.ColorBorder)
                    .Padding(3).Text(model.CantidadConLetra ?? "");
            });
        }
    }
}
