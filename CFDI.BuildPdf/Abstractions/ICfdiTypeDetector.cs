using System.Xml.Linq;

namespace CFDI.BuildPdf.Abstractions
{
    /// <summary>
    /// Contrato para detectar el tipo de complemento CFDI presente en un documento XML.
    /// </summary>
    internal interface ICfdiTypeDetector
    {
        /// <summary>
        /// Detecta el tipo de complemento contenido en el XDocument.
        /// </summary>
        /// <param name="xdoc">Documento XML CFDI a analizar.</param>
        /// <returns>El tipo de complemento detectado.</returns>
        CfdiType Detect(XDocument xdoc);
    }
}
