using System.Threading.Tasks;
using CFDI.BuildPdf.Complements;
using CFDI.BuildPdf.Mappers.CartaPorte;
using CFDI.BuildPdf.Mappers.Nomina;
using CFDI.BuildPdf.PdfBuilders.CartaPorte;
using CFDI.BuildPdf.PdfBuilders.Nomina;
using CFDI.BuildPdf;
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

        [Theory]
        [InlineData("I")]
        [InlineData("E")]
        public async Task FacturaBaseIngresoEgreso_GeneraPdf(string tipo)
        {
            var xml =
                $"<cfdi:Comprobante xmlns:cfdi=\"http://www.sat.gob.mx/cfd/4\" Version=\"4.0\" " +
                $"TipoDeComprobante=\"{tipo}\" SubTotal=\"100\" Total=\"116\" Moneda=\"MXN\">" +
                "<cfdi:Emisor Rfc=\"AAA010101AAA\" Nombre=\"E\" RegimenFiscal=\"601\"/>" +
                "<cfdi:Receptor Rfc=\"XAXX010101000\" Nombre=\"R\" RegimenFiscalReceptor=\"601\" UsoCFDI=\"G03\"/>" +
                "<cfdi:Conceptos><cfdi:Concepto ClaveProdServ=\"01010101\" Cantidad=\"1\" ClaveUnidad=\"H87\" Descripcion=\"X\" ValorUnitario=\"100\" Importe=\"100\" ObjetoImp=\"02\"/></cfdi:Conceptos>" +
                "</cfdi:Comprobante>";

            var pdf = await CfdiPdf.DesdeXmlStringAsync(xml);
            Assert.True(pdf.Length > 1000);
            Assert.Equal((byte)'%', pdf[0]);
        }

        [Theory]
        [InlineData("T")]
        [InlineData("P")]
        public async Task TipoNoSoportadoSinComplemento_Lanza(string tipo)
        {
            var xml =
                $"<cfdi:Comprobante xmlns:cfdi=\"http://www.sat.gob.mx/cfd/4\" Version=\"4.0\" " +
                $"TipoDeComprobante=\"{tipo}\" SubTotal=\"0\" Total=\"0\"></cfdi:Comprobante>";

            await Assert.ThrowsAsync<CfdiComplementoNoSoportadoException>(
                () => CfdiPdf.DesdeXmlStringAsync(xml));
        }
    }
}
