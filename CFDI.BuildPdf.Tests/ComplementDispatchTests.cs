using System.Threading.Tasks;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Complements;
using CFDI.BuildPdf.Mappers.CartaPorte;
using CFDI.BuildPdf.Mappers.Nomina;
using CFDI.BuildPdf.PdfBuilders.CartaPorte;
using CFDI.BuildPdf.PdfBuilders.Nomina;
using CFDI.BuildPdf.Service;
using CFDI.BuildPdf.Services;
using CFDI.BuildPdf.Tests.Helpers;
using Xunit;

namespace CFDI.BuildPdf.Tests
{
    /// <summary>
    /// Cubre el despacho por namespace del orquestador (reemplaza la lógica del antiguo switch/CanMap).
    /// </summary>
    public class ComplementDispatchTests
    {
        private static CartaPorteComplementHandler NewCartaPorteHandler()
            => new(new CartaPorteMapper(new FakeQrGenerator()), new CartaPorteDocumentBuilder());

        private static NominaComplementHandler NewNominaHandler()
            => new(new NominaMapper(new FakeQrGenerator()), new NominaDocumentBuilder());

        [Fact]
        public void CartaPorteHandler_DeclaraNamespaceCartaPorte31()
        {
            var handler = NewCartaPorteHandler();
            Assert.Contains("http://www.sat.gob.mx/CartaPorte31", handler.ComplementNamespaces);
            Assert.DoesNotContain("http://www.sat.gob.mx/nomina12", handler.ComplementNamespaces);
        }

        [Fact]
        public void NominaHandler_DeclaraNamespaceNomina12()
        {
            var handler = NewNominaHandler();
            Assert.Contains("http://www.sat.gob.mx/nomina12", handler.ComplementNamespaces);
            Assert.DoesNotContain("http://www.sat.gob.mx/CartaPorte31", handler.ComplementNamespaces);
        }

        [Fact]
        public void DosHandlersMismoNamespace_LanzaInvalidOperation()
        {
            // Dos handlers que declaran el mismo namespace deben fallar al construir el orquestador.
            var handlers = new ICfdiComplementHandler[] { NewCartaPorteHandler(), NewCartaPorteHandler() };
            Assert.Throws<System.InvalidOperationException>(() => new CfdiPdfGenerator(handlers));
        }

        [Fact]
        public async Task CfdiSinComplementoSoportado_LanzaCfdiComplementoNoSoportado()
        {
            // CFDI 4.0 bien formado pero sin complemento Carta Porte ni Nómina.
            const string xmlSinComplemento =
                "<cfdi:Comprobante xmlns:cfdi=\"http://www.sat.gob.mx/cfd/4\" Version=\"4.0\" " +
                "Total=\"0\" SubTotal=\"0\"></cfdi:Comprobante>";

            await Assert.ThrowsAsync<CfdiComplementoNoSoportadoException>(
                () => CfdiPdf.DesdeXmlStringAsync(xmlSinComplemento));
        }
    }
}
