using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFDI.BuildPdf.Models
{
    public class CfdiCartaPorteViewModel
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
        public string UsoCFDI { get; set; }
        public string TipoCambio { get; set; }
        public string Moneda { get; set; }
        public string FormaPago { get; set; }
        public string MetodoPago { get; set; }
        public string TipoComprobante { get; set; }
        public string Exportacion { get; set; }
        public string CondicionesPago { get; set; }
        public string UUID { get; set; }


        // Conceptos
        public List<ConceptoViewModel> Conceptos { get; set; } = new();

        // Totales
        public decimal SubTotal { get; set; }
        public decimal Total { get; set; }
        public decimal TotalImpuestosTrasladados { get; set; }
        public string CantidadConLetra { get; set; }

        // Carta Porte
        public CartaPorteViewModel CartaPorte { get; set; }

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

    public class ConceptoViewModel
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
    }

    public class CartaPorteViewModel
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

    public class UbicacionViewModel
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


    public class MercanciaViewModel
    {
        public string Descripcion { get; set; }
        public decimal Cantidad { get; set; }
        public string ClaveUnidad { get; set; }
        public decimal PesoEnKg { get; set; }
        public decimal ValorMercancia { get; set; }

        public List<string> DocumentosAduaneros { get; set; } = new();
    }


    public class AutotransporteViewModel
    {
        public string PermisoSCT { get; set; }
        public string NumeroPermisoSCT { get; set; }
        public string ConfigVehicular { get; set; }
        public decimal PesoBrutoVehicular { get; set; }
        public string PlacaVM { get; set; }
        public int AnioModeloVM { get; set; }
    }

    public class SeguroViewModel
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

    public class RemolqueViewModel
    {
        public string SubTipoRemolque { get; set; }
        public string Placa { get; set; }
    }

    public class FiguraTransporteViewModel
    {
        public string TipoFigura { get; set; }
        public string RFCFigura { get; set; }
        public string NombreFigura { get; set; }
        public string NumeroLicencia { get; set; }
    }

}
