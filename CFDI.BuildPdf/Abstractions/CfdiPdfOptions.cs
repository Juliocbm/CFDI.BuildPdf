namespace CFDI.BuildPdf
{
    /// <summary>
    /// Opciones de configuración para la generación de PDFs CFDI.
    /// Centraliza todos los parámetros de personalización del PDF generado.
    /// </summary>
    public class CfdiPdfOptions
    {
        /// <summary>
        /// Si se debe mostrar el detalle de mercancías en Carta Porte. Default: true.
        /// </summary>
        public bool MostrarMercancias { get; set; } = true;

        /// <summary>
        /// Si se debe incluir la página de condiciones del contrato en Carta Porte. Default: true.
        /// </summary>
        public bool MostrarCondicionesContrato { get; set; } = true;

        /// <summary>
        /// Si se debe incluir la sección de addenda en el PDF. Default: true.
        /// </summary>
        public bool MostrarAddenda { get; set; } = true;

        /// <summary>
        /// Cadena Base64 del logo de la empresa (opcional).
        /// </summary>
        public string? LogoBase64 { get; set; }

        /// <summary>
        /// Orientación de la página del PDF. Default: Portrait.
        /// </summary>
        public PdfOrientation Orientacion { get; set; } = PdfOrientation.Portrait;
    }

    /// <summary>
    /// Orientación de página para el PDF generado.
    /// </summary>
    public enum PdfOrientation
    {
        /// <summary>Vertical (predeterminado).</summary>
        Portrait,

        /// <summary>Horizontal.</summary>
        Landscape
    }
}
