using System;
using System.Globalization;

namespace CFDI.BuildPdf.Mappers.Common
{
    /// <summary>
    /// Construye la URL de verificación QR del SAT para un CFDI.
    /// </summary>
    internal static class QrUrlBuilder
    {
        /// <summary>
        /// Construye la URL de verificación del SAT con los datos del CFDI.
        /// </summary>
        /// <param name="uuid">UUID del timbre fiscal digital.</param>
        /// <param name="rfcEmisor">RFC del emisor.</param>
        /// <param name="rfcReceptor">RFC del receptor.</param>
        /// <param name="total">Total del comprobante.</param>
        /// <param name="selloEmisor">Sello digital del emisor.</param>
        /// <returns>URL completa de verificación del SAT.</returns>
        public static string Construir(string uuid, string rfcEmisor, string rfcReceptor, decimal total, string selloEmisor)
        {
            string totalFormateado = total.ToString("000000000000000000.000000", CultureInfo.InvariantCulture).Replace(".", string.Empty);
            totalFormateado = totalFormateado.Insert(totalFormateado.Length - 6, ".");

            string fe = !string.IsNullOrEmpty(selloEmisor) && selloEmisor.Length >= 8
                ? selloEmisor.Substring(selloEmisor.Length - 8)
                : selloEmisor ?? string.Empty;

            return $"https://verificacfdi.facturaelectronica.sat.gob.mx/default.aspx?id={uuid}&re={rfcEmisor}&rr={rfcReceptor}&tt={totalFormateado}&fe={fe}";
        }
    }
}
