using System.Collections.Generic;

namespace CFDI.BuildPdf.Complements
{
    /// <summary>
    /// Implementado por los handlers que casan por namespace de complemento.
    /// El orquestador lo usa para validar que dos handlers no declaren el mismo namespace.
    /// </summary>
    internal interface IComplementNamespacesProvider
    {
        IReadOnlyCollection<string> ComplementNamespaces { get; }
    }
}
