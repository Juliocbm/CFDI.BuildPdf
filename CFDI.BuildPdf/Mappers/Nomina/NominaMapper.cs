using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Mappers.Common;
using CFDI.BuildPdf.Models;
using Microsoft.Extensions.Logging;

namespace CFDI.BuildPdf.Mappers.Nomina
{
    /// <summary>
    /// Mapper de CFDI 4.0 con complemento Nómina 1.2.
    /// Hereda lógica común de <see cref="BaseCfdiMapper{TModel}"/> (Template Method).
    /// </summary>
    internal class NominaMapper : BaseCfdiMapper<CfdiNominaViewModel>
    {
        private static readonly XNamespace Nom = "http://www.sat.gob.mx/nomina12";

        public NominaMapper(IQrGenerator qrGenerator, ILogger<NominaMapper>? logger = null)
            : base(qrGenerator, logger) { }

        /// <inheritdoc />
        public override bool CanMap(XDocument xdoc)
        {
            return xdoc.Root?.Descendants(Nom + "Nomina").Any() == true;
        }

        /// <inheritdoc />
        protected override CfdiNominaViewModel CreateModel() => new();

        /// <inheritdoc />
        protected override void MapComprobanteBase(XDocument xdoc, CfdiNominaViewModel model)
        {
            base.MapComprobanteBase(xdoc, model);

            // Override: TipoCambio default "1" para Nómina
            model.TipoCambio ??= "1";
        }

        /// <inheritdoc />
        protected override void MapComplemento(XDocument xdoc, CfdiNominaViewModel model)
        {
            var comprobante = xdoc.Root;

            // Conceptos Nómina
            model.Conceptos = comprobante
                .Element(Cfdi + "Conceptos")
                ?.Elements(Cfdi + "Concepto")
                .Select(c => new ConceptoNominaViewModel
                {
                    ClaveProductoServicio = c.Attribute("ClaveProdServ")?.Value,
                    NumeroIdentificacion = c.Attribute("NoIdentificacion")?.Value,
                    Cantidad = decimal.Parse(c.Attribute("Cantidad")?.Value ?? "0", CultureInfo.InvariantCulture),
                    ClaveUnidad = c.Attribute("ClaveUnidad")?.Value,
                    Unidad = c.Attribute("Unidad")?.Value,
                    Descripcion = c.Attribute("Descripcion")?.Value,
                    ValorUnitario = decimal.Parse(c.Attribute("ValorUnitario")?.Value ?? "0", CultureInfo.InvariantCulture),
                    Importe = decimal.Parse(c.Attribute("Importe")?.Value ?? "0", CultureInfo.InvariantCulture),
                    Descuento = decimal.Parse(c.Attribute("Descuento")?.Value ?? "0", CultureInfo.InvariantCulture),
                    ObjetoImpuesto = c.Attribute("ObjetoImp")?.Value
                }).ToList() ?? new List<ConceptoNominaViewModel>();

            // Descuento a nivel comprobante
            model.Descuento = decimal.Parse(comprobante?.Attribute("Descuento")?.Value ?? "0", CultureInfo.InvariantCulture);

            // Complemento Nómina
            var nominaNode = comprobante.Descendants(Nom + "Nomina").FirstOrDefault();
            if (nominaNode != null)
            {
                model.Nomina = new NominaViewModel
                {
                    Version = nominaNode.Attribute("Version")?.Value,
                    TipoNomina = nominaNode.Attribute("TipoNomina")?.Value,
                    FechaPago = DateTime.Parse(nominaNode.Attribute("FechaPago")?.Value ?? DateTime.MinValue.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture),
                    FechaInicialPago = DateTime.Parse(nominaNode.Attribute("FechaInicialPago")?.Value ?? DateTime.MinValue.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture),
                    FechaFinalPago = DateTime.Parse(nominaNode.Attribute("FechaFinalPago")?.Value ?? DateTime.MinValue.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture),
                    NumDiasPagados = decimal.Parse(nominaNode.Attribute("NumDiasPagados")?.Value ?? "0", CultureInfo.InvariantCulture),
                    TotalPercepciones = GetDecimalOrNull(nominaNode.Attribute("TotalPercepciones")?.Value),
                    TotalDeducciones = GetDecimalOrNull(nominaNode.Attribute("TotalDeducciones")?.Value),
                    TotalOtrosPagos = GetDecimalOrNull(nominaNode.Attribute("TotalOtrosPagos")?.Value)
                };

                MapNominaEmisor(nominaNode, model);
                MapNominaReceptor(nominaNode, model);
                MapPercepciones(nominaNode, model);
                MapDeducciones(nominaNode, model);
                MapOtrosPagos(nominaNode, model);
                MapIncapacidades(nominaNode, model);
            }
        }

        #region Métodos privados de mapeo Nómina

        private static void MapNominaEmisor(XElement nominaNode, CfdiNominaViewModel model)
        {
            var node = nominaNode.Element(Nom + "Emisor");
            if (node == null) return;

            model.Nomina.Emisor = new EmisorNominaViewModel
            {
                Curp = node.Attribute("Curp")?.Value,
                RegistroPatronal = node.Attribute("RegistroPatronal")?.Value,
                RfcPatronOrigen = node.Attribute("RfcPatronOrigen")?.Value
            };
        }

        private static void MapNominaReceptor(XElement nominaNode, CfdiNominaViewModel model)
        {
            var node = nominaNode.Element(Nom + "Receptor");
            if (node == null) return;

            model.Nomina.Receptor = new ReceptorNominaViewModel
            {
                Curp = node.Attribute("Curp")?.Value,
                NumSeguridadSocial = node.Attribute("NumSeguridadSocial")?.Value,
                FechaInicioRelLaboral = GetDateOrNull(node.Attribute("FechaInicioRelLaboral")?.Value),
                Antiguedad = node.Attribute("Antiguedad")?.Value,
                TipoContrato = node.Attribute("TipoContrato")?.Value,
                Sindicalizado = node.Attribute("Sindicalizado")?.Value,
                TipoRegimen = node.Attribute("TipoRegimen")?.Value,
                NumEmpleado = node.Attribute("NumEmpleado")?.Value,
                Departamento = node.Attribute("Departamento")?.Value,
                Puesto = node.Attribute("Puesto")?.Value,
                RiesgoPuesto = node.Attribute("RiesgoPuesto")?.Value,
                PeriodicidadPago = node.Attribute("PeriodicidadPago")?.Value,
                Banco = node.Attribute("Banco")?.Value,
                CuentaBancaria = node.Attribute("CuentaBancaria")?.Value,
                SalarioBaseCotApor = GetDecimalOrNull(node.Attribute("SalarioBaseCotApor")?.Value),
                SalarioDiarioIntegrado = GetDecimalOrNull(node.Attribute("SalarioDiarioIntegrado")?.Value),
                ClaveEntFed = node.Attribute("ClaveEntFed")?.Value
            };
        }

        private static void MapPercepciones(XElement nominaNode, CfdiNominaViewModel model)
        {
            var node = nominaNode.Element(Nom + "Percepciones");
            if (node == null) return;

            model.Nomina.Percepciones = new PercepcionesNominaViewModel
            {
                TotalSueldos = GetDecimalOrNull(node.Attribute("TotalSueldos")?.Value),
                TotalSeparacionIndemnizacion = GetDecimalOrNull(node.Attribute("TotalSeparacionIndemnizacion")?.Value),
                TotalJubilacionPensionRetiro = GetDecimalOrNull(node.Attribute("TotalJubilacionPensionRetiro")?.Value),
                TotalGravado = decimal.Parse(node.Attribute("TotalGravado")?.Value ?? "0", CultureInfo.InvariantCulture),
                TotalExento = decimal.Parse(node.Attribute("TotalExento")?.Value ?? "0", CultureInfo.InvariantCulture),
                PercepcionesDetalle = node.Elements(Nom + "Percepcion")
                    .Select(p => new PercepcionDetalleViewModel
                    {
                        TipoPercepcion = p.Attribute("TipoPercepcion")?.Value,
                        Clave = p.Attribute("Clave")?.Value,
                        Concepto = p.Attribute("Concepto")?.Value,
                        ImporteGravado = decimal.Parse(p.Attribute("ImporteGravado")?.Value ?? "0", CultureInfo.InvariantCulture),
                        ImporteExento = decimal.Parse(p.Attribute("ImporteExento")?.Value ?? "0", CultureInfo.InvariantCulture),
                        HorasExtra = p.Elements(Nom + "HorasExtra")
                            .Select(h => new HoraExtraViewModel
                            {
                                Dias = int.Parse(h.Attribute("Dias")?.Value ?? "0"),
                                TipoHoras = h.Attribute("TipoHoras")?.Value,
                                HorasExtra = int.Parse(h.Attribute("HorasExtra")?.Value ?? "0"),
                                ImportePagado = decimal.Parse(h.Attribute("ImportePagado")?.Value ?? "0", CultureInfo.InvariantCulture)
                            }).ToList()
                    }).ToList() ?? new List<PercepcionDetalleViewModel>()
            };
        }

        private static void MapDeducciones(XElement nominaNode, CfdiNominaViewModel model)
        {
            var node = nominaNode.Element(Nom + "Deducciones");
            if (node == null) return;

            model.Nomina.Deducciones = new DeduccionesNominaViewModel
            {
                TotalOtrasDeducciones = GetDecimalOrNull(node.Attribute("TotalOtrasDeducciones")?.Value),
                TotalImpuestosRetenidos = GetDecimalOrNull(node.Attribute("TotalImpuestosRetenidos")?.Value),
                DeduccionesDetalle = node.Elements(Nom + "Deduccion")
                    .Select(d => new DeduccionDetalleViewModel
                    {
                        TipoDeduccion = d.Attribute("TipoDeduccion")?.Value,
                        Clave = d.Attribute("Clave")?.Value,
                        Concepto = d.Attribute("Concepto")?.Value,
                        Importe = decimal.Parse(d.Attribute("Importe")?.Value ?? "0", CultureInfo.InvariantCulture)
                    }).ToList() ?? new List<DeduccionDetalleViewModel>()
            };
        }

        private static void MapOtrosPagos(XElement nominaNode, CfdiNominaViewModel model)
        {
            var node = nominaNode.Element(Nom + "OtrosPagos");
            if (node == null) return;

            model.Nomina.OtrosPagos = new OtrosPagosNominaViewModel
            {
                OtrosPagosDetalle = node.Elements(Nom + "OtroPago")
                    .Select(op => new OtroPagoDetalleViewModel
                    {
                        TipoOtroPago = op.Attribute("TipoOtroPago")?.Value,
                        Clave = op.Attribute("Clave")?.Value,
                        Concepto = op.Attribute("Concepto")?.Value,
                        Importe = decimal.Parse(op.Attribute("Importe")?.Value ?? "0", CultureInfo.InvariantCulture),
                        SubsidioAlEmpleo = op.Element(Nom + "SubsidioAlEmpleo") != null
                            ? new SubsidioAlEmpleoViewModel
                            {
                                SubsidioCausado = decimal.Parse(op.Element(Nom + "SubsidioAlEmpleo").Attribute("SubsidioCausado")?.Value ?? "0", CultureInfo.InvariantCulture)
                            }
                            : null
                    }).ToList() ?? new List<OtroPagoDetalleViewModel>()
            };
        }

        private static void MapIncapacidades(XElement nominaNode, CfdiNominaViewModel model)
        {
            var node = nominaNode.Element(Nom + "Incapacidades");
            if (node == null) return;

            model.Nomina.Incapacidades = node.Elements(Nom + "Incapacidad")
                .Select(i => new IncapacidadViewModel
                {
                    DiasIncapacidad = int.Parse(i.Attribute("DiasIncapacidad")?.Value ?? "0"),
                    TipoIncapacidad = i.Attribute("TipoIncapacidad")?.Value,
                    ImporteMonetario = GetDecimalOrNull(i.Attribute("ImporteMonetario")?.Value)
                }).ToList() ?? new List<IncapacidadViewModel>();
        }

        #endregion
    }
}
