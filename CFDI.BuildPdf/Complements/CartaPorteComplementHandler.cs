using System.Collections.Generic;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Models;

namespace CFDI.BuildPdf.Complements
{
    /// <summary>Handler del complemento Carta Porte 3.1.</summary>
    internal sealed class CartaPorteComplementHandler : ComplementHandlerBase<CfdiCartaPorteViewModel>
    {
        public CartaPorteComplementHandler(
            ICfdiModelMapper<CfdiCartaPorteViewModel> mapper,
            IPdfDocumentBuilder<CfdiCartaPorteViewModel> builder)
            : base(mapper, builder) { }

        public override IReadOnlyCollection<string> ComplementNamespaces { get; }
            = new[] { "http://www.sat.gob.mx/CartaPorte31" };
    }
}
