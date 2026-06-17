using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Complements;

namespace CFDI.BuildPdf.Services
{
    /// <summary>
    /// Orquestador principal de generación de PDF desde XML CFDI.
    /// Detecta el complemento por namespace y delega en el <see cref="ICfdiComplementHandler"/> correspondiente.
    /// </summary>
    internal class CfdiPdfGenerator : ICfdiPdfGenerator
    {
        private readonly IReadOnlyList<ICfdiComplementHandler> _handlers;

        public CfdiPdfGenerator(IEnumerable<ICfdiComplementHandler> handlers)
        {
            if (handlers is null)
                throw new ArgumentNullException(nameof(handlers));

            _handlers = handlers.OrderByDescending(h => h.Priority).ToList();

            // Unicidad: dos handlers no pueden declarar el mismo namespace de complemento.
            var seen = new HashSet<string>();
            foreach (var provider in _handlers.OfType<IComplementNamespacesProvider>())
                foreach (var ns in provider.ComplementNamespaces)
                    if (!seen.Add(ns))
                        throw new InvalidOperationException(
                            $"Más de un handler declara el namespace de complemento '{ns}'.");
        }

        /// <inheritdoc />
        public async Task<byte[]> GenerarDesdeRutaAsync(string rutaXml, CfdiPdfOptions? options = null)
        {
            if (string.IsNullOrWhiteSpace(rutaXml))
                throw new ArgumentException("La ruta del XML no puede ser nula ni vacía.", nameof(rutaXml));
            if (!File.Exists(rutaXml))
                throw new FileNotFoundException($"No se encontró el archivo XML en la ruta especificada: {rutaXml}", rutaXml);

            await using var stream = new FileStream(rutaXml, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            var xdoc = await LoadXDocumentAsync(() => XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None), $"archivo '{rutaXml}'");
            return GenerarPdfInterno(xdoc, options);
        }

        /// <inheritdoc />
        public async Task<byte[]> GenerarDesdeXmlStringAsync(string xmlContent, CfdiPdfOptions? options = null)
        {
            if (string.IsNullOrWhiteSpace(xmlContent))
                throw new ArgumentException("El contenido XML no puede ser nulo ni vacío.", nameof(xmlContent));

            using var reader = new StringReader(xmlContent);
            var xdoc = await LoadXDocumentAsync(() => XDocument.LoadAsync(reader, LoadOptions.None, CancellationToken.None), "cadena XML");
            return GenerarPdfInterno(xdoc, options);
        }

        /// <inheritdoc />
        public async Task<byte[]> GenerarDesdeXmlBytesAsync(byte[] xmlBytes, CfdiPdfOptions? options = null)
        {
            if (xmlBytes is null)
                throw new ArgumentNullException(nameof(xmlBytes));
            if (xmlBytes.Length == 0)
                throw new ArgumentException("El arreglo de bytes del XML está vacío.", nameof(xmlBytes));

            using var ms = new MemoryStream(xmlBytes);
            var xdoc = await LoadXDocumentAsync(() => XDocument.LoadAsync(ms, LoadOptions.None, CancellationToken.None), "arreglo de bytes XML");
            return GenerarPdfInterno(xdoc, options);
        }

        /// <inheritdoc />
        public async Task<byte[]> GenerarDesdeStreamAsync(Stream xmlStream, CfdiPdfOptions? options = null)
        {
            if (xmlStream is null)
                throw new ArgumentNullException(nameof(xmlStream));
            if (!xmlStream.CanRead)
                throw new ArgumentException("El Stream proporcionado no es legible.", nameof(xmlStream));

            var xdoc = await LoadXDocumentAsync(() => XDocument.LoadAsync(xmlStream, LoadOptions.None, CancellationToken.None), "Stream XML");
            return GenerarPdfInterno(xdoc, options);
        }

        private static async Task<XDocument> LoadXDocumentAsync(Func<Task<XDocument>> loader, string origen)
        {
            try
            {
                return await loader();
            }
            catch (XmlException ex)
            {
                throw new CfdiXmlInvalidoException(
                    $"El {origen} no contiene un XML válido: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Lógica central: selecciona el handler cuyo namespace de complemento esté presente y delega.
        /// </summary>
        private byte[] GenerarPdfInterno(XDocument xdoc, CfdiPdfOptions? options)
        {
            var opts = options ?? new CfdiPdfOptions();
            var handler = ResolveHandler(xdoc);

            if (handler is null)
                throw new CfdiComplementoNoSoportadoException(
                    "Tipo de CFDI no soportado. Actualmente la librería solo soporta Carta Porte 3.1 y Nómina 1.2.");

            return handler.Generate(xdoc, opts);
        }

        /// <summary>
        /// Devuelve el handler de mayor prioridad que declara poder procesar el documento.
        /// </summary>
        private ICfdiComplementHandler? ResolveHandler(XDocument xdoc)
        {
            // _handlers ya está ordenado por Priority descendente.
            return _handlers.FirstOrDefault(h => h.CanHandle(xdoc));
        }
    }
}
