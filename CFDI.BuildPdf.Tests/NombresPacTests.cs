using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CFDI.BuildPdf;
using CFDI.BuildPdf.PdfBuilders.Common;
using CFDI.BuildPdf.Tests.Helpers;
using UglyToad.PdfPig;
using Xunit;

namespace CFDI.BuildPdf.Tests
{
    /// <summary>
    /// Verifica el campo "PAC QUE TIMBRÓ" y <see cref="CfdiPdfOptions.NombresPac"/>: la librería no trae nombres
    /// de PAC; imprime el nombre configurado por el consumidor (sin distinguir mayúsculas) o, si no hay, solo el
    /// RFC del timbre (no como error).
    /// </summary>
    public class NombresPacTests
    {
        private static async Task<string> TextoPdf(CfdiPdfOptions? options)
        {
            var pdf = await CfdiPdf.DesdeXmlStringAsync(TestXmlLoader.LoadFacturaIngreso().ToString(), options);
            using var doc = PdfDocument.Open(pdf);
            // Sin espacios: un nombre largo salta de renglón en la columna del encabezado.
            return Regex.Replace(string.Join("\n", doc.GetPages().Select(p => p.Text)), @"\s+", "");
        }

        [Fact]
        public void SinConfiguracion_NoHayNombres()
        {
            // Ni siquiera los PACs más comunes: la librería no trae catálogo de PACs.
            Assert.Null(PacTimbrador.Nombre("SST060807KU0", null));
            Assert.Null(PacTimbrador.Nombre("SST060807KU0", new Dictionary<string, string>()));
            Assert.Null(PacTimbrador.Nombre(null, new Dictionary<string, string> { ["X"] = "Y" }));
        }

        [Theory]
        [InlineData("SST060807KU0", "Buzón E (SST060807KU0)")]
        [InlineData("PPD101129EA3", "PPD101129EA3")]        // sin nombre: solo el RFC, no "PAC no identificado"
        [InlineData(" PPD101129EA3 ", "PPD101129EA3")]
        [InlineData(null, "")]
        public void Texto_NombreYRfc_OSoloRfc(string? rfc, string esperado)
        {
            var nombres = new Dictionary<string, string> { ["SST060807KU0"] = "Buzón E" };

            Assert.Equal(esperado, PacTimbrador.Texto(rfc, nombres));
        }

        [Fact]
        public void PacNuevo_SeAgregaPorConfiguracion()
        {
            var nombres = new Dictionary<string, string> { ["PPD101129EA3"] = "Prodigia" };

            Assert.Equal("Prodigia (PPD101129EA3)", PacTimbrador.Texto("PPD101129EA3", nombres));
        }

        [Fact]
        public void RfcSinDistinguirMayusculas_AunConDiccionarioSensible()
        {
            // El consumidor puede asignar su propio Dictionary (sensible a mayúsculas por defecto).
            var nombres = new Dictionary<string, string> { ["ppd101129ea3"] = "Prodigia" };

            Assert.Equal("Prodigia", PacTimbrador.Nombre("PPD101129EA3", nombres));
            Assert.Equal("Prodigia", PacTimbrador.Nombre(" PPD101129EA3 ", nombres));
        }

        [Fact]
        public void NombreVacioEnConfiguracion_SeIgnoraYQuedaSoloElRfc()
        {
            var nombres = new Dictionary<string, string> { ["SST060807KU0"] = "  " };

            Assert.Equal("SST060807KU0", PacTimbrador.Texto("SST060807KU0", nombres));
        }

        [Fact]
        public async Task Pdf_RfcSinNombre_ImprimeSoloElRfc()
        {
            var texto = await TextoPdf(null);

            Assert.Contains("PACQUETIMBRÓ:PPD101129EA3", texto);
            Assert.DoesNotContain("noidentificado", texto);
        }

        [Fact]
        public async Task Pdf_ImprimeElNombreConfigurado()
        {
            var options = new CfdiPdfOptions();
            options.NombresPac["PPD101129EA3"] = "Prodigia";

            Assert.Contains("PACQUETIMBRÓ:Prodigia(PPD101129EA3)", await TextoPdf(options));
        }

        [Fact]
        public void CombinarConGlobales_LaLlamadaGanaYNadaSeModifica()
        {
            var globales = new Dictionary<string, string> { ["SST060807KU0"] = "Global BuzonE", ["PPD101129EA3"] = "Global Prodigia" };
            var llamada = new CfdiPdfOptions { TextoPiePagina = "PIE" };
            llamada.NombresPac["ppd101129ea3"] = "Prodigia de la llamada";

            var efectivas = llamada.ConNombresPacGlobales(globales);

            Assert.NotSame(llamada, efectivas);
            Assert.Equal("PIE", efectivas.TextoPiePagina);
            Assert.Equal("Global BuzonE", efectivas.NombresPac["SST060807KU0"]);
            Assert.Equal("Prodigia de la llamada", efectivas.NombresPac["PPD101129EA3"]);
            Assert.Single(llamada.NombresPac);   // las opciones del llamador no se tocan
            Assert.Equal(2, globales.Count);      // ni el diccionario global
        }

        [Fact]
        public void CombinarSinGlobales_DevuelveLaMismaInstancia()
        {
            var llamada = new CfdiPdfOptions();

            Assert.Same(llamada, llamada.ConNombresPacGlobales(null));
            Assert.Same(llamada, llamada.ConNombresPacGlobales(new Dictionary<string, string>()));
        }
    }
}
