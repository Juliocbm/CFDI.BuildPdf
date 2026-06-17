using System;

namespace CFDI.BuildPdf
{
    /// <summary>
    /// Excepción base para errores de generación de PDFs CFDI.
    /// </summary>
    public class CfdiPdfException : Exception
    {
        /// <inheritdoc />
        public CfdiPdfException(string message) : base(message) { }

        /// <inheritdoc />
        public CfdiPdfException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Se lanza cuando el XML recibido no se puede parsear o no corresponde a un CFDI 4.0 válido.
    /// </summary>
    public class CfdiXmlInvalidoException : CfdiPdfException
    {
        /// <inheritdoc />
        public CfdiXmlInvalidoException(string message) : base(message) { }

        /// <inheritdoc />
        public CfdiXmlInvalidoException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Se lanza cuando el CFDI incluye un complemento aún no soportado por la librería.
    /// </summary>
    public class CfdiComplementoNoSoportadoException : CfdiPdfException
    {
        /// <inheritdoc />
        public CfdiComplementoNoSoportadoException(string message) : base(message) { }
    }
}
