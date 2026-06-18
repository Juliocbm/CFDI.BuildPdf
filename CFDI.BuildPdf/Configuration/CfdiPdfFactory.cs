using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Complements;
using CFDI.BuildPdf.Helpers;
using CFDI.BuildPdf.Mappers.CartaPorte;
using CFDI.BuildPdf.Mappers.Factura;
using CFDI.BuildPdf.Mappers.Nomina;
using CFDI.BuildPdf.PdfBuilders.CartaPorte;
using CFDI.BuildPdf.PdfBuilders.Factura;
using CFDI.BuildPdf.PdfBuilders.Nomina;
using CFDI.BuildPdf.Services;
using Microsoft.Extensions.Logging;

namespace CFDI.BuildPdf.Configuration
{
    /// <summary>
    /// Composition root interno: define en UN solo lugar el grafo mapper→builder→handler→orquestador.
    /// Lo usan tanto la fachada estática (<see cref="CFDI.BuildPdf.CfdiPdf"/>) como
    /// <see cref="Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions.AddCfdiPdfServices"/>, evitando duplicar el cableado.
    /// </summary>
    internal static class CfdiPdfFactory
    {
        /// <summary>
        /// Construye el orquestador con todos los handlers de complemento soportados.
        /// </summary>
        /// <param name="loggerFactory">Factory opcional para inyectar loggers en los mappers.</param>
        public static ICfdiPdfGenerator CreateGenerator(ILoggerFactory? loggerFactory = null)
        {
            var qrGenerator = new QrGeneratorService();

            var handlers = new ICfdiComplementHandler[]
            {
                new CartaPorteComplementHandler(
                    new CartaPorteMapper(qrGenerator, loggerFactory?.CreateLogger<CartaPorteMapper>()),
                    new CartaPorteDocumentBuilder()),
                new NominaComplementHandler(
                    new NominaMapper(qrGenerator, loggerFactory?.CreateLogger<NominaMapper>()),
                    new NominaDocumentBuilder()),
                new FacturaComplementHandler(
                    new FacturaMapper(qrGenerator, loggerFactory?.CreateLogger<FacturaMapper>()),
                    new FacturaDocumentBuilder())
            };

            return new CfdiPdfGenerator(handlers);
        }
    }
}
