using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Models;

namespace CFDI.BuildPdf.Complements
{
    /// <summary>
    /// Base para handlers de complemento que casan por namespace (Carta Porte, Nómina).
    /// </summary>
    internal abstract class ComplementHandlerBase<TModel> : CfdiHandlerBase<TModel>, IComplementNamespacesProvider
        where TModel : CfdiViewModelBase
    {
        protected ComplementHandlerBase(ICfdiModelMapper<TModel> mapper, IPdfDocumentBuilder<TModel> builder)
            : base(mapper, builder) { }

        /// <summary>Namespace(s) de complemento que este handler reconoce (incluye la versión).</summary>
        public abstract IReadOnlyCollection<string> ComplementNamespaces { get; }

        public override bool CanHandle(XDocument xdoc)
        {
            var root = xdoc.Root;
            if (root is null) return false;
            var present = new HashSet<string>(root.Descendants().Select(e => e.Name.NamespaceName));
            return ComplementNamespaces.Any(present.Contains);
        }
    }
}
