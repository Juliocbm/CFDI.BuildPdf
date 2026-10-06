using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CFDI.BuildPdf.Configuration;
using CFDI.BuildPdf.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CFDI.BuildPdf.Tests
{
    public class CompositionRootTests
    {
        [Fact]
        public void Factory_CableaLoggersEnLosMappers()
        {
            var spy = new SpyLoggerFactory();

            var generator = CfdiPdfFactory.CreateGenerator(spy);

            Assert.NotNull(generator);
            Assert.Contains(spy.CategoriasCreadas, c => c.Contains("CartaPorteMapper"));
            Assert.Contains(spy.CategoriasCreadas, c => c.Contains("NominaMapper"));
        }

        [Fact]
        public async Task DI_ResuelveOrquestadorYGeneraPdf()
        {
            var services = new ServiceCollection();
            services.AddCfdiPdfServices();
            using var provider = services.BuildServiceProvider();

            var generator = provider.GetRequiredService<ICfdiPdfGenerator>();
            var xml = TestXmlLoader.LoadCartaPorte().ToString();

            var pdf = await generator.GenerarDesdeXmlStringAsync(xml);

            Assert.True(pdf.Length > 1000);
            Assert.Equal((byte)'%', pdf[0]);
        }

        private static string Texto(byte[] pdf)
        {
            using var doc = UglyToad.PdfPig.PdfDocument.Open(pdf);
            return string.Join("\n", doc.GetPages().Select(p => p.Text));
        }

        [Fact]
        public async Task DI_ConfigureSeAplicaCuandoLaLlamadaNoTraeOpciones()
        {
            var services = new ServiceCollection();
            services.AddCfdiPdfServices(configure: o => o.TextoPiePagina = "PIE-DESDE-DI");
            using var provider = services.BuildServiceProvider();
            var generator = provider.GetRequiredService<ICfdiPdfGenerator>();
            var xml = TestXmlLoader.LoadFacturaIngreso().ToString();

            var sinOpciones = Texto(await generator.GenerarDesdeXmlStringAsync(xml));
            var conOpciones = Texto(await generator.GenerarDesdeXmlStringAsync(xml, new CfdiPdfOptions { TextoPiePagina = "PIE-DE-LA-LLAMADA" }));

            Assert.Contains("PIE-DESDE-DI", sinOpciones);
            // Las opciones de la llamada reemplazan completas a las de DI (no se mezclan).
            Assert.Contains("PIE-DE-LA-LLAMADA", conOpciones);
            Assert.DoesNotContain("PIE-DESDE-DI", conOpciones);
        }

        [Fact]
        public async Task DI_NombresPacGlobales_SeCombinanConLasOpcionesDeLaLlamada()
        {
            var services = new ServiceCollection();
            services.AddCfdiPdfServices(configure: o => o.NombresPac["PPD101129EA3"] = "PAC GLOBAL");
            using var provider = services.BuildServiceProvider();
            var generator = provider.GetRequiredService<ICfdiPdfGenerator>();
            var xml = TestXmlLoader.LoadFacturaIngreso().ToString();
            static string SinEspacios(string s) => System.Text.RegularExpressions.Regex.Replace(s, @"\s+", "");

            // La llamada trae sus propias opciones (p. ej. el pie): el nombre global del PAC no se pierde.
            var llamada = new CfdiPdfOptions { TextoPiePagina = "PIE-DE-LA-LLAMADA" };
            var conPie = SinEspacios(Texto(await generator.GenerarDesdeXmlStringAsync(xml, llamada)));
            Assert.Contains("PIE-DE-LA-LLAMADA", conPie);
            Assert.Contains("PACGLOBAL(PPD101129EA3)", conPie);
            Assert.Empty(llamada.NombresPac);   // no se modifican las opciones del llamador

            // Si la llamada trae nombre para el mismo RFC, gana el de la llamada.
            var sobrescribe = new CfdiPdfOptions();
            sobrescribe.NombresPac["PPD101129EA3"] = "PAC LLAMADA";
            var conNombre = SinEspacios(Texto(await generator.GenerarDesdeXmlStringAsync(xml, sobrescribe)));
            Assert.Contains("PACLLAMADA(PPD101129EA3)", conNombre);
            Assert.DoesNotContain("PACGLOBAL", conNombre);
        }

        [Fact]
        public async Task DI_RespetaOpcionesCargadasDesdeConfiguracion()
        {
            // Mismo escenario que appsettings.json: "CfdiPdf": { "TextoPiePagina": ..., "NombresPac": { RFC: nombre } }
            var configuracion = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CfdiPdf:TextoPiePagina"] = "HG-F-CXC-02 REV.:02 24/09/2026",
                    ["CfdiPdf:NombresPac:PPD101129EA3"] = "PAC DESDE APPSETTINGS",
                })
                .Build();
            var services = new ServiceCollection();
            services.Configure<CfdiPdfOptions>(configuracion.GetSection("CfdiPdf"));
            services.AddCfdiPdfServices();
            using var provider = services.BuildServiceProvider();

            var pdf = await provider.GetRequiredService<ICfdiPdfGenerator>()
                .GenerarDesdeXmlStringAsync(TestXmlLoader.LoadFacturaIngreso().ToString());
            var texto = Texto(pdf);

            Assert.Contains("HG-F-CXC-02 REV.:02 24/09/2026", texto);
            // El nombre largo salta de renglón en la columna del encabezado: comparar sin espacios.
            Assert.Contains("PACDESDEAPPSETTINGS(PPD101129EA3)", System.Text.RegularExpressions.Regex.Replace(texto, @"\s+", ""));
        }

        [Fact]
        public void Licencia_NoSeDegradaSiYaEstaConfigurada()
        {
            var original = QuestPDF.Settings.License;
            try
            {
                QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Enterprise;

                new ServiceCollection().AddCfdiPdfServices(licenseType: CfdiPdfLicenseType.Community);

                Assert.Equal(QuestPDF.Infrastructure.LicenseType.Enterprise, QuestPDF.Settings.License);
            }
            finally
            {
                QuestPDF.Settings.License = original;
            }
        }

        private sealed class SpyLoggerFactory : ILoggerFactory
        {
            public List<string> CategoriasCreadas { get; } = new();
            public ILogger CreateLogger(string categoryName)
            {
                CategoriasCreadas.Add(categoryName);
                return Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
            }
            public void AddProvider(ILoggerProvider provider) { }
            public void Dispose() { }
        }
    }
}
