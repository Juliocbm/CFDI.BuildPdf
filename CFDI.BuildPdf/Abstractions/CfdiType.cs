namespace CFDI.BuildPdf.Abstractions
{
    /// <summary>
    /// Tipos de complemento CFDI soportados por la librería.
    /// </summary>
    internal enum CfdiType
    {
        /// <summary>Complemento Carta Porte 3.1.</summary>
        CartaPorte,

        /// <summary>Complemento Nómina 1.2.</summary>
        Nomina,

        /// <summary>CFDI sin complemento reconocido.</summary>
        Desconocido
    }
}
