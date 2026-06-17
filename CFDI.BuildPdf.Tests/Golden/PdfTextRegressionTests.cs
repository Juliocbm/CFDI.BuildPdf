using System.Linq;
using System.Threading.Tasks;
using CFDI.BuildPdf;
using CFDI.BuildPdf.Tests.Helpers;
using UglyToad.PdfPig;
using Xunit;

namespace CFDI.BuildPdf.Tests.Golden
{
    /// <summary>
    /// Fija el texto extraído del PDF de Carta Porte como baseline. Sirve de red de
    /// seguridad: al extraer las secciones de render compartidas, el texto debe quedar idéntico.
    /// </summary>
    public class PdfTextRegressionTests
    {
        private static async Task<string> ExtraerTexto(System.Xml.Linq.XDocument xdoc)
        {
            var pdfBytes = await CfdiPdf.DesdeXmlStringAsync(xdoc.ToString());
            using var pdf = PdfDocument.Open(pdfBytes);
            return string.Join("\n", pdf.GetPages().Select(p => p.Text));
        }

        [Fact]
        [Trait("Category", "Golden")]
        public async Task CartaPorte_TextoPdf_CoincideConBaseline()
        {
            var texto = await ExtraerTexto(TestXmlLoader.LoadCartaPorte());
            Snapshot.Match(texto, "CartaPorte.pdftext.txt");
        }

        [Fact]
        [Trait("Category", "Golden")]
        public async Task CartaPorteRetenciones_TextoPdf_CoincideConBaseline()
        {
            var texto = await ExtraerTexto(TestXmlLoader.LoadCartaPorteRetenciones());
            Snapshot.Match(texto, "CartaPorteRetenciones.pdftext.txt");
        }
    }
}
