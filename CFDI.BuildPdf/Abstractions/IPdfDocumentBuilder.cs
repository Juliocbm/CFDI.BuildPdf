using CFDI.BuildPdf.Models;

namespace CFDI.BuildPdf.Abstractions
{
    /// <summary>
    /// Contrato para construir un documento PDF directamente desde un ViewModel CFDI.
    /// Reemplaza la cadena IHtmlRenderer + IPdfConverter con una generación directa.
    /// </summary>
    /// <typeparam name="TModel">Tipo del ViewModel que hereda de <see cref="CfdiViewModelBase"/>.</typeparam>
    internal interface IPdfDocumentBuilder<in TModel> where TModel : CfdiViewModelBase
    {
        /// <summary>
        /// Genera un PDF en bytes a partir del ViewModel y las opciones proporcionadas.
        /// </summary>
        /// <param name="model">ViewModel con los datos del CFDI mapeado.</param>
        /// <param name="options">Opciones de generación del PDF.</param>
        /// <returns>Arreglo de bytes del PDF generado.</returns>
        byte[] Build(TModel model, CfdiPdfOptions options);
    }
}
