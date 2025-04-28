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


            var model = new CfdiCartaPorteViewModel
            {
                // CFDI
                Version = comprobante?.Attribute("Version")?.Value,
                Serie = comprobante?.Attribute("Serie")?.Value,
                Folio = comprobante?.Attribute("Folio")?.Value,
                LugarExpedicion = comprobante?.Attribute("LugarExpedicion")?.Value,
                FechaEmision = DateTime.Parse(comprobante?.Attribute("Fecha")?.Value),
                FechaCertificacion = tfdNode != null ? DateTime.Parse(tfdNode.Attribute("FechaTimbrado").Value) : DateTime.MinValue,
                TipoCambio = comprobante?.Attribute("TipoCambio")?.Value,
                Moneda = comprobante?.Attribute("Moneda")?.Value,
                FormaPago = comprobante?.Attribute("FormaPago")?.Value,
                MetodoPago = comprobante?.Attribute("MetodoPago")?.Value,
                TipoComprobante = comprobante?.Attribute("TipoDeComprobante")?.Value,
                Exportacion = comprobante?.Attribute("Exportacion")?.Value,
                CondicionesPago = comprobante?.Attribute("CondicionesDePago")?.Value,

                // Emisor
                EmisorNombre = comprobante.Element(cfdi + "Emisor")?.Attribute("Nombre")?.Value,
                EmisorRFC = comprobante.Element(cfdi + "Emisor")?.Attribute("Rfc")?.Value,
                EmisorRegimenFiscal = comprobante.Element(cfdi + "Emisor")?.Attribute("RegimenFiscal")?.Value,

                // Receptor
                ReceptorNombre = comprobante.Element(cfdi + "Receptor")?.Attribute("Nombre")?.Value,
                ReceptorRFC = comprobante.Element(cfdi + "Receptor")?.Attribute("Rfc")?.Value,
                ReceptorDomicilioFiscal = comprobante.Element(cfdi + "Receptor")?.Attribute("DomicilioFiscalReceptor")?.Value,
                ReceptorRegimenFiscal = comprobante.Element(cfdi + "Receptor")?.Attribute("RegimenFiscalReceptor")?.Value,
                UsoCFDI = comprobante.Element(cfdi + "Receptor")?.Attribute("UsoCFDI")?.Value,

                // Conceptos
                Conceptos = comprobante
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
                        ObjetoImpuesto = c.Attribute("ObjetoImp")?.Value
                    }).ToList() ?? new(),

                // Totales
                SubTotal = decimal.Parse(comprobante?.Attribute("SubTotal")?.Value ?? "0"),
                Total = decimal.Parse(comprobante?.Attribute("Total")?.Value ?? "0"),

                CantidadConLetra = NumberToWordsConverter.Convertir(decimal.Parse(comprobante?.Attribute("Total")?.Value ?? "0"), comprobante?.Attribute("Moneda")?.Value),

                TotalImpuestosTrasladados = comprobante.Element(cfdi + "Impuestos")?.Attribute("TotalImpuestosTrasladados") != null
                    ? decimal.Parse(comprobante.Element(cfdi + "Impuestos")?.Attribute("TotalImpuestosTrasladados")?.Value ?? "0")
                    : 0,

                // Sellos y Cadenas
                SelloEmisor = comprobante?.Attribute("Sello")?.Value,
                NoCertificadoEmisor = comprobante?.Attribute("NoCertificado")?.Value,
                NoCertificadoSAT = tfdNode?.Attribute("NoCertificadoSAT")?.Value,
                SelloSAT = tfdNode?.Attribute("SelloSAT")?.Value,
                CadenaOriginalSAT = cadenaOriginal,
                UUID = tfdNode?.Attribute("UUID")?.Value,

                // QR Code Base64 (queda pendiente que tú lo generes)
                QRCodeBase64 = ""
            };

            var mercanciasNode = cartaPorte.Element(cartaporte + "Mercancias");

            // Carta Porte
            if (cartaPorte != null)
            {
                model.CartaPorte = new CartaPorteViewModel
                {
                    Version = cartaPorte?.Attribute("Version")?.Value,
                    TransporteInternacional = cartaPorte?.Attribute("TranspInternac")?.Value,
                    EntradaSalidaMercancia = cartaPorte?.Attribute("EntradaSalidaMerc")?.Value,
                    ViaEntradaSalida = cartaPorte?.Attribute("ViaEntradaSalida")?.Value,
                    PaisOrigenDestino = cartaPorte?.Attribute("PaisOrigenDestino")?.Value,
                    DistanciaRecorrida = decimal.Parse(cartaPorte?.Attribute("TotalDistRec")?.Value ?? "0"),

                    IdCCP = cartaPorte?.Attribute("IdCCP")?.Value,

                    // Ahora sí obtenemos PesoBrutoTotal, UnidadPeso, NumTotalMercancias
                    PesoBrutoTotal = decimal.Parse(mercanciasNode?.Attribute("PesoBrutoTotal")?.Value ?? "0"),
                    UnidadPeso = mercanciasNode?.Attribute("UnidadPeso")?.Value,
                    NumeroTotalMercancias = int.Parse(mercanciasNode?.Attribute("NumTotalMercancias")?.Value ?? "0"),


                    Ubicaciones = cartaPorte
                        .Element(cartaporte + "Ubicaciones")
                        ?.Elements(cartaporte + "Ubicacion")
                        .Select(u => MapUbicacion(u))
                        .ToList() ?? new List<UbicacionViewModel>(),


                    MercanciasDetalle = cartaPorte
                        .Element(cartaporte + "Mercancias")
                        ?.Elements(cartaporte + "Mercancia")
                        .Select(m => new MercanciaViewModel
                        {
                            Descripcion = m.Attribute("Descripcion")?.Value,
                            Cantidad = decimal.Parse(m.Attribute("Cantidad")?.Value ?? "0"),
                            ClaveUnidad = m.Attribute("ClaveUnidad")?.Value,
                            PesoEnKg = decimal.Parse(m.Attribute("PesoEnKg")?.Value ?? "0"),
                            ValorMercancia = decimal.Parse(m.Attribute("ValorMercancia")?.Value ?? "0"),
                        }).ToList() ?? new(),

                    Autotransporte = MapAutotransporte(cartaPorte.Element(cartaporte + "Mercancias")?.Element(cartaporte + "Autotransporte")),
                    Seguro = MapSeguro(cartaPorte.Element(cartaporte + "Mercancias")?.Element(cartaporte + "Autotransporte")?.Element(cartaporte + "Seguros")),
                    Remolque = MapRemolque(cartaPorte.Element(cartaporte + "Mercancias")?.Element(cartaporte + "Autotransporte")?.Element(cartaporte + "Remolques")?.Element(cartaporte + "Remolque")),
                    FigurasTransporte = cartaPorte.Element(cartaporte + "FiguraTransporte")?.Elements(cartaporte + "TiposFigura").Select(tf => new FiguraTransporteViewModel
                        {
                            TipoFigura = tf.Attribute("TipoFigura")?.Value,
                            RFCFigura = tf.Attribute("RFCFigura")?.Value,
                            NombreFigura = tf.Attribute("NombreFigura")?.Value,
                            NumeroLicencia = tf.Attribute("NumLicencia")?.Value
                        }).ToList() ?? new()


                };
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
        private static UbicacionViewModel MapUbicacion(XElement ubicacion)
        {
            if (ubicacion == null) return null;

            var domicilio = ubicacion.Element(cartaporte + "Domicilio");

            return new UbicacionViewModel
            {
                TipoUbicacion = ubicacion.Attribute("TipoUbicacion")?.Value,
                IDUbicacion = ubicacion.Attribute("IDUbicacion")?.Value,
                RFCRemitenteDestinatario = ubicacion.Attribute("RFCRemitenteDestinatario")?.Value,
                NombreRemitenteDestinatario = ubicacion.Attribute("NombreRemitenteDestinatario")?.Value,
                FechaHoraSalidaLlegada = ubicacion.Attribute("FechaHoraSalidaLlegada") != null
                    ? DateTime.Parse(ubicacion.Attribute("FechaHoraSalidaLlegada").Value)
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

            var identificacionVehicular = autotransporte.Element(cartaporte + "IdentificacionVehicular");

            return new AutotransporteViewModel
            {
                PermisoSCT = autotransporte.Attribute("PermSCT")?.Value,
                NumeroPermisoSCT = autotransporte.Attribute("NumPermisoSCT")?.Value,
                ConfigVehicular = identificacionVehicular?.Attribute("ConfigVehicular")?.Value,
                PesoBrutoVehicular = decimal.Parse(identificacionVehicular?.Attribute("PesoBrutoVehicular")?.Value ?? "0"),
                PlacaVM = identificacionVehicular?.Attribute("PlacaVM")?.Value,
                AnioModeloVM = int.Parse(identificacionVehicular?.Attribute("AnioModeloVM")?.Value ?? "0")
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
