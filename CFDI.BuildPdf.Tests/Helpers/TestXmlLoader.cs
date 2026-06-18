using System.IO;
using System.Reflection;
using System.Xml.Linq;

namespace CFDI.BuildPdf.Tests.Helpers
{
    /// <summary>
    /// Helper para cargar XMLs de prueba desde recursos embebidos.
    /// </summary>
    internal static class TestXmlLoader
    {
        private static readonly Assembly Assembly = typeof(TestXmlLoader).Assembly;

        public static XDocument LoadCartaPorte()
            => Load("CFDI.BuildPdf.Tests.TestData.cfdi_cartaporte.xml");

        public static XDocument LoadNomina()
            => Load("CFDI.BuildPdf.Tests.TestData.cfdi_nomina.xml");

        public static XDocument LoadCartaPorteRetenciones()
            => Load("CFDI.BuildPdf.Tests.TestData.cfdi_cartaporte_retenciones.xml");

        public static XDocument LoadNominaIncapacidades()
            => Load("CFDI.BuildPdf.Tests.TestData.cfdi_nomina_incapacidades.xml");

        public static XDocument LoadFacturaIngreso()
            => Load("CFDI.BuildPdf.Tests.TestData.cfdi_factura_ingreso.xml");

        public static XDocument LoadFacturaEgreso()
            => Load("CFDI.BuildPdf.Tests.TestData.cfdi_factura_egreso.xml");

        public static XDocument Load(string resourceName)
        {
            using var stream = Assembly.GetManifestResourceStream(resourceName)
                ?? throw new FileNotFoundException($"Recurso embebido no encontrado: {resourceName}");
            return XDocument.Load(stream);
        }
    }
}
