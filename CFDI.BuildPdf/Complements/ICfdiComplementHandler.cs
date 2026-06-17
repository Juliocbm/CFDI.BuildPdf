using System.Collections.Generic;
using System.Xml.Linq;
using CFDI.BuildPdf.Abstractions;

namespace CFDI.BuildPdf.Complements
{
    /// <summary>
    /// Maneja la generación de PDF de un complemento CFDI concreto.
    /// El orquestador selecciona el handler cuyo <see cref="ComplementNamespaces"/> esté
    /// presente en el XML; <see cref="Priority"/> desempata si aplican varios al mismo documento.
    /// </summary>
    internal interface ICfdiComplementHandler
    {
        /// <summary>Namespace(s) de complemento que este handler reconoce (incluye la versión).</summary>
        IReadOnlyCollection<string> ComplementNamespaces { get; }

        /// <summary>Prioridad de desempate cuando varios handlers aplican al mismo documento. Mayor gana.</summary>
        int Priority { get; }

        /// <summary>Genera el PDF a partir del XML CFDI ya cargado.</summary>
        byte[] Generate(XDocument xdoc, CfdiPdfOptions options);
    }
}
