using System.Collections.Generic;
using System.Threading.Tasks;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Configuration;
using CFDI.BuildPdf.Tests.Helpers;
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
