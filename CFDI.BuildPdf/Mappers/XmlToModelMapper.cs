using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using CFDI.BuildPdf.Helpers;
using CFDI.BuildPdf.Models;

namespace CFDI.BuildPdf.Mappers
{
    public static class XmlToModelMapper
    {
        private static XNamespace cfdi = "http://www.sat.gob.mx/cfd/4";
        private static XNamespace cartaporte = "http://www.sat.gob.mx/CartaPorte31";
        private static XNamespace tfd = "http://www.sat.gob.mx/TimbreFiscalDigital";

        public static CfdiCartaPorteViewModel Map(XDocument xdoc)
        {
            var comprobante = xdoc.Root;
            var cartaPorte = comprobante.Descendants(cartaporte + "CartaPorte").FirstOrDefault();
            var tfdNode = comprobante.Descendants(tfd + "TimbreFiscalDigital").FirstOrDefault();

            var cadenaOriginal = tfdNode != null
                ? $"||{tfdNode.Attribute("Version")?.Value}|{tfdNode.Attribute("UUID")?.Value}|{tfdNode.Attribute("FechaTimbrado")?.Value}|{tfdNode.Attribute("RfcProvCertif")?.Value}|{tfdNode.Attribute("SelloCFD")?.Value}|{tfdNode.Attribute("NoCertificadoSAT")?.Value}||"
                : string.Empty;


            var model = new CfdiCartaPorteViewModel();

            // CFDI
            model.Version = comprobante?.Attribute("Version")?.Value;
            model.Serie = comprobante?.Attribute("Serie")?.Value;
            model.Folio = comprobante?.Attribute("Folio")?.Value;
            model.LugarExpedicion = comprobante?.Attribute("LugarExpedicion")?.Value;
            model.FechaEmision = DateTime.Parse(comprobante?.Attribute("Fecha")?.Value);
            model.FechaCertificacion = tfdNode != null ? DateTime.Parse(tfdNode.Attribute("FechaTimbrado").Value) : DateTime.MinValue;
            model.TipoCambio = comprobante?.Attribute("TipoCambio")?.Value;
            model.Moneda = comprobante?.Attribute("Moneda")?.Value;
            model.FormaPago = comprobante?.Attribute("FormaPago")?.Value;
            model.MetodoPago = comprobante?.Attribute("MetodoPago")?.Value;
            model.TipoComprobante = comprobante?.Attribute("TipoDeComprobante")?.Value;
            model.Exportacion = comprobante?.Attribute("Exportacion")?.Value;
            model.CondicionesPago = comprobante?.Attribute("CondicionesDePago")?.Value;

            // Emisor
            model.EmisorNombre = comprobante.Element(cfdi + "Emisor")?.Attribute("Nombre")?.Value;
            model.EmisorRFC = comprobante.Element(cfdi + "Emisor")?.Attribute("Rfc")?.Value;
            model.EmisorRegimenFiscal = comprobante.Element(cfdi + "Emisor")?.Attribute("RegimenFiscal")?.Value;

            // Receptor
            model.ReceptorNombre = comprobante.Element(cfdi + "Receptor")?.Attribute("Nombre")?.Value;
            model.ReceptorRFC = comprobante.Element(cfdi + "Receptor")?.Attribute("Rfc")?.Value;
            model.ReceptorDomicilioFiscal = comprobante.Element(cfdi + "Receptor")?.Attribute("DomicilioFiscalReceptor")?.Value;
            model.ReceptorRegimenFiscal = comprobante.Element(cfdi + "Receptor")?.Attribute("RegimenFiscalReceptor")?.Value;
            model.UsoCFDI = comprobante.Element(cfdi + "Receptor")?.Attribute("UsoCFDI")?.Value;


            model.Conceptos = comprobante
                    .Element(cfdi + "Conceptos")
                    ?.Elements(cfdi + "Concepto")
                    .Select(c => new ConceptoViewModel
                    {
                        ClaveProductoServicio = c.Attribute("ClaveProdServ")?.Value,
                        NumeroIdentificacion = c.Attribute("NoIdentificacion")?.Value,
                        Descripcion = c.Attribute("Descripcion")?.Value,
                        Cantidad = decimal.Parse(c.Attribute("Cantidad")?.Value ?? "0"),
                        ClaveUnidad = c.Attribute("ClaveUnidad")?.Value,
                        Unidad = c.Attribute("Unidad")?.Value,
                        ValorUnitario = decimal.Parse(c.Attribute("ValorUnitario")?.Value ?? "0"),
                        Importe = decimal.Parse(c.Attribute("Importe")?.Value ?? "0"),
                        Descuento = decimal.Parse(c.Attribute("Descuento")?.Value ?? "0"),
                        ObjetoImpuesto = c.Attribute("ObjetoImp")?.Value,
                        Traslados = c.Element(cfdi + "Impuestos")
                             ?.Element(cfdi + "Traslados")
                             ?.Elements(cfdi + "Traslado")
                             .Select(t => new TrasladoImpuestoViewModel
                             {
                                 Impuesto = t.Attribute("Impuesto")?.Value,
                                 TipoFactor = t.Attribute("TipoFactor")?.Value,
                                 TasaOCuota = decimal.Parse(t.Attribute("TasaOCuota")?.Value ?? "0"),
                                 Base = decimal.Parse(t.Attribute("Base")?.Value ?? "0"),
                                 Importe = decimal.Parse(t.Attribute("Importe")?.Value ?? "0")
                             }).ToList() ?? new List<TrasladoImpuestoViewModel>()

                    }).ToList() ?? new();


            // Totales
            model.SubTotal = decimal.Parse(comprobante?.Attribute("SubTotal")?.Value ?? "0");
            model.Total = decimal.Parse(comprobante?.Attribute("Total")?.Value ?? "0");
            model.CantidadConLetra = NumberToWordsConverter.Convertir(decimal.Parse(comprobante?.Attribute("Total")?.Value ?? "0"), comprobante?.Attribute("Moneda")?.Value);
            model.TotalImpuestosTrasladados = comprobante.Element(cfdi + "Impuestos")?.Attribute("TotalImpuestosTrasladados") != null
                    ? decimal.Parse(comprobante.Element(cfdi + "Impuestos")?.Attribute("TotalImpuestosTrasladados")?.Value ?? "0")
                    : 0;

            // Sellos y Cadenas
            model.SelloEmisor = comprobante?.Attribute("Sello")?.Value;
            model.NoCertificadoEmisor = comprobante?.Attribute("NoCertificado")?.Value;
            model.NoCertificadoSAT = tfdNode?.Attribute("NoCertificadoSAT")?.Value;
            model.SelloSAT = tfdNode?.Attribute("SelloSAT")?.Value;
            model.CadenaOriginalSAT = cadenaOriginal;
            model.UUID = tfdNode?.Attribute("UUID")?.Value;

            // QR Code Base64 (queda pendiente que tú lo generes)
            model.QRCodeBase64 = "";


            var addendaNode = comprobante.Element(cfdi + "Addenda");

            if (addendaNode != null)
            {
                model.Addenda = MapAddenda(addendaNode);
            }


            var mercanciasNode = cartaPorte.Element(cartaporte + "Mercancias");

            // Carta Porte
            if (cartaPorte != null)
            {
                model.CartaPorte = new CartaPorteViewModel();

                model.CartaPorte.Version = cartaPorte?.Attribute("Version")?.Value;
                model.CartaPorte.TransporteInternacional = cartaPorte?.Attribute("TranspInternac")?.Value;
                model.CartaPorte.EntradaSalidaMercancia = cartaPorte?.Attribute("EntradaSalidaMerc")?.Value;
                model.CartaPorte.ViaEntradaSalida = cartaPorte?.Attribute("ViaEntradaSalida")?.Value;
                model.CartaPorte.PaisOrigenDestino = cartaPorte?.Attribute("PaisOrigenDestino")?.Value;
                model.CartaPorte.DistanciaRecorrida = decimal.Parse(cartaPorte?.Attribute("TotalDistRec")?.Value ?? "0");

                model.CartaPorte.IdCCP = cartaPorte?.Attribute("IdCCP")?.Value;

                // Ahora sí obtenemos PesoBrutoTotal, UnidadPeso, NumTotalMercancias
                model.CartaPorte.PesoBrutoTotal = decimal.Parse(mercanciasNode?.Attribute("PesoBrutoTotal")?.Value ?? "0");
                model.CartaPorte.UnidadPeso = mercanciasNode?.Attribute("UnidadPeso")?.Value;
                model.CartaPorte.NumeroTotalMercancias = int.Parse(mercanciasNode?.Attribute("NumTotalMercancias")?.Value ?? "0");


                model.CartaPorte.Ubicaciones = cartaPorte
                        .Element(cartaporte + "Ubicaciones")
                        ?.Elements(cartaporte + "Ubicacion")
                        .Select(u => MapUbicacion(u))
                        .ToList() ?? new List<UbicacionViewModel>();


                model.CartaPorte.MercanciasDetalle = cartaPorte
                        .Element(cartaporte + "Mercancias")
                        ?.Elements(cartaporte + "Mercancia")
                        .Select(m => new MercanciaViewModel
                        {
                            Descripcion = m.Attribute("Descripcion")?.Value,
                            Cantidad = decimal.Parse(m.Attribute("Cantidad")?.Value ?? "0"),
                            ClaveUnidad = m.Attribute("ClaveUnidad")?.Value,
                            PesoEnKg = decimal.Parse(m.Attribute("PesoEnKg")?.Value ?? "0"),
                            ValorMercancia = decimal.Parse(m.Attribute("ValorMercancia")?.Value ?? "0"),
                        }).ToList() ?? new();

                model.CartaPorte.Autotransporte = MapAutotransporte(cartaPorte.Element(cartaporte + "Mercancias")?.Element(cartaporte + "Autotransporte"));
                model.CartaPorte.Seguro = MapSeguro(cartaPorte.Element(cartaporte + "Mercancias")?.Element(cartaporte + "Autotransporte")?.Element(cartaporte + "Seguros"));
                model.CartaPorte.Remolque = MapRemolque(cartaPorte.Element(cartaporte + "Mercancias")?.Element(cartaporte + "Autotransporte")?.Element(cartaporte + "Remolques")?.Element(cartaporte + "Remolque"));
                model.CartaPorte.FigurasTransporte = cartaPorte.Element(cartaporte + "FiguraTransporte")?.Elements(cartaporte + "TiposFigura").Select(tf => new FiguraTransporteViewModel
                {
                    TipoFigura = tf.Attribute("TipoFigura")?.Value,
                    RFCFigura = tf.Attribute("RFCFigura")?.Value,
                    NombreFigura = tf.Attribute("NombreFigura")?.Value,
                    NumeroLicencia = tf.Attribute("NumLicencia")?.Value
                }).ToList() ?? new();



            }

            // ⚡ Generar URL del QR
            model.UrlQr = ConstruirUrlQr(
                model.UUID,
                model.EmisorRFC,
                model.ReceptorRFC,
                model.Total,
                model.SelloEmisor
            );

            // ⚡ Generar QR Base64 a partir de URL
            model.QRCodeBase64 = QrGeneratorService.GenerateQr(model.UrlQr);

            return model;
        }

        private static AddendaViewModel MapAddenda(XElement addendaNode)
        {
            var result = new AddendaViewModel();

            var informacionAdicional = addendaNode.Descendants()
                .FirstOrDefault(x => x.Name.LocalName == "InformacionAdicional");

            if (informacionAdicional != null && informacionAdicional.ToString().Contains("buzone.com.mx"))
            {
                result.IsParserGenerico = true;

                var cdata = informacionAdicional.Nodes().OfType<XCData>().FirstOrDefault();
                string xmlContent;

                if (cdata != null)
                {
                    xmlContent = cdata.Value;
                }
                else
                {
                    xmlContent = informacionAdicional.ToString();
                }

                try
                {
                    var innerDoc = XDocument.Parse(xmlContent);

                    foreach (var seccion in innerDoc.Root.Elements())
                    {
                        var nuevaSeccion = new AddendaSeccionViewModel
                        {
                            NombreSeccion = seccion.Name.LocalName
                        };

                        // Mapeo especial: detectar etiquetaX + valorX
                        var atributos = seccion.Attributes().ToList();
                        var etiquetas = atributos.Where(a => a.Name.LocalName.StartsWith("etiqueta")).ToList();
                        var valores = atributos.Where(a => a.Name.LocalName.StartsWith("valor")).ToList();

                        foreach (var etiqueta in etiquetas)
                        {
                            // Sacar número de la etiqueta (por ejemplo: etiqueta8T --> 8)
                            var numero = new string(etiqueta.Name.LocalName.Where(char.IsDigit).ToArray());

                            var valorCorrespondiente = valores
                                .FirstOrDefault(v => v.Name.LocalName.Contains(numero));

                            if (!string.IsNullOrWhiteSpace(etiqueta.Value) && valorCorrespondiente != null && !string.IsNullOrWhiteSpace(valorCorrespondiente.Value))
                            {
                                nuevaSeccion.Campos.Add(new KeyValuePair<string, string>(
                                    etiqueta.Value, // El texto amigable de la etiqueta
                                    valorCorrespondiente.Value // El dato real
                                ));
                            }
                        }

                        // También capturamos cualquier otro atributo (que no sea etiqueta/valor)
                        foreach (var atributo in atributos
                            .Where(a => !a.Name.LocalName.StartsWith("etiqueta") && !a.Name.LocalName.StartsWith("valor")))
                        {
                            if (!string.IsNullOrWhiteSpace(atributo.Value))
                            {
                                nuevaSeccion.Campos.Add(new KeyValuePair<string, string>(
                                    atributo.Name.LocalName,
                                    atributo.Value
                                ));
                            }
                        }

                        if (nuevaSeccion.Campos.Any())
                        {
                            result.Secciones.Add(nuevaSeccion);
                        }
                    }
                }
                catch
                {
                    result.XmlRaw = xmlContent;
                }
            }
            else
            {
                result.XmlRaw = addendaNode.ToString();
            }

            return result;
        }


        private static UbicacionViewModel MapUbicacion(XElement ubicacion)
        {
            if (ubicacion == null) return null;

            var domicilio = ubicacion.Element(cartaporte + "Domicilio");

            var ubicaciones = new UbicacionViewModel();

            ubicaciones.TipoUbicacion = ubicacion.Attribute("TipoUbicacion")?.Value;
            ubicaciones.IDUbicacion = ubicacion.Attribute("IDUbicacion")?.Value;
            ubicaciones.RFCRemitenteDestinatario = ubicacion.Attribute("RFCRemitenteDestinatario")?.Value;
            ubicaciones.NombreRemitenteDestinatario = ubicacion.Attribute("NombreRemitenteDestinatario")?.Value;
            ubicaciones.FechaHoraSalidaLlegada = ubicacion.Attribute("FechaHoraSalidaLlegada") != null
                    ? DateTime.Parse(ubicacion.Attribute("FechaHoraSalidaLlegada").Value)
                    : (DateTime?)null;
            ubicaciones.CodigoPostal = domicilio?.Attribute("CodigoPostal")?.Value;
            ubicaciones.Municipio = domicilio?.Attribute("Municipio")?.Value;
            ubicaciones.Localidad = domicilio?.Attribute("Localidad")?.Value;
            ubicaciones.Estado = domicilio?.Attribute("Estado")?.Value;
            ubicaciones.Pais = domicilio?.Attribute("Pais")?.Value;
            ubicaciones.NumRegIdTrib = ubicacion.Attribute("NumRegIdTrib")?.Value;
            ubicaciones.ResidenciaFiscal = ubicacion.Attribute("ResidenciaFiscal")?.Value;

            return ubicaciones;
        }


        private static AutotransporteViewModel MapAutotransporte(XElement autotransporte)
        {
            if (autotransporte == null) return null;

            var identificacionVehicular = autotransporte.Element(cartaporte + "IdentificacionVehicular");

            var autoTrans = new AutotransporteViewModel();


            autoTrans.PermisoSCT = autotransporte.Attribute("PermSCT")?.Value;
            autoTrans.NumeroPermisoSCT = autotransporte.Attribute("NumPermisoSCT")?.Value;
            autoTrans.ConfigVehicular = identificacionVehicular?.Attribute("ConfigVehicular")?.Value;
            autoTrans.PesoBrutoVehicular = decimal.Parse(identificacionVehicular?.Attribute("PesoBrutoVehicular")?.Value ?? "0");
            autoTrans.PlacaVM = identificacionVehicular?.Attribute("PlacaVM")?.Value;
            autoTrans.AnioModeloVM = int.Parse(identificacionVehicular?.Attribute("AnioModeloVM")?.Value ?? "0");

            return autoTrans;
        }


        private static SeguroViewModel MapSeguro(XElement seguros)
        {
            if (seguros == null) return null;

            var segurosVm = new SeguroViewModel();

            segurosVm.AseguradoraResponsabilidadCivil = seguros.Attribute("AseguraRespCivil")?.Value;
            segurosVm.PolizaResponsabilidadCivil = seguros.Attribute("PolizaRespCivil")?.Value;
            segurosVm.AseguradoraCarga = seguros.Attribute("AseguraCarga")?.Value;
            segurosVm.PolizaCarga = seguros.Attribute("PolizaCarga")?.Value;
            segurosVm.AseguradoraMedAmbiente = seguros.Attribute("AseguraMedAmbiente")?.Value;
            segurosVm.PolizaMedAmbiente = seguros.Attribute("PolizaMedAmbiente")?.Value;

            return segurosVm;
        }



        private static RemolqueViewModel MapRemolque(XElement remolque)
        {
            if (remolque == null) return null;

            var remolqueVm = new RemolqueViewModel();

            remolqueVm.SubTipoRemolque = remolque.Attribute("SubTipoRem")?.Value;
            remolqueVm.Placa = remolque.Attribute("Placa")?.Value;

            return remolqueVm;


        }

        private static string ConstruirUrlQr(string uuid, string rfcEmisor, string rfcReceptor, decimal total, string selloEmisor)
        {
            // Total formateado a 18 posiciones (ceros a la izquierda y 6 decimales)
            string totalFormateado = total.ToString("000000000000000000.000000").Replace(',', '.').Replace(".", string.Empty);
            totalFormateado = totalFormateado.Insert(totalFormateado.Length - 6, ".");

            // Tomar los últimos 8 caracteres del sello
            string fe = selloEmisor.Length >= 8 ? selloEmisor.Substring(selloEmisor.Length - 8) : selloEmisor;

            // Armar URL
            return $"https://verificacfdi.facturaelectronica.sat.gob.mx/default.aspx?id={uuid}&re={rfcEmisor}&rr={rfcReceptor}&tt={totalFormateado}&fe={fe}";
        }

    }
}
