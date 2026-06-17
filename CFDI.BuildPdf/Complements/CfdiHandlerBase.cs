using System;
using System.Xml.Linq;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Models;

namespace CFDI.BuildPdf.Complements
{
    /// <summary>
    /// Base de handlers: coordina mapper→builder y aplica las opciones comunes (logo).
    /// Las subclases definen únicamente <see cref="CanHandle"/> (y, si aplica, <see cref="Priority"/>).
    /// </summary>
    internal abstract class CfdiHandlerBase<TModel> : ICfdiComplementHandler
        where TModel : CfdiViewModelBase
    {
        private readonly ICfdiModelMapper<TModel> _mapper;
        private readonly IPdfDocumentBuilder<TModel> _builder;

        protected CfdiHandlerBase(ICfdiModelMapper<TModel> mapper, IPdfDocumentBuilder<TModel> builder)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        public abstract bool CanHandle(XDocument xdoc);

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
