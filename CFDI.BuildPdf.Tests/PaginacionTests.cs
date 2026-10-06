using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using CFDI.BuildPdf;
using CFDI.BuildPdf.Tests.Helpers;
using UglyToad.PdfPig;
using Xunit;

namespace CFDI.BuildPdf.Tests
{
    /// <summary>
    /// Regresión de paginación. Cada caso es una variante que en 3.2.0 dejaba un título o encabezado
    /// huérfano al pie, o una tabla que continuaba en la hoja siguiente sin encabezado o con una fila partida
    /// (barrido de 372 PDFs del 2026-10-05). Las variantes agregan copias de un nodo repetible del XML de
    /// prueba para mover los cortes de página.
    /// </summary>
    public class PaginacionTests
    {
        private const string Pie = "PIEMARK UNO";

        private static readonly string[] Titulos =
        {
            "ADDENDA GENÉRICA", "COMPLEMENTO CARTA PORTE", "CONCEPTO PRINCIPAL DEL COMPROBANTE", "CONCEPTOS FACTURADOS",
            "DATOS DE AUTOTRANSPORTE", "DATOS DEL COMPROBANTE DE NÓMINA", "DATOS DEL EMPLEADO", "DATOS DEL REMOLQUE",
            "DATOS DEL SEGURO", "DEDUCCIONES", "DETALLES DEL PAGO DE NÓMINA", "FIGURAS DE TRANSPORTE",
            "INFORMACIÓN FISCAL DIGITAL", "INCAPACIDADES", "MERCANCÍAS", "OTROS PAGOS", "PERCEPCIONES",
            "RESUMEN DE MERCANCÍAS", "TOTALES GENERALES DEL COMPROBANTE", "UBICACIONES",
        };

        // Último encabezado de cada tabla de datos: si el contenido de una hoja termina así, el encabezado quedó solo.
        private static readonly string[] UltimosEncabezados =
        {
            "OBJETO IMP.", "PAÍS", "VALOR MERCANCÍA", "LICENCIA", "DESCUENTO", "IMPORTE EXENTO", "IMPORTE", "IMPORTE MONETARIO",
        };

        private static string SinEspacios(string s) => Regex.Replace(s, @"\s+", "");

        private static async Task<List<string>> Hojas(string recurso, string nodo, int copias, bool conPie)
        {
            var doc = TestXmlLoader.Load($"CFDI.BuildPdf.Tests.TestData.{recurso}.xml");
            var original = doc.Descendants().First(e => e.Name.LocalName == nodo);
            for (var i = 0; i < copias; i++)
                original.AddAfterSelf(new XElement(original));

            var pdf = await CfdiPdf.DesdeXmlStringAsync(doc.ToString(), new CfdiPdfOptions { TextoPiePagina = conPie ? Pie : null });
            using var documento = PdfDocument.Open(pdf);
            return documento.GetPages().Select(p => SinEspacios(p.Text)).ToList();
        }

        /// <summary>Texto de la hoja antes del pie de página (texto libre, leyenda o paginado).</summary>
        private static string Contenido(string hoja)
        {
            var marcas = new[] { SinEspacios(Pie), SinEspacios("ESTE DOCUMENTO ES UNA REPRESENTACIÓN IMPRESA DE UN CFDI") };
            var corte = marcas.Select(m => hoja.LastIndexOf(m, System.StringComparison.Ordinal)).Where(i => i >= 0).DefaultIfEmpty(-1).Min();
            if (corte < 0)
                corte = hoja.LastIndexOf("Página", System.StringComparison.Ordinal);
            return corte >= 0 ? hoja[..corte] : hoja;
        }

        [Theory]
        [InlineData("cfdi_cartaporte", "Mercancia", 1, true)]    // FIGURAS DE TRANSPORTE
        [InlineData("cfdi_cartaporte", "Mercancia", 0, true)]    // encabezado de FIGURAS DE TRANSPORTE
        [InlineData("cfdi_cartaporte", "Mercancia", 5, false)]   // DATOS DEL REMOLQUE
        [InlineData("cfdi_cartaporte", "Mercancia", 9, true)]    // DATOS DEL SEGURO
        [InlineData("cfdi_cartaporte", "Mercancia", 16, true)]   // DATOS DE AUTOTRANSPORTE
        [InlineData("cfdi_factura_ingreso", "Concepto", 3, true)] // INFORMACIÓN FISCAL DIGITAL
        [InlineData("cfdi_nomina", "Percepcion", 3, false)]      // encabezado de OTROS PAGOS
        [InlineData("cfdi_nomina", "Percepcion", 3, true)]       // OTROS PAGOS
        [InlineData("cfdi_nomina", "Percepcion", 6, true)]       // encabezado de DEDUCCIONES
        [InlineData("cfdi_nomina", "Percepcion", 7, false)]      // DEDUCCIONES
        [InlineData("cfdi_nomina", "Percepcion", 28, false)]     // TOTALES GENERALES DEL COMPROBANTE
        public async Task NingunaHojaTerminaEnTituloOEncabezadoSolo(string recurso, string nodo, int copias, bool conPie)
        {
            var hojas = await Hojas(recurso, nodo, copias, conPie);

            Assert.True(hojas.Count > 1, "La variante debía producir más de una hoja.");
            for (var i = 0; i < hojas.Count - 1; i++)
            {
                var contenido = Contenido(hojas[i]);
                Assert.DoesNotContain(Titulos, t => contenido.EndsWith(SinEspacios(t), System.StringComparison.Ordinal));
                Assert.DoesNotContain(UltimosEncabezados, h => contenido.EndsWith(SinEspacios(h), System.StringComparison.Ordinal));
            }
        }

        [Theory]
        [InlineData("cfdi_factura_ingreso", "Concepto", 9, false, "CONCEPTOS FACTURADOS", "OBJETO IMP.01010101")]
        [InlineData("cfdi_cartaporte", "Mercancia", 19, true, "MERCANCÍAS", "VALOR MERCANCÍAProducto")]
        [InlineData("cfdi_nomina", "Percepcion", 8, true, "PERCEPCIONES", "IMPORTE EXENTO")]
        public async Task TablaQueContinua_RepiteTituloYEncabezadoSinPartirFilas(
            string recurso, string nodo, int copias, bool conPie, string titulo, string encabezadoYPrimeraFila)
        {
            var hojas = await Hojas(recurso, nodo, copias, conPie);

            // La hoja 2 continúa la tabla: arranca con el título y los encabezados repetidos, y la primera fila
            // de datos empieza completa (en 3.2.0 arrancaba con filas sueltas o con media fila).
            Assert.StartsWith(SinEspacios(titulo), hojas[1], System.StringComparison.Ordinal);
            Assert.Contains(SinEspacios(encabezadoYPrimeraFila), hojas[1], System.StringComparison.Ordinal);
        }
    }
}
