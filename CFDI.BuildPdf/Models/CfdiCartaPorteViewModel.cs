using System;
using System.Collections.Generic;

namespace CFDI.BuildPdf.Models
{
    /// <summary>
    /// ViewModel para CFDI 4.0 con complemento Carta Porte 3.1.
    /// Hereda propiedades comunes de <see cref="CfdiViewModelBase"/>.
    /// </summary>
    internal class CfdiCartaPorteViewModel : CfdiViewModelBase
    {
        public string CondicionesPago { get; set; }
        public AddendaViewModel Addenda { get; set; }

        // Conceptos
        public List<ConceptoViewModel> Conceptos { get; set; } = new();

        // Totales específicos
        public decimal TotalImpuestosTrasladados { get; set; }
        public decimal TotalImpuestosRetenidos { get; set; }

        /// <summary>
        /// Desglose agrupado de impuestos trasladados a nivel comprobante
        /// (ej. IVA 16.00% → 867.97). Se usa para renderizar el panel de totales.
        /// </summary>
        public List<ImpuestoConceptoViewModel> TrasladosResumen { get; set; } = new();

        /// <summary>
        /// Desglose agrupado de impuestos retenidos a nivel comprobante
        /// (ej. IVA → 216.99). Se usa para renderizar el panel de totales.
        /// </summary>
        public List<RetencionImpuestoViewModel> RetencionesResumen { get; set; } = new();

        // Carta Porte
        public CartaPorteViewModel CartaPorte { get; set; }
    }
    internal class AddendaViewModel
    {

        public bool IsParserGenerico { get; set; }
        public List<AddendaSeccionViewModel> Secciones { get; set; } = new();
        public string XmlRaw { get; set; }
    }

    internal class AddendaSeccionViewModel
    {
        public string NombreSeccion { get; set; }
        public List<KeyValuePair<string, string>> Campos { get; set; } = new();
    }

    internal class CartaPorteViewModel
    {
        public string IdCCP { get; set; }        
        public string Version { get; set; }
        public string TransporteInternacional { get; set; }
        public string EntradaSalidaMercancia { get; set; }
        public string ViaEntradaSalida { get; set; }
        public string PaisOrigenDestino { get; set; }
        public decimal DistanciaRecorrida { get; set; }

        public decimal PesoBrutoTotal { get; set; }
        public string UnidadPeso { get; set; }
        public int NumeroTotalMercancias { get; set; }

        public List<string> RegimenesAduaneros { get; set; } = new();
        public List<UbicacionViewModel> Ubicaciones { get; set; } = new();
        public List<MercanciaViewModel> MercanciasDetalle { get; set; } = new();
        public AutotransporteViewModel Autotransporte { get; set; }
        public SeguroViewModel Seguro { get; set; }
        public RemolqueViewModel Remolque { get; set; }
        //public FiguraTransporteViewModel FiguraTransporte { get; set; }
        public List<FiguraTransporteViewModel> FigurasTransporte { get; set; } = new();


    }

    internal class UbicacionViewModel
    {
        public string TipoUbicacion { get; set; }
        public string IDUbicacion { get; set; }
        public string RFCRemitenteDestinatario { get; set; }
        public string NombreRemitenteDestinatario { get; set; }
        public DateTime? FechaHoraSalidaLlegada { get; set; }
        public string CodigoPostal { get; set; }
        public string Municipio { get; set; }
        public string Localidad { get; set; }
        public string Estado { get; set; }
        public string Pais { get; set; }
        public string NumRegIdTrib { get; set; }
        public string ResidenciaFiscal { get; set; }
    }


    internal class MercanciaViewModel
    {
        public string Descripcion { get; set; }
        public decimal Cantidad { get; set; }
        public string ClaveUnidad { get; set; }
        public decimal PesoEnKg { get; set; }
        public decimal ValorMercancia { get; set; }

        public List<string> DocumentosAduaneros { get; set; } = new();
    }


    internal class AutotransporteViewModel
    {
        public string PermisoSCT { get; set; }
        public string NumeroPermisoSCT { get; set; }
        public string ConfigVehicular { get; set; }
        public decimal PesoBrutoVehicular { get; set; }
        public string PlacaVM { get; set; }
        public int AnioModeloVM { get; set; }
    }

    internal class SeguroViewModel
    {
        // Seguro de Responsabilidad Civil
        public string AseguradoraResponsabilidadCivil { get; set; }
        public string PolizaResponsabilidadCivil { get; set; }

        // Seguro de Carga
        public string AseguradoraCarga { get; set; }
        public string PolizaCarga { get; set; }

        // Seguro de Medio Ambiente
        public string AseguradoraMedAmbiente { get; set; }
        public string PolizaMedAmbiente { get; set; }
    }

    internal class RemolqueViewModel
    {
        public string SubTipoRemolque { get; set; }
        public string Placa { get; set; }
    }

    internal class FiguraTransporteViewModel
    {
        public string TipoFigura { get; set; }
        public string RFCFigura { get; set; }
        public string NombreFigura { get; set; }
        public string NumeroLicencia { get; set; }
    }

}
