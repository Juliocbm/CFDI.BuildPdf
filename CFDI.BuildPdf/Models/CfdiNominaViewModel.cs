// CFDI.BuildPdf/Models/CfdiNominaViewModel.cs
using System;
using System.Collections.Generic;
using System.Globalization; // Required for CultureInfo

namespace CFDI.BuildPdf.Models
{
    public class CfdiNominaViewModel
    {
        // CFDI Principal
        public string Version { get; set; }
        public string Serie { get; set; }
        public string Folio { get; set; }
        public string LugarExpedicion { get; set; }
        public DateTime FechaEmision { get; set; }
        public DateTime FechaCertificacion { get; set; }
        public string EmisorNombre { get; set; }
        public string EmisorRFC { get; set; }
        public string EmisorRegimenFiscal { get; set; }
        public string ReceptorNombre { get; set; }
        public string ReceptorRFC { get; set; }
        public string ReceptorDomicilioFiscal { get; set; }
        public string ReceptorRegimenFiscal { get; set; }
        public string UsoCFDI { get; set; } // For Nomina, usually "CN01" (Nómina)
        public string TipoCambio { get; set; }
        public string Moneda { get; set; }
        public string FormaPago { get; set; } // Usually not applicable or "99" for Nomina
        public string MetodoPago { get; set; } // Usually "PUE" for Nomina
        public string TipoComprobante { get; set; } // "N" for Nómina
        public string Exportacion { get; set; }
        public string UUID { get; set; }
        public string LogoBase64 { get; set; }

        // Conceptos (usually one for Nomina, representing the total payment)
        public List<ConceptoNominaViewModel> Conceptos { get; set; } = new();

        // Totales
        public decimal SubTotal { get; set; }
        public decimal Total { get; set; }
        public decimal Descuento { get; set; } // From Nomina:TotalDeducciones
        public string CantidadConLetra { get; set; }

        // Nomina Complement
        public NominaViewModel Nomina { get; set; }

        // Sellos
        public string SelloEmisor { get; set; }
        public string SelloSAT { get; set; }
        public string CadenaOriginalSAT { get; set; }
        public string NoCertificadoSAT { get; set; }
        public string NoCertificadoEmisor { get; set; }

        //QR
        public string UrlQr { get; set; }
        public string QRCodeBase64 { get; set; }
    }

    public class ConceptoNominaViewModel
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

    public class NominaViewModel
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

    public class EmisorNominaViewModel
    {
        public string Curp { get; set; } // Optional CURP for employer (physical person)
        public string RegistroPatronal { get; set; } // Optional
        public string RfcPatronOrigen { get; set; } // Optional
    }

    public class ReceptorNominaViewModel
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

    public class PercepcionesNominaViewModel
    {
        public decimal? TotalSueldos { get; set; }
        public decimal? TotalSeparacionIndemnizacion { get; set; }
        public decimal? TotalJubilacionPensionRetiro { get; set; }
        public decimal TotalGravado { get; set; }
        public decimal TotalExento { get; set; }
        public List<PercepcionDetalleViewModel> PercepcionesDetalle { get; set; } = new();
    }

    public class PercepcionDetalleViewModel
    {
        public string TipoPercepcion { get; set; }
        public string Clave { get; set; }
        public string Concepto { get; set; }
        public decimal ImporteGravado { get; set; }
        public decimal ImporteExento { get; set; }
        public List<HoraExtraViewModel> HorasExtra { get; set; } = new();
        // AccionesOTitulos can be added here if needed
    }

    public class HoraExtraViewModel
    {
        public int Dias { get; set; }
        public string TipoHoras { get; set; } // "01" Dobles, "02" Triples, "03" Simples
        public int HorasExtra { get; set; }
        public decimal ImportePagado { get; set; }
    }

    public class DeduccionesNominaViewModel
    {
        public decimal? TotalOtrasDeducciones { get; set; }
        public decimal? TotalImpuestosRetenidos { get; set; }
        public List<DeduccionDetalleViewModel> DeduccionesDetalle { get; set; } = new();
    }

    public class DeduccionDetalleViewModel
    {
        public string TipoDeduccion { get; set; }
        public string Clave { get; set; }
        public string Concepto { get; set; }
        public decimal Importe { get; set; }
    }

    public class OtrosPagosNominaViewModel
    {
        public List<OtroPagoDetalleViewModel> OtrosPagosDetalle { get; set; } = new();
    }

    public class OtroPagoDetalleViewModel
    {
        public string TipoOtroPago { get; set; }
        public string Clave { get; set; }
        public string Concepto { get; set; }
        public decimal Importe { get; set; }
        public SubsidioAlEmpleoViewModel SubsidioAlEmpleo { get; set; }
        // CompensacionSaldosAFavor can be added here
    }

    public class SubsidioAlEmpleoViewModel
    {
        public decimal SubsidioCausado { get; set; }
    }

    public class IncapacidadViewModel
    {
        public int DiasIncapacidad { get; set; }
        public string TipoIncapacidad { get; set; }
        public decimal? ImporteMonetario { get; set; } // Optional
    }
}
