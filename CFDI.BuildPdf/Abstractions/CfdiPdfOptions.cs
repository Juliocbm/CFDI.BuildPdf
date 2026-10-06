namespace CFDI.BuildPdf
{
    /// <summary>
    /// Opciones de configuración para la generación de PDFs CFDI.
    /// Centraliza todos los parámetros de personalización del PDF generado.
    /// </summary>
    public class CfdiPdfOptions
    {
        /// <summary>
        /// Si se debe mostrar el detalle de mercancías en Carta Porte. Default: true.
        /// </summary>
        public bool MostrarMercancias { get; set; } = true;

        /// <summary>
        /// Si se debe incluir la página de condiciones del contrato en Carta Porte. Default: true.
        /// </summary>
        public bool MostrarCondicionesContrato { get; set; } = true;

        /// <summary>
        /// Si se debe incluir la sección de addenda en el PDF. Default: true.
        /// </summary>
        public bool MostrarAddenda { get; set; } = true;

        /// <summary>
        /// Cadena Base64 del logo de la empresa (opcional).
        /// </summary>
        public string? LogoBase64 { get; set; }

        /// <summary>
        /// Orientación de la página del PDF. Default: Portrait.
        /// </summary>
        public PdfOrientation Orientacion { get; set; } = PdfOrientation.Portrait;

        /// <summary>
        /// Texto libre que se imprime centrado en el pie de página de todas las hojas, arriba de la
        /// leyenda y el paginado (por ejemplo un folio interno, una leyenda o un código de control).
        /// Admite saltos de línea; se muestran a lo más 2 renglones y el excedente se corta con "…".
        /// Null o vacío (default): no se imprime nada y el PDF queda igual que sin esta opción.
        /// </summary>
        public string? TextoPiePagina { get; set; }

        /// <summary>
        /// Nombres comerciales de PAC por RFC (atributo <c>RfcProvCertif</c> del timbre) para el campo
        /// "PAC QUE TIMBRÓ", que se imprime como <c>Nombre (RFC)</c>, o solo el RFC si no hay nombre.
        /// La librería no trae nombres de PAC (cambian de RFC, de nombre o de autorización fuera de su ciclo de
        /// versiones): los aporta el consumidor, normalmente desde configuración. Las claves no distinguen
        /// mayúsculas/minúsculas.
        /// A diferencia del resto de opciones, es un catálogo acumulable: los nombres configurados en el
        /// contenedor DI se combinan con los de la llamada (si un RFC está en ambos, gana el de la llamada).
        /// </summary>
        /// <example>
        /// <code>options.NombresPac["SST060807KU0"] = "Buzón E";</code>
        /// o en appsettings: <c>"CfdiPdf": { "NombresPac": { "SST060807KU0": "Buzón E" } }</c>.
        /// </example>
        public IDictionary<string, string> NombresPac { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Devuelve estas opciones con <see cref="NombresPac"/> combinado con <paramref name="nombresGlobales"/>
        /// (los de esta instancia ganan). No modifica esta instancia ni el diccionario global: si hay algo que
        /// combinar, trabaja sobre una copia.
        /// </summary>
        internal CfdiPdfOptions ConNombresPacGlobales(IDictionary<string, string>? nombresGlobales)
        {
            if (nombresGlobales is null || nombresGlobales.Count == 0)
                return this;

            var combinados = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var par in nombresGlobales)
                if (!string.IsNullOrWhiteSpace(par.Value))
                    combinados[par.Key.Trim()] = par.Value;
            foreach (var par in NombresPac ?? new Dictionary<string, string>())
                if (!string.IsNullOrWhiteSpace(par.Value))
                    combinados[par.Key.Trim()] = par.Value;

            var copia = (CfdiPdfOptions)MemberwiseClone();
            copia.NombresPac = combinados;
            return copia;
        }
    }

    /// <summary>
    /// Orientación de página para el PDF generado.
    /// </summary>
    public enum PdfOrientation
    {
        /// <summary>Vertical (predeterminado).</summary>
        Portrait,

        /// <summary>Horizontal.</summary>
        Landscape
    }
}
