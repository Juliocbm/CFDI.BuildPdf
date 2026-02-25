using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Models;

namespace CFDI.BuildPdf.Mappers.Common
{
    /// <summary>
    /// Clase base que encapsula el mapeo común de cualquier CFDI 4.0 (Template Method).
    /// Las subclases implementan únicamente el mapeo específico de su complemento.
    /// </summary>
    /// <typeparam name="TModel">Tipo del ViewModel destino, debe heredar de <see cref="CfdiViewModelBase"/>.</typeparam>
    internal abstract class BaseCfdiMapper<TModel> : ICfdiModelMapper<TModel>
        where TModel : CfdiViewModelBase
    {
        protected static readonly XNamespace Cfdi = "http://www.sat.gob.mx/cfd/4";
        protected static readonly XNamespace Tfd = "http://www.sat.gob.mx/TimbreFiscalDigital";

        private readonly IQrGenerator _qrGenerator;

        protected BaseCfdiMapper(IQrGenerator qrGenerator)
        {
            _qrGenerator = qrGenerator;
        }

        /// <inheritdoc />
        public abstract bool CanMap(XDocument xdoc);

        /// <inheritdoc />
        public TModel Map(XDocument xdoc)
        {
            var model = CreateModel();
            MapComprobanteBase(xdoc, model);
            MapComplemento(xdoc, model);
            MapQr(xdoc, model);
            return model;
        }

        /// <summary>
        /// Crea una instancia vacía del ViewModel específico del complemento.
        /// </summary>
        protected abstract TModel CreateModel();

        /// <summary>
        /// Mapea los datos específicos del complemento (Carta Porte, Nómina, etc.).
        /// </summary>
        protected abstract void MapComplemento(XDocument xdoc, TModel model);

        /// <summary>
        /// Mapea las propiedades comunes de cualquier CFDI 4.0:
        /// comprobante, emisor, receptor, totales, sellos y cadena original.
        /// </summary>
        protected virtual void MapComprobanteBase(XDocument xdoc, TModel model)
        {
            var comprobante = xdoc.Root;
            var tfdNode = comprobante.Descendants(Tfd + "TimbreFiscalDigital").FirstOrDefault();

            // Cadena original del timbre
            model.CadenaOriginalSAT = tfdNode != null
                ? $"||{tfdNode.Attribute("Version")?.Value}|{tfdNode.Attribute("UUID")?.Value}|{tfdNode.Attribute("FechaTimbrado")?.Value}|{tfdNode.Attribute("RfcProvCertif")?.Value}|{tfdNode.Attribute("SelloCFD")?.Value}|{tfdNode.Attribute("NoCertificadoSAT")?.Value}||"
                : string.Empty;

            // Comprobante
            model.Version = comprobante?.Attribute("Version")?.Value;
            model.Serie = comprobante?.Attribute("Serie")?.Value;
            model.Folio = comprobante?.Attribute("Folio")?.Value;
            model.LugarExpedicion = comprobante?.Attribute("LugarExpedicion")?.Value;
            model.FechaEmision = DateTime.Parse(comprobante?.Attribute("Fecha")?.Value ?? DateTime.MinValue.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
            model.FechaCertificacion = tfdNode != null && tfdNode.Attribute("FechaTimbrado") != null
                ? DateTime.Parse(tfdNode.Attribute("FechaTimbrado").Value, CultureInfo.InvariantCulture)
                : DateTime.MinValue;
            model.TipoCambio = comprobante?.Attribute("TipoCambio")?.Value;
            model.Moneda = comprobante?.Attribute("Moneda")?.Value;
            model.FormaPago = comprobante?.Attribute("FormaPago")?.Value;
            model.MetodoPago = comprobante?.Attribute("MetodoPago")?.Value;
            model.TipoComprobante = comprobante?.Attribute("TipoDeComprobante")?.Value;
            model.Exportacion = comprobante?.Attribute("Exportacion")?.Value;

            // Emisor
            var emisorNode = comprobante.Element(Cfdi + "Emisor");
            model.EmisorNombre = emisorNode?.Attribute("Nombre")?.Value;
            model.EmisorRFC = emisorNode?.Attribute("Rfc")?.Value;
            model.EmisorRegimenFiscal = emisorNode?.Attribute("RegimenFiscal")?.Value;

            // Receptor
            var receptorNode = comprobante.Element(Cfdi + "Receptor");
            model.ReceptorNombre = receptorNode?.Attribute("Nombre")?.Value;
            model.ReceptorRFC = receptorNode?.Attribute("Rfc")?.Value;
            model.ReceptorDomicilioFiscal = receptorNode?.Attribute("DomicilioFiscalReceptor")?.Value;
            model.ReceptorRegimenFiscal = receptorNode?.Attribute("RegimenFiscalReceptor")?.Value;
            model.UsoCFDI = receptorNode?.Attribute("UsoCFDI")?.Value;

            // Totales
            model.SubTotal = decimal.Parse(comprobante?.Attribute("SubTotal")?.Value ?? "0", CultureInfo.InvariantCulture);
            model.Total = decimal.Parse(comprobante?.Attribute("Total")?.Value ?? "0", CultureInfo.InvariantCulture);
            model.CantidadConLetra = NumberToWordsConverter.Convertir(model.Total, model.Moneda);

            // Sellos y TFD
            model.SelloEmisor = comprobante?.Attribute("Sello")?.Value;
            model.NoCertificadoEmisor = comprobante?.Attribute("NoCertificado")?.Value;
            model.UUID = tfdNode?.Attribute("UUID")?.Value;
            model.NoCertificadoSAT = tfdNode?.Attribute("NoCertificadoSAT")?.Value;
            model.SelloSAT = tfdNode?.Attribute("SelloSAT")?.Value;
        }

        /// <summary>
        /// Genera la URL de verificación y el código QR en Base64.
        /// </summary>
        private void MapQr(XDocument xdoc, TModel model)
        {
            model.UrlQr = QrUrlBuilder.Construir(
                model.UUID,
                model.EmisorRFC,
                model.ReceptorRFC,
                model.Total,
                model.SelloEmisor ?? string.Empty
            );
            model.QRCodeBase64 = _qrGenerator.GenerateBase64(model.UrlQr);
        }

        #region Helpers compartidos

        protected static decimal? GetDecimalOrNull(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                return result;
            return null;
        }

        protected static DateTime? GetDateOrNull(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
                return result;
            return null;
        }

        #endregion
    }
}
