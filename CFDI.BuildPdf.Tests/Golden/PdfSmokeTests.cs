using System.Linq;
using System.Threading.Tasks;
using CFDI.BuildPdf;
using CFDI.BuildPdf.Tests.Helpers;
using UglyToad.PdfPig;
using Xunit;

namespace CFDI.BuildPdf.Tests.Golden
{
    public class PdfSmokeTests
    {
        [Fact]
        [Trait("Category", "Golden")]
        public async Task CartaPorte_GeneraPdfValidoConContenido()
        {
            var xml = TestXmlLoader.LoadCartaPorte().ToString();

            var pdfBytes = await CfdiPdf.DesdeXmlStringAsync(xml);

            // Es un PDF válido y no trivial
            Assert.NotNull(pdfBytes);
            Assert.True(pdfBytes.Length > 1000, $"PDF demasiado pequeño: {pdfBytes.Length} bytes");
            Assert.Equal((byte)'%', pdfBytes[0]);
            Assert.Equal((byte)'P', pdfBytes[1]);
            Assert.Equal((byte)'D', pdfBytes[2]);
            Assert.Equal((byte)'F', pdfBytes[3]);

            // Tiene páginas y contenido textual real
            using var pdf = PdfDocument.Open(pdfBytes);
            Assert.True(pdf.NumberOfPages >= 1);
            var texto = string.Join(" ", pdf.GetPages().Select(p => p.Text));
            Assert.True(texto.Length > 200, $"Texto extraído demasiado corto: {texto.Length} chars");
        }

        [Fact]
        [Trait("Category", "Golden")]
        public async Task Nomina_GeneraPdfValidoConContenido()
        {
            var xml = TestXmlLoader.LoadNomina().ToString();

            var pdfBytes = await CfdiPdf.DesdeXmlStringAsync(xml);

            Assert.NotNull(pdfBytes);
            Assert.True(pdfBytes.Length > 1000, $"PDF demasiado pequeño: {pdfBytes.Length} bytes");
            Assert.Equal((byte)'%', pdfBytes[0]);
            Assert.Equal((byte)'P', pdfBytes[1]);
            Assert.Equal((byte)'D', pdfBytes[2]);
            Assert.Equal((byte)'F', pdfBytes[3]);

            using var pdf = PdfDocument.Open(pdfBytes);
            Assert.True(pdf.NumberOfPages >= 1);
            var texto = string.Join(" ", pdf.GetPages().Select(p => p.Text));
            Assert.True(texto.Length > 200, $"Texto extraído demasiado corto: {texto.Length} chars");
        }

        [Fact]
        [Trait("Category", "Golden")]
        public async Task CartaPorteRetenciones_GeneraPdfValidoConContenido()
        {
            var xml = TestXmlLoader.LoadCartaPorteRetenciones().ToString();

            var pdfBytes = await CfdiPdf.DesdeXmlStringAsync(xml);

            Assert.NotNull(pdfBytes);
            Assert.True(pdfBytes.Length > 1000, $"PDF demasiado pequeño: {pdfBytes.Length} bytes");
            Assert.Equal((byte)'%', pdfBytes[0]);
            Assert.Equal((byte)'P', pdfBytes[1]);
            Assert.Equal((byte)'D', pdfBytes[2]);
            Assert.Equal((byte)'F', pdfBytes[3]);

            using var pdf = PdfDocument.Open(pdfBytes);
            Assert.True(pdf.NumberOfPages >= 1);
            var texto = string.Join(" ", pdf.GetPages().Select(p => p.Text));
            Assert.True(texto.Length > 200, $"Texto extraído demasiado corto: {texto.Length} chars");
        }

        [Fact]
        [Trait("Category", "Golden")]
        public async Task NominaIncapacidades_GeneraPdfValidoConContenido()
        {
            var xml = TestXmlLoader.LoadNominaIncapacidades().ToString();

            var pdfBytes = await CfdiPdf.DesdeXmlStringAsync(xml);

            Assert.NotNull(pdfBytes);
            Assert.True(pdfBytes.Length > 1000, $"PDF demasiado pequeño: {pdfBytes.Length} bytes");
            Assert.Equal((byte)'%', pdfBytes[0]);
            Assert.Equal((byte)'P', pdfBytes[1]);
            Assert.Equal((byte)'D', pdfBytes[2]);
            Assert.Equal((byte)'F', pdfBytes[3]);

            using var pdf = PdfDocument.Open(pdfBytes);
            Assert.True(pdf.NumberOfPages >= 1);
            var texto = string.Join(" ", pdf.GetPages().Select(p => p.Text));
            Assert.True(texto.Length > 200, $"Texto extraído demasiado corto: {texto.Length} chars");
        }
    }
}
