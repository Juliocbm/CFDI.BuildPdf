using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Models;

namespace CFDI.BuildPdf.Services
{
    /// <summary>
    /// Orquestador principal de generación de PDF desde XML CFDI.
    /// Implementa <see cref="ICfdiPdfGenerator"/> usando inyección de dependencias.
    /// Detecta automáticamente el tipo de complemento y delega al mapper/builder correspondiente.
    /// </summary>
    internal class CfdiPdfGenerator : ICfdiPdfGenerator
    {
        private readonly ICfdiTypeDetector _typeDetector;
        private readonly ICfdiModelMapper<CfdiCartaPorteViewModel> _cartaPorteMapper;
        private readonly ICfdiModelMapper<CfdiNominaViewModel> _nominaMapper;
        private readonly IPdfDocumentBuilder<CfdiCartaPorteViewModel> _cartaPorteBuilder;
        private readonly IPdfDocumentBuilder<CfdiNominaViewModel> _nominaBuilder;

        public CfdiPdfGenerator(
            ICfdiTypeDetector typeDetector,
            ICfdiModelMapper<CfdiCartaPorteViewModel> cartaPorteMapper,
            ICfdiModelMapper<CfdiNominaViewModel> nominaMapper,
            IPdfDocumentBuilder<CfdiCartaPorteViewModel> cartaPorteBuilder,
            IPdfDocumentBuilder<CfdiNominaViewModel> nominaBuilder)
        {
            _typeDetector = typeDetector ?? throw new ArgumentNullException(nameof(typeDetector));
            _cartaPorteMapper = cartaPorteMapper ?? throw new ArgumentNullException(nameof(cartaPorteMapper));
            _nominaMapper = nominaMapper ?? throw new ArgumentNullException(nameof(nominaMapper));
            _cartaPorteBuilder = cartaPorteBuilder ?? throw new ArgumentNullException(nameof(cartaPorteBuilder));
            _nominaBuilder = nominaBuilder ?? throw new ArgumentNullException(nameof(nominaBuilder));
        }

        /// <inheritdoc />
        public Task<byte[]> GenerarDesdeRutaAsync(string rutaXml, CfdiPdfOptions? options = null)
        {
            if (string.IsNullOrWhiteSpace(rutaXml))
                throw new ArgumentException("La ruta del XML no puede ser nula ni vacía.", nameof(rutaXml));
            if (!File.Exists(rutaXml))
                throw new FileNotFoundException($"No se encontró el archivo XML en la ruta especificada: {rutaXml}", rutaXml);

            var xdoc = LoadXDocument(() => XDocument.Load(rutaXml), $"archivo '{rutaXml}'");
            return Task.FromResult(GenerarPdfInterno(xdoc, options));
        }

        /// <inheritdoc />
        public Task<byte[]> GenerarDesdeXmlStringAsync(string xmlContent, CfdiPdfOptions? options = null)
        {
            if (string.IsNullOrWhiteSpace(xmlContent))
                throw new ArgumentException("El contenido XML no puede ser nulo ni vacío.", nameof(xmlContent));

            var xdoc = LoadXDocument(() =>
            {
                using var reader = new StringReader(xmlContent);
                return XDocument.Load(reader);
            }, "cadena XML");

            return Task.FromResult(GenerarPdfInterno(xdoc, options));
        }

        /// <inheritdoc />
        public Task<byte[]> GenerarDesdeXmlBytesAsync(byte[] xmlBytes, CfdiPdfOptions? options = null)
        {
            if (xmlBytes is null)
                throw new ArgumentNullException(nameof(xmlBytes));
            if (xmlBytes.Length == 0)
                throw new ArgumentException("El arreglo de bytes del XML está vacío.", nameof(xmlBytes));

            var xdoc = LoadXDocument(() =>
            {
                using var ms = new MemoryStream(xmlBytes);
                return XDocument.Load(ms);
            }, "arreglo de bytes XML");

            return Task.FromResult(GenerarPdfInterno(xdoc, options));
        }

        /// <inheritdoc />
        public Task<byte[]> GenerarDesdeStreamAsync(Stream xmlStream, CfdiPdfOptions? options = null)
        {
            if (xmlStream is null)
                throw new ArgumentNullException(nameof(xmlStream));
            if (!xmlStream.CanRead)
                throw new ArgumentException("El Stream proporcionado no es legible.", nameof(xmlStream));

            var xdoc = LoadXDocument(() => XDocument.Load(xmlStream), "Stream XML");
            return Task.FromResult(GenerarPdfInterno(xdoc, options));
        }

        private static XDocument LoadXDocument(Func<XDocument> loader, string origen)
        {
            try
            {
                return loader();
            }
            catch (XmlException ex)
            {
                throw new CfdiXmlInvalidoException(
                    $"El {origen} no contiene un XML válido: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Lógica central: detecta tipo de complemento, mapea y genera el PDF directamente.
        /// </summary>
        private byte[] GenerarPdfInterno(XDocument xdoc, CfdiPdfOptions? options)
        {
            var opts = options ?? new CfdiPdfOptions();
            var tipo = _typeDetector.Detect(xdoc);

            return tipo switch
            {
                CfdiType.CartaPorte => GenerarCartaPortePdf(xdoc, opts),
                CfdiType.Nomina => GenerarNominaPdf(xdoc, opts),
                _ => throw new CfdiComplementoNoSoportadoException(
                    $"Tipo de CFDI no soportado: {tipo}. Actualmente la librería solo soporta Carta Porte 3.1 y Nómina 1.2.")
            };
        }

        private byte[] GenerarCartaPortePdf(XDocument xdoc, CfdiPdfOptions opts)
        {
            var model = _cartaPorteMapper.Map(xdoc);

            if (!string.IsNullOrEmpty(opts.LogoBase64))
                model.LogoBase64 = opts.LogoBase64;

            return _cartaPorteBuilder.Build(model, opts);
        }

        private byte[] GenerarNominaPdf(XDocument xdoc, CfdiPdfOptions opts)
        {
            var model = _nominaMapper.Map(xdoc);

            if (!string.IsNullOrEmpty(opts.LogoBase64))
                model.LogoBase64 = opts.LogoBase64;

            return _nominaBuilder.Build(model, opts);
        }
    }
}
