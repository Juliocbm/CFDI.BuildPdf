using System.Xml.Linq;

namespace CFDI.BuildPdf.Abstractions
{
    /// <summary>
    /// Contrato para mapear un XDocument CFDI a un ViewModel de complemento específico.
    /// </summary>
    /// <typeparam name="TModel">Tipo del ViewModel destino.</typeparam>
    public interface ICfdiModelMapper<TModel> where TModel : class
    {
        /// <summary>
        /// Determina si este mapper puede procesar el XDocument dado.
        /// </summary>
        /// <param name="xdoc">Documento XML CFDI.</param>
        /// <returns>true si el mapper reconoce el complemento del documento.</returns>
        bool CanMap(XDocument xdoc);

        /// <summary>
        /// Mapea el XDocument al ViewModel correspondiente.
        /// </summary>
        /// <param name="xdoc">Documento XML CFDI.</param>
        /// <returns>Instancia del ViewModel poblado con los datos del XML.</returns>
        TModel Map(XDocument xdoc);
    }
}
