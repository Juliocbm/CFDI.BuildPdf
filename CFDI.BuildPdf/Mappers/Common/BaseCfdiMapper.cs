using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
        protected readonly ILogger Logger;

        protected BaseCfdiMapper(IQrGenerator qrGenerator, ILogger? logger = null)
        {
            _qrGenerator = qrGenerator ?? throw new ArgumentNullException(nameof(qrGenerator));
            Logger = logger ?? NullLogger.Instance;
        }

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
            model.FechaEmision = ParseDateOrMin(comprobante?.Attribute("Fecha")?.Value, "Comprobante.Fecha");
            model.FechaCertificacion = ParseDateOrMin(tfdNode?.Attribute("FechaTimbrado")?.Value, "TimbreFiscalDigital.FechaTimbrado");
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
            model.RfcProvCertif = tfdNode?.Attribute("RfcProvCertif")?.Value;

            // CFDI Relacionados (nodo opcional)
            var relacionadosNode = comprobante?.Element(Cfdi + "CfdiRelacionados");
            if (relacionadosNode != null)
            {
                model.TipoRelacion = relacionadosNode.Attribute("TipoRelacion")?.Value;
                model.RelacionadosUuids = relacionadosNode
                    .Elements(Cfdi + "CfdiRelacionado")
                    .Select(r => r.Attribute("UUID")?.Value)
                    .Where(u => !string.IsNullOrWhiteSpace(u))
                    .ToList();
            }
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

        /// <summary>Mapea los Conceptos del comprobante con sus impuestos a nivel concepto.</summary>
        protected static List<ConceptoViewModel> MapConceptos(XElement comprobante)
        {
            return comprobante
                .Element(Cfdi + "Conceptos")
                ?.Elements(Cfdi + "Concepto")
                .Select(c => new ConceptoViewModel
                {
                    ClaveProductoServicio = c.Attribute("ClaveProdServ")?.Value,
                    NumeroIdentificacion = c.Attribute("NoIdentificacion")?.Value,
                    Descripcion = c.Attribute("Descripcion")?.Value,
                    Cantidad = decimal.Parse(c.Attribute("Cantidad")?.Value ?? "0", CultureInfo.InvariantCulture),
                    ClaveUnidad = c.Attribute("ClaveUnidad")?.Value,
                    Unidad = c.Attribute("Unidad")?.Value,
                    ValorUnitario = decimal.Parse(c.Attribute("ValorUnitario")?.Value ?? "0", CultureInfo.InvariantCulture),
                    Importe = decimal.Parse(c.Attribute("Importe")?.Value ?? "0", CultureInfo.InvariantCulture),
                    Descuento = decimal.Parse(c.Attribute("Descuento")?.Value ?? "0", CultureInfo.InvariantCulture),
                    ObjetoImpuesto = c.Attribute("ObjetoImp")?.Value,
                    Traslados = c.Element(Cfdi + "Impuestos")?.Element(Cfdi + "Traslados")?.Elements(Cfdi + "Traslado")
                        .Select(t => new ImpuestoConceptoViewModel
                        {
                            Impuesto = t.Attribute("Impuesto")?.Value,
                            TipoFactor = t.Attribute("TipoFactor")?.Value,
                            TasaOCuota = decimal.Parse(t.Attribute("TasaOCuota")?.Value ?? "0", CultureInfo.InvariantCulture),
                            Base = decimal.Parse(t.Attribute("Base")?.Value ?? "0", CultureInfo.InvariantCulture),
                            Importe = decimal.Parse(t.Attribute("Importe")?.Value ?? "0", CultureInfo.InvariantCulture)
                        }).ToList() ?? new List<ImpuestoConceptoViewModel>(),
                    Retenciones = c.Element(Cfdi + "Impuestos")?.Element(Cfdi + "Retenciones")?.Elements(Cfdi + "Retencion")
                        .Select(r => new ImpuestoConceptoViewModel
                        {
                            Impuesto = r.Attribute("Impuesto")?.Value,
                            TipoFactor = r.Attribute("TipoFactor")?.Value,
                            TasaOCuota = decimal.Parse(r.Attribute("TasaOCuota")?.Value ?? "0", CultureInfo.InvariantCulture),
                            Base = decimal.Parse(r.Attribute("Base")?.Value ?? "0", CultureInfo.InvariantCulture),
                            Importe = decimal.Parse(r.Attribute("Importe")?.Value ?? "0", CultureInfo.InvariantCulture)
                        }).ToList() ?? new List<ImpuestoConceptoViewModel>()
                }).ToList() ?? new List<ConceptoViewModel>();
        }

        /// <summary>
        /// Mapea el resumen de impuestos a nivel comprobante (totales + desglose agrupado),
        /// con fallback que agrega desde los conceptos cuando no hay nodo global.
        /// </summary>
        protected static void MapResumenImpuestos(
            XElement comprobante,
            List<ConceptoViewModel> conceptos,
            out decimal totalTrasladados,
            out decimal totalRetenidos,
            out List<ImpuestoConceptoViewModel> trasladosResumen,
            out List<RetencionImpuestoViewModel> retencionesResumen)
        {
            var impuestosNode = comprobante.Element(Cfdi + "Impuestos");
            totalTrasladados = ParseDecimalAttr(impuestosNode?.Attribute("TotalImpuestosTrasladados"));
            totalRetenidos = ParseDecimalAttr(impuestosNode?.Attribute("TotalImpuestosRetenidos"));

            trasladosResumen = impuestosNode?.Element(Cfdi + "Traslados")?.Elements(Cfdi + "Traslado")
                .Select(t => new ImpuestoConceptoViewModel
                {
                    Impuesto = t.Attribute("Impuesto")?.Value,
                    TipoFactor = t.Attribute("TipoFactor")?.Value,
                    TasaOCuota = ParseDecimalAttr(t.Attribute("TasaOCuota")),
                    Base = ParseDecimalAttr(t.Attribute("Base")),
                    Importe = ParseDecimalAttr(t.Attribute("Importe"))
                }).ToList() ?? new List<ImpuestoConceptoViewModel>();

            retencionesResumen = impuestosNode?.Element(Cfdi + "Retenciones")?.Elements(Cfdi + "Retencion")
                .Select(r => new RetencionImpuestoViewModel
                {
                    Impuesto = r.Attribute("Impuesto")?.Value,
                    Importe = ParseDecimalAttr(r.Attribute("Importe"))
                }).ToList() ?? new List<RetencionImpuestoViewModel>();

            if (trasladosResumen.Count == 0)
            {
                trasladosResumen = conceptos
                    .SelectMany(c => c.Traslados)
                    .GroupBy(t => new { t.Impuesto, t.TipoFactor, t.TasaOCuota })
                    .Select(g => new ImpuestoConceptoViewModel
                    {
                        Impuesto = g.Key.Impuesto,
                        TipoFactor = g.Key.TipoFactor,
                        TasaOCuota = g.Key.TasaOCuota,
                        Base = g.Sum(x => x.Base),
                        Importe = g.Sum(x => x.Importe)
                    }).ToList();

                if (totalTrasladados == 0)
                    totalTrasladados = trasladosResumen.Sum(t => t.Importe);
            }

            if (retencionesResumen.Count == 0)
            {
                retencionesResumen = conceptos
                    .SelectMany(c => c.Retenciones)
                    .GroupBy(r => r.Impuesto)
                    .Select(g => new RetencionImpuestoViewModel
                    {
                        Impuesto = g.Key,
                        Importe = g.Sum(x => x.Importe)
                    }).ToList();

                if (totalRetenidos == 0)
                    totalRetenidos = retencionesResumen.Sum(r => r.Importe);
            }
        }

        /// <summary>Parsea un atributo decimal con InvariantCulture; 0 si ausente/ inválido.</summary>
        protected static decimal ParseDecimalAttr(XAttribute? attribute)
        {
            if (attribute == null || string.IsNullOrWhiteSpace(attribute.Value))
                return 0m;
            return decimal.TryParse(attribute.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0m;
        }

        #region Helpers compartidos

        private DateTime ParseDateOrMin(string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                return DateTime.MinValue;

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
                return result;

            Logger.LogWarning("No se pudo parsear la fecha del campo {Field} con valor '{Value}'. Se usará DateTime.MinValue.", fieldName, value);
            return DateTime.MinValue;
        }

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
