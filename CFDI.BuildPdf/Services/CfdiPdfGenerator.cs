using System;
using System.IO;
using System.Threading.Tasks;
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
            _typeDetector = typeDetector;
            _cartaPorteMapper = cartaPorteMapper;
            _nominaMapper = nominaMapper;
            _cartaPorteBuilder = cartaPorteBuilder;
            _nominaBuilder = nominaBuilder;
        }

        /// <inheritdoc />
        public Task<byte[]> GenerarDesdeRutaAsync(string rutaXml, CfdiPdfOptions? options = null)
        {
            var xdoc = XDocument.Load(rutaXml);
            return Task.FromResult(GenerarPdfInterno(xdoc, options));
        }

        /// <inheritdoc />
        public Task<byte[]> GenerarDesdeXmlStringAsync(string xmlContent, CfdiPdfOptions? options = null)
        {
            using var reader = new StringReader(xmlContent);
            var xdoc = XDocument.Load(reader);
            return Task.FromResult(GenerarPdfInterno(xdoc, options));
        }

        /// <inheritdoc />
        public Task<byte[]> GenerarDesdeXmlBytesAsync(byte[] xmlBytes, CfdiPdfOptions? options = null)
        {
            using var ms = new MemoryStream(xmlBytes);
            var xdoc = XDocument.Load(ms);
            return Task.FromResult(GenerarPdfInterno(xdoc, options));
        }

        /// <inheritdoc />
        public Task<byte[]> GenerarDesdeStreamAsync(Stream xmlStream, CfdiPdfOptions? options = null)
        {
            var xdoc = XDocument.Load(xmlStream);
            return Task.FromResult(GenerarPdfInterno(xdoc, options));
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
                _ => throw new NotSupportedException($"Tipo de CFDI no soportado: {tipo}")
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
