using System;
using System.IO;
using System.Threading.Tasks;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Configuration;
using Microsoft.Extensions.Logging;
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
        private static CfdiPdfLicenseType _licenseType = CfdiPdfLicenseType.Community;
        private static ILoggerFactory? _loggerFactory;

        private static readonly Lazy<ICfdiPdfGenerator> _generator = new(() =>
        {
            // Idempotente: no pisar una licencia ya configurada explícitamente.
            if (QuestPDF.Settings.License is null)
                QuestPDF.Settings.License = MapLicense(_licenseType);

            return CfdiPdfFactory.CreateGenerator(_loggerFactory);
        });

        private static ICfdiPdfGenerator Generator => _generator.Value;

        /// <summary>
        /// Configura el tipo de licencia QuestPDF antes del primer uso de la fachada.
        /// Debe llamarse una sola vez al inicio del proceso (por ejemplo, en <c>Program.cs</c> o <c>Startup</c>).
        /// Si nunca se llama, se usa <see cref="CfdiPdfLicenseType.Community"/>.
        /// </summary>
        /// <param name="licenseType">Tipo de licencia QuestPDF que tu organización ha adquirido.</param>
        /// <remarks>
        /// QuestPDF es una librería con licencia dual (Community gratuita y Pro/Enterprise de pago).
        /// El consumidor final es responsable de adquirir la licencia correcta. Consulta
        /// https://www.questpdf.com/license/ para determinar cuál aplica a tu empresa.
        /// </remarks>
        public static void ConfigureQuestPdfLicense(CfdiPdfLicenseType licenseType)
        {
            _licenseType = licenseType;
            // Intención explícita del consumidor: aplicar de inmediato (puede sobre-escribir el default idempotente).
            QuestPDF.Settings.License = MapLicense(licenseType);
        }

        /// <summary>
        /// Configura el <see cref="ILoggerFactory"/> para el diagnóstico de los mappers en el camino de la fachada estática.
        /// Debe llamarse una sola vez al inicio del proceso, antes del primer uso. Si no se llama, no se emite logging.
        /// </summary>
        /// <param name="loggerFactory">Factory de loggers de tu aplicación.</param>
        public static void ConfigureLogging(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        internal static LicenseType MapLicense(CfdiPdfLicenseType licenseType) => licenseType switch
        {
            CfdiPdfLicenseType.Professional => LicenseType.Professional,
            CfdiPdfLicenseType.Enterprise => LicenseType.Enterprise,
            _ => LicenseType.Community
        };

        #region Desde ruta de archivo

        /// <summary>
        /// Genera un PDF a partir de un archivo XML en una ruta física.
        /// Detecta automáticamente el tipo de complemento.
        /// </summary>
        /// <param name="rutaXml">Ruta completa del archivo XML.</param>
        /// <param name="options">Opciones de generación (opcional).</param>
        /// <returns>PDF generado como arreglo de bytes.</returns>
        /// <exception cref="ArgumentException">Si <paramref name="rutaXml"/> es nula o vacía.</exception>
        /// <exception cref="FileNotFoundException">Si el archivo indicado no existe.</exception>
        /// <exception cref="CfdiXmlInvalidoException">Si el contenido no es un XML válido.</exception>
        /// <exception cref="CfdiComplementoNoSoportadoException">Si el CFDI no contiene un complemento soportado.</exception>
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
        /// <exception cref="ArgumentException">Si <paramref name="xmlContent"/> es nulo o vacío.</exception>
        /// <exception cref="CfdiXmlInvalidoException">Si el contenido no es un XML válido.</exception>
        /// <exception cref="CfdiComplementoNoSoportadoException">Si el CFDI no contiene un complemento soportado.</exception>
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
        /// <exception cref="ArgumentNullException">Si <paramref name="xmlBytes"/> es nulo.</exception>
        /// <exception cref="ArgumentException">Si <paramref name="xmlBytes"/> está vacío.</exception>
        /// <exception cref="CfdiXmlInvalidoException">Si el contenido no es un XML válido.</exception>
        /// <exception cref="CfdiComplementoNoSoportadoException">Si el CFDI no contiene un complemento soportado.</exception>
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
        /// <exception cref="ArgumentNullException">Si <paramref name="xmlStream"/> es nulo.</exception>
        /// <exception cref="ArgumentException">Si el Stream no es legible.</exception>
        /// <exception cref="CfdiXmlInvalidoException">Si el contenido no es un XML válido.</exception>
        /// <exception cref="CfdiComplementoNoSoportadoException">Si el CFDI no contiene un complemento soportado.</exception>
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
            if (string.IsNullOrWhiteSpace(rutaPdfDestino))
                throw new ArgumentException("La ruta del PDF de destino no puede ser nula ni vacía.", nameof(rutaPdfDestino));

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
            if (outputStream is null)
                throw new ArgumentNullException(nameof(outputStream));
            if (!outputStream.CanWrite)
                throw new ArgumentException("El Stream de salida no es escribible.", nameof(outputStream));

            var pdfBytes = await DesdeStreamAsync(xmlStream, options);
            await outputStream.WriteAsync(pdfBytes, 0, pdfBytes.Length);
        }

        #endregion
    }
}
