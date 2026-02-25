namespace CFDI.BuildPdf.PdfBuilders.Common
{
    /// <summary>
    /// Constantes de estilo compartidas entre los document builders de CFDI.
    /// </summary>
    internal static class PdfStyleConstants
    {
        public const string FontFamily = "Arial";
        public const float FontSizeDefault = 8f;
        public const float FontSizeSmall = 7f;
        public const float FontSizeVerySmall = 6f;
        public const float FontSizeTitle = 10f;
        public const float FontSizeHeader = 12f;

        public const string ColorHeaderBg = "#F0F0F0";
        public const string ColorBorder = "#CCCCCC";
        public const string ColorText = "#333333";
        public const string ColorSecondaryText = "#555555";
    }
}
