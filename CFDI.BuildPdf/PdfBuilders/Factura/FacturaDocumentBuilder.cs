using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Models;
using CFDI.BuildPdf.PdfBuilders.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CFDI.BuildPdf.PdfBuilders.Factura
{
    /// <summary>
    /// Construye el PDF de una factura CFDI 4.0 base (Ingreso/Egreso) sin complemento,
    /// reutilizando las secciones compartidas de comprobante.
    /// </summary>
    internal class FacturaDocumentBuilder : IPdfDocumentBuilder<CfdiFacturaViewModel>
    {
        private readonly ILogger<FacturaDocumentBuilder> _logger;

        public FacturaDocumentBuilder(ILogger<FacturaDocumentBuilder>? logger = null)
        {
            _logger = logger ?? NullLogger<FacturaDocumentBuilder>.Instance;
        }

        public byte[] Build(CfdiFacturaViewModel model, CfdiPdfOptions options)
        {
            var pageSize = options.Orientacion == PdfOrientation.Landscape
                ? PageSizes.Letter.Landscape()
                : PageSizes.Letter;

            var document = Document.Create(container =>
            {
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
            });

            return document.GeneratePdf();
        }
    }
}
