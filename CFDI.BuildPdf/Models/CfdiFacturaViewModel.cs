using System.Collections.Generic;

namespace CFDI.BuildPdf.Models
{
    /// <summary>
    /// ViewModel para una factura CFDI 4.0 base (Ingreso/Egreso) sin complemento.
    /// Hereda las propiedades comunes de <see cref="CfdiViewModelBase"/>.
    /// </summary>
    internal class CfdiFacturaViewModel : CfdiViewModelBase
    {
        public string CondicionesPago { get; set; }

        public List<ConceptoViewModel> Conceptos { get; set; } = new();

        public decimal TotalImpuestosTrasladados { get; set; }
        public decimal TotalImpuestosRetenidos { get; set; }

        public List<ImpuestoConceptoViewModel> TrasladosResumen { get; set; } = new();
        public List<RetencionImpuestoViewModel> RetencionesResumen { get; set; } = new();
    }
}
