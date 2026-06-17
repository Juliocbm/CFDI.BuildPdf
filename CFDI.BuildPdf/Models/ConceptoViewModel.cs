using System.Collections.Generic;

namespace CFDI.BuildPdf.Models
{
    internal class ConceptoViewModel
    {
        public string ClaveProductoServicio { get; set; }
        public string NumeroIdentificacion { get; set; }
        public decimal Cantidad { get; set; }
        public string ClaveUnidad { get; set; }
        public string Unidad { get; set; }
        public string Descripcion { get; set; }
        public decimal ValorUnitario { get; set; }
        public decimal Importe { get; set; }
        public decimal Descuento { get; set; }
        public string ObjetoImpuesto { get; set; }

        public List<ImpuestoConceptoViewModel> Traslados { get; set; } = new();
        public List<ImpuestoConceptoViewModel> Retenciones { get; set; } = new();
    }

    internal class ImpuestoConceptoViewModel
    {
        public string Impuesto { get; set; }       // IVA, ISR, etc.
        public string TipoFactor { get; set; }      // Tasa, Cuota, Exento
        public decimal TasaOCuota { get; set; }
        public decimal Base { get; set; }
        public decimal Importe { get; set; }
    }

    internal class RetencionImpuestoViewModel
    {
        public string Impuesto { get; set; }       // IVA, ISR, etc.
        public decimal Importe { get; set; }
    }
}
