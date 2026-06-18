using System.Xml.Linq;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Mappers.Common;
using CFDI.BuildPdf.Models;
using Microsoft.Extensions.Logging;

namespace CFDI.BuildPdf.Mappers.Factura
{
    /// <summary>
    /// Mapper de factura CFDI 4.0 base (Ingreso/Egreso) sin complemento.
    /// Reutiliza el mapeo común de <see cref="BaseCfdiMapper{TModel}"/> y los helpers
    /// compartidos de conceptos/impuestos.
    /// </summary>
    internal class FacturaMapper : BaseCfdiMapper<CfdiFacturaViewModel>
    {
        public FacturaMapper(IQrGenerator qrGenerator, ILogger<FacturaMapper>? logger = null)
            : base(qrGenerator, logger) { }

        protected override CfdiFacturaViewModel CreateModel() => new();

        protected override void MapComplemento(XDocument xdoc, CfdiFacturaViewModel model)
        {
            var comprobante = xdoc.Root;

            model.CondicionesPago = comprobante?.Attribute("CondicionesDePago")?.Value;

            model.Conceptos = MapConceptos(comprobante);

            MapResumenImpuestos(comprobante, model.Conceptos,
                out var totalTras, out var totalRet,
                out var trasResumen, out var retResumen);
            model.TotalImpuestosTrasladados = totalTras;
            model.TotalImpuestosRetenidos = totalRet;
            model.TrasladosResumen = trasResumen;
            model.RetencionesResumen = retResumen;
        }
    }
}
