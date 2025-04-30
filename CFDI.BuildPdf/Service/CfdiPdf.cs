using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFDI.BuildPdf.Service
{
    public static class CfdiPdf
    {
        private static readonly PdfService _pdfService = new PdfService();

        // Desde una ruta física
        public static Task<byte[]> DesdeRutaAsync(string rutaXml, bool mostrarMercancias = true)
        {
            return _pdfService.GenerarPdfDesdeXmlAsync(rutaXml, mostrarMercancias);
        }

        // Desde un string XML
        public static Task<byte[]> DesdeXmlStringAsync(string xmlContent, bool mostrarMercancias = true)
        {
            return _pdfService.GenerarPdfDesdeXmlAsync(xmlContent, mostrarMercancias);
        }

        // Desde un arreglo de bytes
        public static Task<byte[]> DesdeXmlBytesAsync(byte[] xmlBytes, bool mostrarMercancias = true)
        {
            return _pdfService.GenerarPdfDesdeXmlAsync(xmlBytes, mostrarMercancias);
        }
    }
}
