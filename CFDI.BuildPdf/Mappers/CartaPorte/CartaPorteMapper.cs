using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Mappers.Common;
using CFDI.BuildPdf.Models;
using Microsoft.Extensions.Logging;

namespace CFDI.BuildPdf.Mappers.CartaPorte
{
    /// <summary>
    /// Mapper de CFDI 4.0 con complemento Carta Porte 3.1.
    /// Hereda lógica común de <see cref="BaseCfdiMapper{TModel}"/> (Template Method).
    /// </summary>
    internal class CartaPorteMapper : BaseCfdiMapper<CfdiCartaPorteViewModel>
    {
        private static readonly XNamespace Cp = "http://www.sat.gob.mx/CartaPorte31";

        public CartaPorteMapper(IQrGenerator qrGenerator, ILogger<CartaPorteMapper>? logger = null)
            : base(qrGenerator, logger) { }

        /// <inheritdoc />
        protected override CfdiCartaPorteViewModel CreateModel() => new();

        /// <inheritdoc />
        protected override void MapComplemento(XDocument xdoc, CfdiCartaPorteViewModel model)
        {
            var comprobante = xdoc.Root;

            // Propiedades específicas de Carta Porte
            model.CondicionesPago = comprobante?.Attribute("CondicionesDePago")?.Value;

            // Conceptos con impuestos a nivel concepto (mapeo compartido)
            model.Conceptos = MapConceptos(comprobante);

            // Resumen de impuestos a nivel comprobante (mapeo compartido, con fallback)
            MapResumenImpuestos(comprobante, model.Conceptos,
                out var totalTras, out var totalRet,
                out var trasResumen, out var retResumen);
            model.TotalImpuestosTrasladados = totalTras;
            model.TotalImpuestosRetenidos = totalRet;
            model.TrasladosResumen = trasResumen;
            model.RetencionesResumen = retResumen;

            // Addenda
            var addendaNode = comprobante.Element(Cfdi + "Addenda");
            if (addendaNode != null)
                model.Addenda = MapAddenda(addendaNode);

            // Complemento Carta Porte
            var cartaPorteNode = comprobante.Descendants(Cp + "CartaPorte").FirstOrDefault();
            if (cartaPorteNode != null)
                MapCartaPorteComplemento(cartaPorteNode, model);
        }

        #region Carta Porte

        private static void MapCartaPorteComplemento(XElement cpNode, CfdiCartaPorteViewModel model)
        {
            var mercanciasNode = cpNode.Element(Cp + "Mercancias");

            model.CartaPorte = new CartaPorteViewModel
            {
                Version = cpNode.Attribute("Version")?.Value,
                TransporteInternacional = cpNode.Attribute("TranspInternac")?.Value,
                EntradaSalidaMercancia = cpNode.Attribute("EntradaSalidaMerc")?.Value,
                ViaEntradaSalida = cpNode.Attribute("ViaEntradaSalida")?.Value,
                PaisOrigenDestino = cpNode.Attribute("PaisOrigenDestino")?.Value,
                DistanciaRecorrida = decimal.Parse(cpNode.Attribute("TotalDistRec")?.Value ?? "0", CultureInfo.InvariantCulture),
                IdCCP = cpNode.Attribute("IdCCP")?.Value,
                PesoBrutoTotal = decimal.Parse(mercanciasNode?.Attribute("PesoBrutoTotal")?.Value ?? "0", CultureInfo.InvariantCulture),
                UnidadPeso = mercanciasNode?.Attribute("UnidadPeso")?.Value,
                NumeroTotalMercancias = int.Parse(mercanciasNode?.Attribute("NumTotalMercancias")?.Value ?? "0", CultureInfo.InvariantCulture)
            };

            model.CartaPorte.Ubicaciones = cpNode
                .Element(Cp + "Ubicaciones")
                ?.Elements(Cp + "Ubicacion")
                .Select(u => MapUbicacion(u))
                .ToList() ?? new List<UbicacionViewModel>();

            model.CartaPorte.MercanciasDetalle = cpNode
                .Element(Cp + "Mercancias")
                ?.Elements(Cp + "Mercancia")
                .Select(m => new MercanciaViewModel
                {
                    Descripcion = m.Attribute("Descripcion")?.Value,
                    Cantidad = decimal.Parse(m.Attribute("Cantidad")?.Value ?? "0", CultureInfo.InvariantCulture),
                    ClaveUnidad = m.Attribute("ClaveUnidad")?.Value,
                    PesoEnKg = decimal.Parse(m.Attribute("PesoEnKg")?.Value ?? "0", CultureInfo.InvariantCulture),
                    ValorMercancia = decimal.Parse(m.Attribute("ValorMercancia")?.Value ?? "0", CultureInfo.InvariantCulture),
                }).ToList() ?? new();

            model.CartaPorte.Autotransporte = MapAutotransporte(cpNode.Element(Cp + "Mercancias")?.Element(Cp + "Autotransporte"));
            model.CartaPorte.Seguro = MapSeguro(cpNode.Element(Cp + "Mercancias")?.Element(Cp + "Autotransporte")?.Element(Cp + "Seguros"));
            model.CartaPorte.Remolque = MapRemolque(cpNode.Element(Cp + "Mercancias")?.Element(Cp + "Autotransporte")?.Element(Cp + "Remolques")?.Element(Cp + "Remolque"));
            model.CartaPorte.FigurasTransporte = cpNode.Element(Cp + "FiguraTransporte")?.Elements(Cp + "TiposFigura").Select(tf => new FiguraTransporteViewModel
            {
                TipoFigura = tf.Attribute("TipoFigura")?.Value,
                RFCFigura = tf.Attribute("RFCFigura")?.Value,
                NombreFigura = tf.Attribute("NombreFigura")?.Value,
                NumeroLicencia = tf.Attribute("NumLicencia")?.Value
            }).ToList() ?? new();
        }

        #endregion

        #region Addenda

        private AddendaViewModel MapAddenda(XElement addendaNode)
        {
            var result = new AddendaViewModel();

            var informacionAdicional = addendaNode.Descendants()
                .FirstOrDefault(x => x.Name.LocalName == "InformacionAdicional");

            if (informacionAdicional != null && informacionAdicional.ToString().Contains("buzone.com.mx"))
            {
                result.IsParserGenerico = true;

                var cdata = informacionAdicional.Nodes().OfType<XCData>().FirstOrDefault();
                string xmlContent = cdata != null ? cdata.Value : informacionAdicional.ToString();

                try
                {
                    var innerDoc = XDocument.Parse(xmlContent);

                    foreach (var seccion in innerDoc.Root.Elements())
                    {
                        var nuevaSeccion = new AddendaSeccionViewModel
                        {
                            NombreSeccion = seccion.Name.LocalName
                        };

                        var atributos = seccion.Attributes().ToList();
                        var etiquetas = atributos.Where(a => a.Name.LocalName.StartsWith("etiqueta")).ToList();
                        var valores = atributos.Where(a => a.Name.LocalName.StartsWith("valor")).ToList();

                        foreach (var etiqueta in etiquetas)
                        {
                            var numero = new string(etiqueta.Name.LocalName.Where(char.IsDigit).ToArray());
                            var valorCorrespondiente = valores.FirstOrDefault(v => v.Name.LocalName.Contains(numero));

                            if (!string.IsNullOrWhiteSpace(etiqueta.Value) && valorCorrespondiente != null && !string.IsNullOrWhiteSpace(valorCorrespondiente.Value))
                            {
                                nuevaSeccion.Campos.Add(new KeyValuePair<string, string>(etiqueta.Value, valorCorrespondiente.Value));
                            }
                        }

                        foreach (var atributo in atributos.Where(a => !a.Name.LocalName.StartsWith("etiqueta") && !a.Name.LocalName.StartsWith("valor")))
                        {
                            if (!string.IsNullOrWhiteSpace(atributo.Value))
                            {
                                nuevaSeccion.Campos.Add(new KeyValuePair<string, string>(atributo.Name.LocalName, atributo.Value));
                            }
                        }

                        if (nuevaSeccion.Campos.Any())
                        {
                            result.Secciones.Add(nuevaSeccion);
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.XmlRaw = xmlContent;
                    Logger.LogWarning(ex, "No se pudo parsear el contenido de la addenda; se usará XmlRaw como fallback.");
                }
            }
            else
            {
                result.XmlRaw = addendaNode.ToString();
            }

            return result;
        }

        #endregion

        #region Helpers Carta Porte

        private static UbicacionViewModel MapUbicacion(XElement ubicacion)
        {
            if (ubicacion == null) return null;

            var domicilio = ubicacion.Element(Cp + "Domicilio");

            return new UbicacionViewModel
            {
                TipoUbicacion = ubicacion.Attribute("TipoUbicacion")?.Value,
                IDUbicacion = ubicacion.Attribute("IDUbicacion")?.Value,
                RFCRemitenteDestinatario = ubicacion.Attribute("RFCRemitenteDestinatario")?.Value,
                NombreRemitenteDestinatario = ubicacion.Attribute("NombreRemitenteDestinatario")?.Value,
                FechaHoraSalidaLlegada = ubicacion.Attribute("FechaHoraSalidaLlegada") != null
                    ? DateTime.Parse(ubicacion.Attribute("FechaHoraSalidaLlegada").Value, CultureInfo.InvariantCulture)
                    : (DateTime?)null,
                CodigoPostal = domicilio?.Attribute("CodigoPostal")?.Value,
                Municipio = domicilio?.Attribute("Municipio")?.Value,
                Localidad = domicilio?.Attribute("Localidad")?.Value,
                Estado = domicilio?.Attribute("Estado")?.Value,
                Pais = domicilio?.Attribute("Pais")?.Value,
                NumRegIdTrib = ubicacion.Attribute("NumRegIdTrib")?.Value,
                ResidenciaFiscal = ubicacion.Attribute("ResidenciaFiscal")?.Value
            };
        }

        private static AutotransporteViewModel MapAutotransporte(XElement autotransporte)
        {
            if (autotransporte == null) return null;

            var idVehicular = autotransporte.Element(Cp + "IdentificacionVehicular");

            return new AutotransporteViewModel
            {
                PermisoSCT = autotransporte.Attribute("PermSCT")?.Value,
                NumeroPermisoSCT = autotransporte.Attribute("NumPermisoSCT")?.Value,
                ConfigVehicular = idVehicular?.Attribute("ConfigVehicular")?.Value,
                PesoBrutoVehicular = decimal.Parse(idVehicular?.Attribute("PesoBrutoVehicular")?.Value ?? "0", CultureInfo.InvariantCulture),
                PlacaVM = idVehicular?.Attribute("PlacaVM")?.Value,
                AnioModeloVM = int.Parse(idVehicular?.Attribute("AnioModeloVM")?.Value ?? "0", CultureInfo.InvariantCulture)
            };
        }

        private static SeguroViewModel MapSeguro(XElement seguros)
        {
            if (seguros == null) return null;

            return new SeguroViewModel
            {
                AseguradoraResponsabilidadCivil = seguros.Attribute("AseguraRespCivil")?.Value,
                PolizaResponsabilidadCivil = seguros.Attribute("PolizaRespCivil")?.Value,
                AseguradoraCarga = seguros.Attribute("AseguraCarga")?.Value,
                PolizaCarga = seguros.Attribute("PolizaCarga")?.Value,
                AseguradoraMedAmbiente = seguros.Attribute("AseguraMedAmbiente")?.Value,
                PolizaMedAmbiente = seguros.Attribute("PolizaMedAmbiente")?.Value
            };
        }

        private static RemolqueViewModel MapRemolque(XElement remolque)
        {
            if (remolque == null) return null;

            return new RemolqueViewModel
            {
                SubTipoRemolque = remolque.Attribute("SubTipoRem")?.Value,
                Placa = remolque.Attribute("Placa")?.Value
            };
        }

        #endregion
    }
}
