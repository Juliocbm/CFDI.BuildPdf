using System.Xml.Linq;
using CFDI.BuildPdf.Abstractions;

namespace CFDI.BuildPdf.Complements
{
    /// <summary>
    /// Maneja la generación de PDF de un tipo de CFDI concreto.
    /// El orquestador elige el handler de mayor <see cref="Priority"/> cuyo
    /// <see cref="CanHandle"/> devuelva true para el documento.
    /// </summary>
    internal interface ICfdiComplementHandler
    {
        /// <summary>Indica si este handler puede procesar el CFDI dado.</summary>
        bool CanHandle(XDocument xdoc);

        /// <summary>Prioridad de desempate cuando varios handlers aplican. Mayor gana.</summary>
        int Priority { get; }

        /// <summary>Genera el PDF a partir del XML CFDI ya cargado.</summary>
        byte[] Generate(XDocument xdoc, CfdiPdfOptions options);
    }
}
