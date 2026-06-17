using System.Collections.Generic;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Models;

namespace CFDI.BuildPdf.Complements
{
    /// <summary>Handler del complemento Nómina 1.2.</summary>
    internal sealed class NominaComplementHandler : ComplementHandlerBase<CfdiNominaViewModel>
    {
        public NominaComplementHandler(
            ICfdiModelMapper<CfdiNominaViewModel> mapper,
            IPdfDocumentBuilder<CfdiNominaViewModel> builder)
            : base(mapper, builder) { }

        public override IReadOnlyCollection<string> ComplementNamespaces { get; }
            = new[] { "http://www.sat.gob.mx/nomina12" };
    }
}
