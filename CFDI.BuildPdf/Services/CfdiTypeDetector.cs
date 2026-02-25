using System.Linq;
using System.Xml.Linq;
using CFDI.BuildPdf.Abstractions;

namespace CFDI.BuildPdf.Services
{
    /// <summary>
    /// Detecta el tipo de complemento CFDI presente en un documento XML
    /// analizando los namespaces de los elementos descendientes.
    /// </summary>
    internal class CfdiTypeDetector : ICfdiTypeDetector
    {
        private static readonly XNamespace NsCartaPorte = "http://www.sat.gob.mx/CartaPorte31";
        private static readonly XNamespace NsNomina = "http://www.sat.gob.mx/nomina12";

        /// <inheritdoc />
        public CfdiType Detect(XDocument xdoc)
        {
            var root = xdoc.Root;
            if (root == null)
                return CfdiType.Desconocido;

            if (root.Descendants(NsCartaPorte + "CartaPorte").Any())
                return CfdiType.CartaPorte;

            if (root.Descendants(NsNomina + "Nomina").Any())
                return CfdiType.Nomina;

            return CfdiType.Desconocido;
        }
    }
}
