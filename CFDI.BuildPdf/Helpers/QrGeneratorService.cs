using QRCoder;
using System;
using CFDI.BuildPdf.Abstractions;

namespace CFDI.BuildPdf.Helpers
{
    /// <summary>
    /// Genera códigos QR en formato Base64 usando QRCoder.
    /// Implementa <see cref="IQrGenerator"/> para inyección de dependencias.
    /// </summary>
    internal class QrGeneratorService : IQrGenerator
    {
        /// <summary>
        /// Genera un QR en Base64 a partir de una URL (método estático para backward-compatibility).
        /// </summary>
        /// <param name="url">URL a codificar en el QR.</param>
        /// <returns>Cadena Base64 de la imagen PNG del QR.</returns>
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

        /// <inheritdoc />
        public string GenerateBase64(string url) => GenerateQr(url);
    }
}
