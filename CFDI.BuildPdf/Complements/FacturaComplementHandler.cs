using System;
using System.Xml.Linq;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Models;

namespace CFDI.BuildPdf.Complements
{
    /// <summary>
    /// Handler de factura CFDI 4.0 base (sin complemento Carta Porte ni Nómina).
    /// Aplica a comprobantes de tipo Ingreso (I) y Egreso (E). Prioridad mínima:
    /// solo se elige cuando ningún handler de complemento aplica.
    /// </summary>
    internal sealed class FacturaComplementHandler : CfdiHandlerBase<CfdiFacturaViewModel>
    {
        public FacturaComplementHandler(
            ICfdiModelMapper<CfdiFacturaViewModel> mapper,
            IPdfDocumentBuilder<CfdiFacturaViewModel> builder)
            : base(mapper, builder) { }

        public override int Priority => int.MinValue;

        public override bool CanHandle(XDocument xdoc)
        {
            var tipo = xdoc.Root?.Attribute("TipoDeComprobante")?.Value;
            return string.Equals(tipo, "I", StringComparison.Ordinal)
                || string.Equals(tipo, "E", StringComparison.Ordinal);
        }
    }
}
