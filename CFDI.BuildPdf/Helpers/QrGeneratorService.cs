using QRCoder;
using System;

namespace CFDI.BuildPdf.Helpers
{
    public static class QrGeneratorService
    {
        /// <summary>
        /// Genera un QR en Base64 a partir de una URL.
        /// </summary>
        /// <param name="url">URL a codificar en el QR</param>
        /// <returns>Cadena Base64 de la imagen PNG del QR</returns>
        public static string GenerateQr(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);

            var qrCodeBytes = qrCode.GetGraphic(20);
            return Convert.ToBase64String(qrCodeBytes);
        }
    }
}
