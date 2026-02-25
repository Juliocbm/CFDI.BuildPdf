using CFDI.BuildPdf.Mappers.Common;

namespace CFDI.BuildPdf.Tests
{
    public class QrUrlBuilderTests
    {
        [Fact]
        public void Construir_ConDatosValidos_GeneraUrlConFormatoCorrecto()
        {
            var url = QrUrlBuilder.Construir(
                "AAB12345-ABCD-1234-EFGH-123456789012",
                "EDE010101AAA",
                "CPR020202BBB",
                1500.00m,
                "ABCDEFGHIJKLMNOP1234567890abcdef");

            Assert.Contains("id=AAB12345-ABCD-1234-EFGH-123456789012", url);
            Assert.Contains("re=EDE010101AAA", url);
            Assert.Contains("rr=CPR020202BBB", url);
            Assert.Contains("fe=90abcdef", url);
            Assert.StartsWith("https://verificacfdi.facturaelectronica.sat.gob.mx/", url);
        }

        [Fact]
        public void Construir_TotalCero_FormatoCorrecto()
        {
            var url = QrUrlBuilder.Construir("UUID", "RFC1", "RFC2", 0m, "12345678");

            Assert.Contains("tt=000000000000000000.000000", url);
        }

        [Fact]
        public void Construir_SelloCorto_UsaSelloCompleto()
        {
            var url = QrUrlBuilder.Construir("UUID", "RFC1", "RFC2", 100m, "ABCD");

            Assert.Contains("fe=ABCD", url);
        }

        [Fact]
        public void Construir_SelloNull_FeVacio()
        {
            var url = QrUrlBuilder.Construir("UUID", "RFC1", "RFC2", 100m, null);

            Assert.Contains("fe=", url);
        }
    }
}
