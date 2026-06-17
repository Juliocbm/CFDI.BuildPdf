using System.Threading.Tasks;
using CFDI.BuildPdf;
using Xunit;

namespace CFDI.BuildPdf.Tests
{
    /// <summary>
    /// Verifica el manejo de XML mal formado en los puntos de entrada públicos.
    /// </summary>
    public class InvalidXmlTests
    {
        [Fact]
        public async Task DesdeXmlString_XmlMalFormado_LanzaCfdiXmlInvalido()
        {
            const string xmlRoto = "<cfdi:Comprobante xmlns:cfdi=\"http://www.sat.gob.mx/cfd/4\" Version=\"4.0\"";

            await Assert.ThrowsAsync<CfdiXmlInvalidoException>(
                () => CfdiPdf.DesdeXmlStringAsync(xmlRoto));
        }

        [Fact]
        public async Task DesdeXmlBytes_XmlMalFormado_LanzaCfdiXmlInvalido()
        {
            var bytesRotos = System.Text.Encoding.UTF8.GetBytes("<no-cerrado>");

            await Assert.ThrowsAsync<CfdiXmlInvalidoException>(
                () => CfdiPdf.DesdeXmlBytesAsync(bytesRotos));
        }
    }
}
