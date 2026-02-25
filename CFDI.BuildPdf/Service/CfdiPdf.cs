using System;
using System.IO;
using System.Threading.Tasks;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Helpers;
using CFDI.BuildPdf.Mappers.CartaPorte;
using CFDI.BuildPdf.Mappers.Nomina;
using CFDI.BuildPdf.PdfBuilders.CartaPorte;
using CFDI.BuildPdf.PdfBuilders.Nomina;
using CFDI.BuildPdf.Services;
using QuestPDF.Infrastructure;

namespace CFDI.BuildPdf.Service
{
    /// <summary>
    /// Fachada estática para generar PDFs desde XMLs CFDI 4.0.
    /// Detecta automáticamente el tipo de complemento (Carta Porte, Nómina).
    /// Para escenarios con DI, usar <see cref="Configuration.ServiceCollectionExtensions.AddCfdiPdfServices"/>.
    /// </summary>
    public static class CfdiPdf
    {
        private static readonly Lazy<ICfdiPdfGenerator> _generator = new(() =>
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var qrGenerator = new QrGeneratorService();
            return new CfdiPdfGenerator(
                new CfdiTypeDetector(),
                new CartaPorteMapper(qrGenerator),
                new NominaMapper(qrGenerator),
                new CartaPorteDocumentBuilder(),
                new NominaDocumentBuilder()
            );
        });

        private static ICfdiPdfGenerator Generator => _generator.Value;

        #region Desde ruta de archivo

        /// <summary>
        /// Genera un PDF a partir de un archivo XML en una ruta física.
        /// Detecta automáticamente el tipo de complemento.
        /// </summary>
        /// <param name="rutaXml">Ruta completa del archivo XML.</param>
        /// <param name="options">Opciones de generación (opcional).</param>
        /// <returns>PDF generado como arreglo de bytes.</returns>
        public static Task<byte[]> DesdeRutaAsync(string rutaXml, CfdiPdfOptions? options = null)
        {
            return Generator.GenerarDesdeRutaAsync(rutaXml, options);
        }

        #endregion

        #region Desde contenido XML string

        /// <summary>
        /// Genera un PDF a partir de una cadena con contenido XML.
        /// Detecta automáticamente el tipo de complemento.
        /// </summary>
        /// <param name="xmlContent">Contenido del XML en texto plano.</param>
        /// <param name="options">Opciones de generación (opcional).</param>
        /// <returns>PDF generado como arreglo de bytes.</returns>
        public static Task<byte[]> DesdeXmlStringAsync(string xmlContent, CfdiPdfOptions? options = null)
        {
            return Generator.GenerarDesdeXmlStringAsync(xmlContent, options);
        }

        #endregion

        #region Desde bytes

        /// <summary>
        /// Genera un PDF a partir del XML como arreglo de bytes.
        /// Detecta automáticamente el tipo de complemento.
        /// </summary>
        /// <param name="xmlBytes">Contenido del archivo XML en bytes.</param>
        /// <param name="options">Opciones de generación (opcional).</param>
        /// <returns>PDF generado como arreglo de bytes.</returns>
        public static Task<byte[]> DesdeXmlBytesAsync(byte[] xmlBytes, CfdiPdfOptions? options = null)
        {
            return Generator.GenerarDesdeXmlBytesAsync(xmlBytes, options);
        }

        #endregion

        #region Desde Stream

        /// <summary>
        /// Genera un PDF a partir de un Stream con contenido XML.
        /// Ideal para escenarios web/API donde el XML llega como Stream.
        /// </summary>
        /// <param name="xmlStream">Stream con el contenido del XML.</param>
        /// <param name="options">Opciones de generación (opcional).</param>
        /// <returns>PDF generado como arreglo de bytes.</returns>
        public static Task<byte[]> DesdeStreamAsync(Stream xmlStream, CfdiPdfOptions? options = null)
        {
            return Generator.GenerarDesdeStreamAsync(xmlStream, options);
        }

        #endregion

        #region Conveniencia: guardar a archivo

        /// <summary>
        /// Genera un PDF desde un archivo XML y lo guarda directamente en la ruta de destino.
        /// </summary>
        /// <param name="rutaXml">Ruta completa del archivo XML de entrada.</param>
        /// <param name="rutaPdfDestino">Ruta donde se guardará el PDF generado.</param>
        /// <param name="options">Opciones de generación (opcional).</param>
        public static async Task GuardarDesdeRutaAsync(string rutaXml, string rutaPdfDestino, CfdiPdfOptions? options = null)
        {
            var pdfBytes = await DesdeRutaAsync(rutaXml, options);
            await File.WriteAllBytesAsync(rutaPdfDestino, pdfBytes);
        }

        /// <summary>
        /// Genera un PDF desde un Stream XML y lo escribe en un Stream de salida.
        /// Ideal para retornar el PDF directamente en una respuesta HTTP.
        /// </summary>
        /// <param name="xmlStream">Stream con el contenido del XML.</param>
        /// <param name="outputStream">Stream de destino donde se escribirá el PDF.</param>
        /// <param name="options">Opciones de generación (opcional).</param>
        public static async Task EscribirEnStreamAsync(Stream xmlStream, Stream outputStream, CfdiPdfOptions? options = null)
        {
            var pdfBytes = await DesdeStreamAsync(xmlStream, options);
            await outputStream.WriteAsync(pdfBytes, 0, pdfBytes.Length);
        }

        #endregion
    }
}
