using System.Xml.Linq;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Services;
using CFDI.BuildPdf.Tests.Helpers;

namespace CFDI.BuildPdf.Tests
{
    public class CfdiTypeDetectorTests
    {
        private readonly CfdiTypeDetector _detector = new();

        [Fact]
        public void Detect_CartaPorteXml_RetornaCartaPorte()
        {
            var xdoc = TestXmlLoader.LoadCartaPorte();
            Assert.Equal(CfdiType.CartaPorte, _detector.Detect(xdoc));
        }

        [Fact]
        public void Detect_NominaXml_RetornaNomina()
        {
            var xdoc = TestXmlLoader.LoadNomina();
            Assert.Equal(CfdiType.Nomina, _detector.Detect(xdoc));
        }

        [Fact]
        public void Detect_XmlSinComplemento_RetornaDesconocido()
        {
            var xdoc = XDocument.Parse(@"<cfdi:Comprobante xmlns:cfdi='http://www.sat.gob.mx/cfd/4' Version='4.0' />");
            Assert.Equal(CfdiType.Desconocido, _detector.Detect(xdoc));
        }

        [Fact]
        public void Detect_XmlConRootNull_RetornaDesconocido()
        {
            var xdoc = new XDocument();
            Assert.Equal(CfdiType.Desconocido, _detector.Detect(xdoc));
        }
    }
}
