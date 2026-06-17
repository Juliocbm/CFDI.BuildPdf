using System;
using System.Collections.Generic;

namespace CFDI.BuildPdf.Models
{
    /// <summary>
    /// ViewModel para CFDI 4.0 con complemento Nómina 1.2.
    /// Hereda propiedades comunes de <see cref="CfdiViewModelBase"/>.
    /// </summary>
    internal class CfdiNominaViewModel : CfdiViewModelBase
    {
        // Conceptos
        public List<ConceptoNominaViewModel> Conceptos { get; set; } = new();

        // Totales específicos
        public decimal Descuento { get; set; }

        // Complemento Nómina
        public NominaViewModel Nomina { get; set; }
    }

    internal class ConceptoNominaViewModel
    {
        public string ClaveProductoServicio { get; set; } // Usually "84111505" for Nomina
        public string NumeroIdentificacion { get; set; } // Can be employee ID or other internal ID
        public decimal Cantidad { get; set; } // Usually 1
        public string ClaveUnidad { get; set; } // Usually "ACT"
        public string Unidad { get; set; } // Usually "Actividad"
        public string Descripcion { get; set; } // Usually "Pago de nómina"
        public decimal ValorUnitario { get; set; } // Corresponds to Nomina:TotalPercepciones + Nomina:TotalOtrosPagos
        public decimal Importe { get; set; } // Same as ValorUnitario for Cantidad = 1
        public decimal Descuento { get; set; } // Corresponds to Nomina:TotalDeducciones
        public string ObjetoImpuesto { get; set; } // Usually "01" (No objeto de impuesto) for the main concept
    }

    internal class NominaViewModel
    {
        public string Version { get; set; } = "1.2";
        public string TipoNomina { get; set; } // O (Ordinaria), E (Extraordinaria)
        public DateTime FechaPago { get; set; }
        public DateTime FechaInicialPago { get; set; }
        public DateTime FechaFinalPago { get; set; }
        public decimal NumDiasPagados { get; set; }
        public decimal? TotalPercepciones { get; set; }
        public decimal? TotalDeducciones { get; set; }
        public decimal? TotalOtrosPagos { get; set; }

        public EmisorNominaViewModel Emisor { get; set; }
        public ReceptorNominaViewModel Receptor { get; set; }
        public PercepcionesNominaViewModel Percepciones { get; set; }
        public DeduccionesNominaViewModel Deducciones { get; set; }
        public OtrosPagosNominaViewModel OtrosPagos { get; set; }
        public List<IncapacidadViewModel> Incapacidades { get; set; } = new();
    }

    internal class EmisorNominaViewModel
    {
        public string Curp { get; set; } // Optional CURP for employer (physical person)
        public string RegistroPatronal { get; set; } // Optional
        public string RfcPatronOrigen { get; set; } // Optional
    }

    internal class ReceptorNominaViewModel
    {
        public string Curp { get; set; }
        public string NumSeguridadSocial { get; set; } // Optional
        public DateTime? FechaInicioRelLaboral { get; set; } // Optional
        public string Antiguedad { get; set; } // PnYnMwD format, e.g., P1Y2M10D (1 year, 2 months, 10 days)
        public string TipoContrato { get; set; }
        public string Sindicalizado { get; set; } // "Sí" or "No", optional
        public string TipoRegimen { get; set; }
        public string NumEmpleado { get; set; }
        public string Departamento { get; set; } // Optional
        public string Puesto { get; set; } // Optional
        public string RiesgoPuesto { get; set; } // Optional
        public string PeriodicidadPago { get; set; }
        public string Banco { get; set; } // Optional, e.g., "002" for BANAMEX
        public string CuentaBancaria { get; set; } // Optional
        public decimal? SalarioBaseCotApor { get; set; } // Optional
        public decimal? SalarioDiarioIntegrado { get; set; } // Optional
        public string ClaveEntFed { get; set; } // State where employee works
    }

    internal class PercepcionesNominaViewModel
    {
        public decimal? TotalSueldos { get; set; }
        public decimal? TotalSeparacionIndemnizacion { get; set; }
        public decimal? TotalJubilacionPensionRetiro { get; set; }
        public decimal TotalGravado { get; set; }
        public decimal TotalExento { get; set; }
        public List<PercepcionDetalleViewModel> PercepcionesDetalle { get; set; } = new();
    }

    internal class PercepcionDetalleViewModel
    {
        public string TipoPercepcion { get; set; }
        public string Clave { get; set; }
        public string Concepto { get; set; }
        public decimal ImporteGravado { get; set; }
        public decimal ImporteExento { get; set; }
        public List<HoraExtraViewModel> HorasExtra { get; set; } = new();
        // AccionesOTitulos can be added here if needed
    }

    internal class HoraExtraViewModel
    {
        public int Dias { get; set; }
        public string TipoHoras { get; set; } // "01" Dobles, "02" Triples, "03" Simples
        public int HorasExtra { get; set; }
        public decimal ImportePagado { get; set; }
    }

    internal class DeduccionesNominaViewModel
    {
        public decimal? TotalOtrasDeducciones { get; set; }
        public decimal? TotalImpuestosRetenidos { get; set; }
        public List<DeduccionDetalleViewModel> DeduccionesDetalle { get; set; } = new();
    }

    internal class DeduccionDetalleViewModel
    {
        public string TipoDeduccion { get; set; }
        public string Clave { get; set; }
        public string Concepto { get; set; }
        public decimal Importe { get; set; }
    }

    internal class OtrosPagosNominaViewModel
    {
        public List<OtroPagoDetalleViewModel> OtrosPagosDetalle { get; set; } = new();
    }

    internal class OtroPagoDetalleViewModel
    {
        public string TipoOtroPago { get; set; }
        public string Clave { get; set; }
        public string Concepto { get; set; }
        public decimal Importe { get; set; }
        public SubsidioAlEmpleoViewModel SubsidioAlEmpleo { get; set; }
        // CompensacionSaldosAFavor can be added here
    }

    internal class SubsidioAlEmpleoViewModel
    {
        public decimal SubsidioCausado { get; set; }
    }

    internal class IncapacidadViewModel
    {
        public int DiasIncapacidad { get; set; }
        public string TipoIncapacidad { get; set; }
        public decimal? ImporteMonetario { get; set; } // Optional
    }
}
