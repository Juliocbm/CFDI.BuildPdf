using System;
using System.Collections.Generic;

namespace CFDI.BuildPdf.Models
{
    /// <summary>
    /// Clase base con las propiedades comunes de cualquier CFDI 4.0,
    /// independientemente del tipo de complemento.
    /// </summary>
    public abstract class CfdiViewModelBase
    {
        // CFDI Principal
        public string Version { get; set; }
        public string Serie { get; set; }
        public string Folio { get; set; }
        public string LugarExpedicion { get; set; }
        public DateTime FechaEmision { get; set; }
        public DateTime FechaCertificacion { get; set; }
        public string TipoCambio { get; set; }
        public string Moneda { get; set; }
        public string FormaPago { get; set; }
        public string MetodoPago { get; set; }
        public string TipoComprobante { get; set; }
        public string Exportacion { get; set; }
        public string UUID { get; set; }
        public string LogoBase64 { get; set; }

        // Emisor
        public string EmisorNombre { get; set; }
        public string EmisorRFC { get; set; }
        public string EmisorRegimenFiscal { get; set; }

        // Receptor
        public string ReceptorNombre { get; set; }
        public string ReceptorRFC { get; set; }
        public string ReceptorDomicilioFiscal { get; set; }
        public string ReceptorRegimenFiscal { get; set; }
        public string UsoCFDI { get; set; }

        // Totales
        public decimal SubTotal { get; set; }
        public decimal Total { get; set; }
        public string CantidadConLetra { get; set; }

        // Sellos
        public string SelloEmisor { get; set; }
        public string SelloSAT { get; set; }
        public string CadenaOriginalSAT { get; set; }
        public string NoCertificadoSAT { get; set; }
        public string NoCertificadoEmisor { get; set; }

        // PAC que timbró el comprobante
        public string RfcProvCertif { get; set; }

        // CFDI Relacionados (opcional — sólo presente cuando el XML incluye el nodo)
        public string TipoRelacion { get; set; }
        public List<string> RelacionadosUuids { get; set; } = new List<string>();

        // QR
        public string UrlQr { get; set; }
        public string QRCodeBase64 { get; set; }
    }
}
