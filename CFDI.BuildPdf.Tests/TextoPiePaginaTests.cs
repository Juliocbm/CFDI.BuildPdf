using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CFDI.BuildPdf;
using CFDI.BuildPdf.PdfBuilders.Common;
using CFDI.BuildPdf.Tests.Helpers;
using UglyToad.PdfPig;
using Xunit;

namespace CFDI.BuildPdf.Tests
{
    /// <summary>
    /// Verifica <see cref="CfdiPdfOptions.TextoPiePagina"/>: se imprime en el pie de todas las hojas,
    /// se trunca a 2 renglones y, si no viene, no altera el PDF.
    /// </summary>
    public class TextoPiePaginaTests
    {
        private const string Marcador = "FOLIO-PRUEBA-0001";

        private static async Task<List<string>> ExtraerTextoPorHoja(string resourceName, CfdiPdfOptions? options = null)
        {
            var xml = TestXmlLoader.Load(resourceName).ToString();
            var pdfBytes = await CfdiPdf.DesdeXmlStringAsync(xml, options);
            using var pdf = PdfDocument.Open(pdfBytes);
            return pdf.GetPages().Select(p => p.Text).ToList();
        }

        private static int ContarOcurrencias(string texto, string buscado)
        {
            var total = 0;
            for (var i = texto.IndexOf(buscado, System.StringComparison.Ordinal); i >= 0;
                 i = texto.IndexOf(buscado, i + buscado.Length, System.StringComparison.Ordinal))
                total++;
            return total;
        }

        [Theory]
        [InlineData("CFDI.BuildPdf.Tests.TestData.cfdi_cartaporte.xml")]               // incluye la hoja de condiciones
        [InlineData("CFDI.BuildPdf.Tests.TestData.cfdi_cartaporte_retenciones.xml")]
        [InlineData("CFDI.BuildPdf.Tests.TestData.cfdi_factura_ingreso.xml")]
        [InlineData("CFDI.BuildPdf.Tests.TestData.cfdi_factura_egreso.xml")]
        [InlineData("CFDI.BuildPdf.Tests.TestData.cfdi_nomina.xml")]
        [InlineData("CFDI.BuildPdf.Tests.TestData.cfdi_nomina_incapacidades.xml")]
        public async Task ConTexto_SeImprimeUnaVezEnCadaHoja(string resourceName)
        {
            var hojas = await ExtraerTextoPorHoja(resourceName, new CfdiPdfOptions { TextoPiePagina = Marcador });

            Assert.NotEmpty(hojas);
            for (var i = 0; i < hojas.Count; i++)
                Assert.True(ContarOcurrencias(hojas[i], Marcador) == 1,
                    $"La hoja {i + 1} de {hojas.Count} debía contener el texto del pie exactamente una vez.");
        }

        [Fact]
        public async Task CartaPorte_HojaDeCondiciones_LlevaElTextoPeroNoElPaginado()
        {
            var hojas = await ExtraerTextoPorHoja(
                "CFDI.BuildPdf.Tests.TestData.cfdi_cartaporte.xml",
                new CfdiPdfOptions { TextoPiePagina = Marcador });

            var condiciones = hojas[^1];
            Assert.Contains(Marcador, condiciones);
            Assert.DoesNotContain("Página", condiciones);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\r\n\t")]
        public async Task SinTexto_ElPdfQuedaIgualQueSinLaOpcion(string? texto)
        {
            const string resourceName = "CFDI.BuildPdf.Tests.TestData.cfdi_cartaporte.xml";

            var sinOpcion = await ExtraerTextoPorHoja(resourceName);
            var conOpcionVacia = await ExtraerTextoPorHoja(resourceName, new CfdiPdfOptions { TextoPiePagina = texto });

            Assert.Equal(sinOpcion, conOpcionVacia);
        }

        [Fact]
        public async Task TextoLargo_SeTruncaSinRomperElLayout()
        {
            var relleno = string.Join(" ", Enumerable.Range(1, 400).Select(n => $"relleno{n}"));
            var options = new CfdiPdfOptions { TextoPiePagina = $"INICIO-PIE {relleno} FIN-PIE" };

            var hojas = await ExtraerTextoPorHoja("CFDI.BuildPdf.Tests.TestData.cfdi_nomina.xml", options);

            Assert.All(hojas, hoja =>
            {
                Assert.Contains("INICIO-PIE", hoja);
                Assert.Contains("…", hoja);
                Assert.DoesNotContain("FIN-PIE", hoja);
            });
        }

        [Fact]
        public async Task CodigoLargoSinEspacios_SeParteYTruncaSinRomperElLayout()
        {
            // Un código/hash sin espacios no tiene dónde partirse por palabra: debe partirse por carácter, no lanzar.
            var options = new CfdiPdfOptions { TextoPiePagina = "COD" + new string('X', 3000) };

            var hojas = await ExtraerTextoPorHoja("CFDI.BuildPdf.Tests.TestData.cfdi_cartaporte.xml", options);

            Assert.All(hojas, hoja =>
            {
                Assert.Contains("CODXXXX", hoja);
                Assert.Contains("…", hoja);
            });
        }

        [Fact]
        public async Task RenglonesVaciosYControles_NoConsumenElTopeDeRenglones()
        {
            // Sin normalizar serían 3 renglones ("FOLIO-A", "", "RUTA\tB") y el tope de 2 cortaría "RUTA B".
            var options = new CfdiPdfOptions { TextoPiePagina = "FOLIO-A\r\n\r\nRUTA\tB" };

            var hojas = await ExtraerTextoPorHoja("CFDI.BuildPdf.Tests.TestData.cfdi_factura_ingreso.xml", options);

            Assert.Contains("FOLIO-A", hojas[0]);
            Assert.Contains("RUTA B", hojas[0]);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData("  \t\r\n ", null)]
        [InlineData("\0\0", null)]
        [InlineData(" FOLIO-1 ", "FOLIO-1")]
        [InlineData("A\r\nB", "A\nB")]
        [InlineData("A\rB", "A\nB")]
        [InlineData("A\n\n\nB", "A\nB")]
        [InlineData("  A  \n  B  ", "A\nB")]
        [InlineData("A\tB", "A B")]
        public void NormalizarTextoPiePagina_LimpiaControlesYRenglonesVacios(string? entrada, string? esperado)
        {
            Assert.Equal(esperado, CfdiPdfSections.NormalizarTextoPiePagina(entrada));
        }
    }
}
