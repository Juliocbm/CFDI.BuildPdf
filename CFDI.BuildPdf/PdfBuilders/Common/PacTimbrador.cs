using System;
using System.Collections.Generic;

namespace CFDI.BuildPdf.PdfBuilders.Common
{
    /// <summary>
    /// Texto del campo "PAC QUE TIMBRÓ". La librería no trae nombres de PAC: el RFC (atributo
    /// <c>RfcProvCertif</c> del timbre, el dato fiscal) siempre se imprime, y el nombre comercial solo si el
    /// consumidor lo configura en <see cref="CfdiPdfOptions.NombresPac"/>. Los PACs cambian de RFC, de nombre o
    /// pierden la autorización fuera del ciclo de versiones de la librería, así que ese dato es del consumidor.
    /// </summary>
    internal static class PacTimbrador
    {
        /// <summary>
        /// <c>Nombre (RFC)</c> si hay nombre configurado para el RFC; si no, solo el RFC.
        /// </summary>
        public static string Texto(string? rfcProvCertif, IDictionary<string, string>? nombresPac)
        {
            var rfc = rfcProvCertif?.Trim() ?? "";
            var nombre = Nombre(rfc, nombresPac);
            return nombre is null ? rfc : $"{nombre} ({rfc})";
        }

        /// <summary>
        /// Nombre configurado para el RFC (sin distinguir mayúsculas), o null si no hay.
        /// </summary>
        public static string? Nombre(string? rfcProvCertif, IDictionary<string, string>? nombresPac)
        {
            if (string.IsNullOrWhiteSpace(rfcProvCertif) || nombresPac is null)
                return null;

            var rfc = rfcProvCertif.Trim();
            // TryGetValue respeta el comparador del diccionario; el recorrido cubre uno sensible a mayúsculas.
            if (nombresPac.TryGetValue(rfc, out var nombre) && !string.IsNullOrWhiteSpace(nombre))
                return nombre.Trim();
            foreach (var par in nombresPac)
                if (string.Equals(par.Key?.Trim(), rfc, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(par.Value))
                    return par.Value.Trim();

            return null;
        }
    }
}
