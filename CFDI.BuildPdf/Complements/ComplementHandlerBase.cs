using System;
using System.Collections.Generic;
using System.Xml.Linq;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Models;

namespace CFDI.BuildPdf.Complements
{
    /// <summary>
    /// Base para handlers de complemento: coordina el mapper y el builder de un tipo
    /// y aplica las opciones comunes (logo) en un solo lugar.
    /// </summary>
    internal abstract class ComplementHandlerBase<TModel> : ICfdiComplementHandler
        where TModel : CfdiViewModelBase
    {
        private readonly ICfdiModelMapper<TModel> _mapper;
        private readonly IPdfDocumentBuilder<TModel> _builder;

        protected ComplementHandlerBase(ICfdiModelMapper<TModel> mapper, IPdfDocumentBuilder<TModel> builder)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        public abstract IReadOnlyCollection<string> ComplementNamespaces { get; }

        public virtual int Priority => 0;

        public byte[] Generate(XDocument xdoc, CfdiPdfOptions options)
        {
            var model = _mapper.Map(xdoc);

            if (!string.IsNullOrEmpty(options.LogoBase64))
                model.LogoBase64 = options.LogoBase64;

            return _builder.Build(model, options);
        }
    }
}
