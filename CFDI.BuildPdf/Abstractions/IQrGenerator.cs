namespace CFDI.BuildPdf.Abstractions
{
    /// <summary>
    /// Contrato para generar códigos QR en formato Base64.
    /// </summary>
    public interface IQrGenerator
    {
        /// <summary>
        /// Genera una imagen QR codificada en Base64 a partir de una URL.
        /// </summary>
        /// <param name="url">URL a codificar en el QR.</param>
        /// <returns>Cadena Base64 de la imagen PNG del QR.</returns>
        string GenerateBase64(string url);
    }
}
