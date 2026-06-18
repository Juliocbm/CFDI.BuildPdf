using System;
using System.Globalization;
using System.Linq;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Catalogs;
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
                        col.Item().Element(c => CfdiPdfSections.ComposeEncabezado(c, model, _logger));
                        ComprobanteSections.ComposeClienteYEmision(col, model);
                        ComprobanteSections.ComposeFormaPago(col, model, model.CondicionesPago);
                        ComprobanteSections.ComposeConceptos(col, model.Conceptos);
                        ComprobanteSections.ComposeTotales(col, model, model.TrasladosResumen, model.RetencionesResumen, model.TotalImpuestosTrasladados, model.TotalImpuestosRetenidos);

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
                CfdiPdfSections.HeaderValueRow(table, 1, 5, "Vía de entrada/salida", SatCatalogos.NombreCveTransporte(cp.ViaEntradaSalida));

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
                        BCell(3).Text(SatCatalogos.NombreClaveUnidad(m.ClaveUnidad)).FontSize(PdfStyleConstants.FontSizeSmall);
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

                CfdiPdfSections.HeaderValueRow(table, 1, 1, "Permiso SCT", SatCatalogos.NombrePermisoSCT(at.PermisoSCT));
                CfdiPdfSections.HeaderValueRow(table, 1, 3, "Número Permiso SCT", at.NumeroPermisoSCT);
                CfdiPdfSections.HeaderValueRow(table, 2, 1, "Configuración Vehicular", SatCatalogos.NombreConfigVehicular(at.ConfigVehicular));
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
                CfdiPdfSections.HeaderValueRow(table, 1, 1, "SubTipo Remolque", SatCatalogos.NombreSubTipoRemolque(model.CartaPorte.Remolque.SubTipoRemolque));
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

                    BCell(1).Text(SatCatalogos.NombreTipoFigura(f.TipoFigura)).FontSize(PdfStyleConstants.FontSizeSmall);
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
