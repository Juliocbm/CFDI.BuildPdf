using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFDI.BuildPdf.Service
{
    /// <summary>
    /// Clase utilitaria para generar un PDF de CFDI + Complemento Carta Porte.
    /// Expone métodos estáticos que permiten crear un PDF desde distintos formatos de entrada.
    /// </summary>
    public static class CfdiPdf
    {
        private static readonly PdfService _pdfService = new PdfService();

        /// <summary>
        /// Genera un PDF a partir de un archivo XML en una ruta física.
        /// </summary>
        /// <param name="rutaXml">Ruta completa del archivo XML.</param>
        /// <param name="mostrarMercancias">Si se desea mostrar el detalle de mercancías (true por defecto).</param>
        /// <param name="logoBase64">Cadena base64 del logo de la empresa (opcional).</param>
        /// <returns>PDF generado en forma de arreglo de bytes.</returns>
        public static Task<byte[]> DesdeRutaAsync(string rutaXml, bool mostrarMercancias = true, string? logoBase64 = null)
        {
            return _pdfService.GenerarPdfDesdeXmlAsync(rutaXml, mostrarMercancias, logoBase64);
        }

        /// <summary>
        /// Genera un PDF a partir de una cadena XML.
        /// </summary>
        /// <param name="xmlContent">Contenido del XML en texto plano.</param>
        /// <param name="esContenidoXml">Si es true, se trata como contenido XML (no como ruta).</param>
        /// <param name="mostrarMercancias">Si se desea mostrar el detalle de mercancías (true por defecto).</param>
        /// <param name="logoBase64">Cadena base64 del logo de la empresa (opcional).</param>
        /// <returns>PDF generado en forma de arreglo de bytes.</returns>
        public static Task<byte[]> DesdeXmlStringAsync(string xmlContent, bool esContenidoXml, bool mostrarMercancias = true, string? logoBase64 = null)
        {
            return _pdfService.GenerarPdfDesdeXmlAsync(xmlContent, esContenidoXml, mostrarMercancias, logoBase64);
        }

        /// <summary>
        /// Genera un PDF a partir de un archivo XML como arreglo de bytes.
        /// </summary>
        /// <param name="xmlBytes">Contenido del archivo XML en bytes.</param>
        /// <param name="mostrarMercancias">Si se desea mostrar el detalle de mercancías (true por defecto).</param>
        /// <param name="logoBase64">Cadena base64 del logo de la empresa (opcional).</param>
        /// <returns>PDF generado en forma de arreglo de bytes.</returns>
        public static Task<byte[]> DesdeXmlBytesAsync(byte[] xmlBytes, bool mostrarMercancias = true, string? logoBase64 = null)
        {
            return _pdfService.GenerarPdfDesdeXmlAsync(xmlBytes, mostrarMercancias, logoBase64);
        }
    }
}
