namespace CFDI.BuildPdf.PdfBuilders.Common
{
    /// <summary>
    /// Constantes de estilo compartidas entre los document builders de CFDI.
    /// </summary>
    internal static class PdfStyleConstants
    {
        public const string FontFamily = "Arial";

        // Jerarquía tipográfica
        public const float FontSizeVerySmall = 6f;
        public const float FontSizeSmall = 6.5f;
        public const float FontSizeDefault = 7.5f;
        public const float FontSizeLabel = 7f;
        public const float FontSizeSectionTitle = 8f;
        public const float FontSizeTitle = 9f;
        public const float FontSizeHeader = 10.5f;
        public const float FontSizeEmisorName = 12f;

        // Paleta
        public const string ColorHeaderBg = "#2C3E50";
        public const string ColorHeaderText = "#FFFFFF";
        public const string ColorSectionBg = "#E8ECF1";
        public const string ColorBorder = "#999999";
        public const string ColorBorderSoft = "#D0D0D0";
        public const string ColorText = "#222222";
        public const string ColorSecondaryText = "#555555";
        public const string ColorZebra = "#F7F7F9";
        public const string ColorAccent = "#1F4E79";
    }
}
