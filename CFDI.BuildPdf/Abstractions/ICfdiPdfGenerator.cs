using System.IO;
using System.Threading.Tasks;

namespace CFDI.BuildPdf.Abstractions
{
    /// <summary>
    /// Contrato público de alto nivel para generar PDFs desde XMLs CFDI.
    /// Soporta múltiples formatos de entrada (ruta, string, bytes, Stream).
    /// </summary>
    public interface ICfdiPdfGenerator
    {
        /// <summary>
        /// Genera un PDF a partir de un archivo XML en una ruta física.
        /// </summary>
        /// <param name="rutaXml">Ruta completa del archivo XML.</param>
        /// <param name="options">Opciones de generación.</param>
        /// <returns>PDF generado en forma de arreglo de bytes.</returns>
        Task<byte[]> GenerarDesdeRutaAsync(string rutaXml, CfdiPdfOptions? options = null);

        /// <summary>
        /// Genera un PDF a partir de una cadena XML.
        /// </summary>
        /// <param name="xmlContent">Contenido del XML en texto plano.</param>
        /// <param name="options">Opciones de generación.</param>
        /// <returns>PDF generado en forma de arreglo de bytes.</returns>
        Task<byte[]> GenerarDesdeXmlStringAsync(string xmlContent, CfdiPdfOptions? options = null);

        /// <summary>
        /// Genera un PDF a partir de un arreglo de bytes del XML.
        /// </summary>
        /// <param name="xmlBytes">Contenido del archivo XML en bytes.</param>
        /// <param name="options">Opciones de generación.</param>
        /// <returns>PDF generado en forma de arreglo de bytes.</returns>
        Task<byte[]> GenerarDesdeXmlBytesAsync(byte[] xmlBytes, CfdiPdfOptions? options = null);

        /// <summary>
        /// Genera un PDF a partir de un Stream que contiene el XML.
        /// Ideal para escenarios web/API donde el XML llega como Stream.
        /// </summary>
        /// <param name="xmlStream">Stream con el contenido del XML.</param>
        /// <param name="options">Opciones de generación.</param>
        /// <returns>PDF generado en forma de arreglo de bytes.</returns>
        Task<byte[]> GenerarDesdeStreamAsync(Stream xmlStream, CfdiPdfOptions? options = null);
    }
}
