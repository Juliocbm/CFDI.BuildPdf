using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CFDI.BuildPdf.Mappers
{
    public static class XmlNamespaceHelper
    {
        public static XNamespace GetCfdiNamespace(XDocument doc)
        {
            return doc.Root?.Name.Namespace ?? "http://www.sat.gob.mx/cfd/4";
        }

        public static XNamespace GetCartaPorteNamespace(XDocument doc)
        {
            var cartaPorteAttr = doc.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "CartaPorte");

            return cartaPorteAttr != null ? cartaPorteAttr.Name.Namespace : "http://www.sat.gob.mx/CartaPorte30";
        }
    }

}
