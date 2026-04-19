using System;
using System.Globalization;
using CFDI.BuildPdf.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CFDI.BuildPdf.PdfBuilders.Common
{
    /// <summary>
    /// Secciones compartidas de PDF reutilizadas entre los builders de CartaPorte y Nómina.
    /// </summary>
    internal static class CfdiPdfSections
    {
        /// <summary>
        /// Renderiza el footer fiscal: QR + sellos digitales.
        /// </summary>
        public static void ComposeFooterFiscal(IContainer container, CfdiViewModelBase model)
        {
            container.Column(col =>
            {
                col.Item().PaddingTop(10).Element(c => SectionTitle(c, "INFORMACIÓN FISCAL DIGITAL"));

                col.Item().PaddingTop(4).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3);
                        c.RelativeColumn(7);
                    });

                    table.Cell().Row(1).Column(1)
                        .AlignCenter().AlignMiddle()
                        .MinHeight(120)
                        .Element(cell =>
                        {
                            if (!string.IsNullOrEmpty(model.QRCodeBase64))
                            {
                                var qrBytes = Convert.FromBase64String(model.QRCodeBase64);
                                cell.AlignCenter().Width(110).Height(110).Image(qrBytes);
                            }
                        });

                    table.Cell().Row(1).Column(2).PaddingLeft(6).Column(right =>
                    {
                        right.Item().Text("SELLO DIGITAL DEL CFDI").Bold()
                            .FontSize(PdfStyleConstants.FontSizeSmall)
                            .FontColor(PdfStyleConstants.ColorAccent);
                        right.Item().PaddingTop(1).Text(model.SelloEmisor ?? "")
                            .FontSize(PdfStyleConstants.FontSizeVerySmall)
                            .FontColor(PdfStyleConstants.ColorSecondaryText);

                        right.Item().PaddingTop(4).Text("CADENA ORIGINAL DEL COMPLEMENTO DE CERTIFICACIÓN DIGITAL DEL SAT").Bold()
                            .FontSize(PdfStyleConstants.FontSizeSmall)
                            .FontColor(PdfStyleConstants.ColorAccent);
                        right.Item().PaddingTop(1).Text(model.CadenaOriginalSAT ?? "")
                            .FontSize(PdfStyleConstants.FontSizeVerySmall)
                            .FontColor(PdfStyleConstants.ColorSecondaryText);

                        right.Item().PaddingTop(4).Text("SELLO DIGITAL DEL SAT").Bold()
                            .FontSize(PdfStyleConstants.FontSizeSmall)
                            .FontColor(PdfStyleConstants.ColorAccent);
                        right.Item().PaddingTop(1).Text(model.SelloSAT ?? "")
                            .FontSize(PdfStyleConstants.FontSizeVerySmall)
                            .FontColor(PdfStyleConstants.ColorSecondaryText);
                    });
                });
            });
        }

        /// <summary>
        /// Renderiza una fila de tabla con encabezado (th) y valor (td).
        /// </summary>
        public static void HeaderValueRow(TableDescriptor table, uint row, uint startCol, string header, string? value)
        {
            table.Cell().Row(row).Column(startCol)
                .Border(0.5f).BorderColor(PdfStyleConstants.ColorBorderSoft)
                .Background(PdfStyleConstants.ColorSectionBg)
                .Padding(3).Text(header).Bold()
                .FontSize(PdfStyleConstants.FontSizeLabel)
                .FontColor(PdfStyleConstants.ColorText);

            table.Cell().Row(row).Column(startCol + 1)
                .Border(0.5f).BorderColor(PdfStyleConstants.ColorBorderSoft)
                .Padding(3).Text(value ?? "")
                .FontSize(PdfStyleConstants.FontSizeLabel)
                .FontColor(PdfStyleConstants.ColorText);
        }

        /// <summary>
        /// Renderiza el título de una sección como banner oscuro full-width con texto blanco.
        /// </summary>
        public static void SectionTitle(IContainer container, string title)
        {
            container.PaddingTop(8).Background(PdfStyleConstants.ColorHeaderBg)
                .PaddingVertical(3).PaddingHorizontal(6)
                .Text(title.ToUpperInvariant())
                .Bold()
                .FontSize(PdfStyleConstants.FontSizeSectionTitle)
                .FontColor(PdfStyleConstants.ColorHeaderText);
        }

        /// <summary>
        /// Renderiza una celda de encabezado de tabla (fondo oscuro, texto blanco bold uppercase).
        /// </summary>
        public static void TableHeaderCell(IContainer cell, string text)
        {
            cell.Background(PdfStyleConstants.ColorHeaderBg)
                .BorderColor(PdfStyleConstants.ColorHeaderBg)
                .PaddingVertical(2).PaddingHorizontal(2)
                .Text(text.ToUpperInvariant()).Bold()
                .FontSize(PdfStyleConstants.FontSizeVerySmall)
                .FontColor(PdfStyleConstants.ColorHeaderText);
        }

        /// <summary>
        /// Renderiza una celda de cuerpo de tabla con borde sutil.
        /// </summary>
        public static IContainer TableBodyCell(IContainer cell)
        {
            return cell.Border(0.3f).BorderColor(PdfStyleConstants.ColorBorderSoft).Padding(2);
        }

        /// <summary>
        /// Formatea un decimal como moneda MXN.
        /// </summary>
        public static string FormatCurrency(decimal value)
        {
            return value.ToString("C2", CultureInfo.GetCultureInfo("es-MX"));
        }

        /// <summary>
        /// Formatea un decimal a 6 posiciones.
        /// </summary>
        public static string Format6(decimal value)
        {
            return value.ToString("F6", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Formatea un decimal a 2 posiciones con separador de miles (es-MX).
        /// </summary>
        public static string Format2(decimal value)
        {
            return value.ToString("N2", CultureInfo.GetCultureInfo("es-MX"));
        }

        /// <summary>
        /// Traduce la clave SAT del catálogo c_TipoDeComprobante a su descripción.
        /// Si la clave no corresponde a un valor conocido, se devuelve tal cual.
        /// </summary>
        public static string NombreTipoComprobante(string? clave)
        {
            return clave switch
            {
                "I" => "Ingreso",
                "E" => "Egreso",
                "T" => "Traslado",
                "P" => "Pago",
                "N" => "Nómina",
                _ => clave ?? ""
            };
        }

        /// <summary>
        /// Traduce la clave SAT del catálogo c_TipoRelacion a su descripción.
        /// Si la clave no corresponde a un valor conocido, se devuelve tal cual.
        /// </summary>
        public static string NombreTipoRelacion(string? clave)
        {
            return clave switch
            {
                "01" => "Nota de crédito de los documentos relacionados",
                "02" => "Nota de débito de los documentos relacionados",
                "03" => "Devolución de mercancía sobre facturas o traslados previos",
                "04" => "Sustitución de los CFDI previos",
                "05" => "Traslados de mercancías facturados previamente",
                "06" => "Factura generada por los traslados previos",
                "07" => "CFDI por aplicación de anticipo",
                _ => clave ?? ""
            };
        }

        public static string NombreUsoCFDI(string? clave)
        {
            return clave switch
            {
                "G01" => "Adquisición de mercancías",
                "G02" => "Devoluciones, descuentos o bonificaciones",
                "G03" => "Gastos en general",
                "I01" => "Construcciones",
                "I02" => "Mobiliario y equipo de oficina por inversiones",
                "I03" => "Equipo de transporte",
                "I04" => "Equipo de computo y accesorios",
                "I05" => "Dados, troqueles, moldes, matrices y herramental",
                "I06" => "Comunicaciones telefónicas",
                "I07" => "Comunicaciones satelitales",
                "I08" => "Otra maquinaria y equipo",
                "D01" => "Honorarios médicos, dentales y gastos hospitalarios",
                "D02" => "Gastos médicos por incapacidad o discapacidad",
                "D03" => "Gastos funerales",
                "D04" => "Donativos",
                "D05" => "Intereses reales por créditos hipotecarios",
                "D06" => "Aportaciones voluntarias al SAR",
                "D07" => "Primas por seguros de gastos médicos",
                "D08" => "Gastos de transportación escolar obligatoria",
                "D09" => "Depósitos en cuentas para el ahorro",
                "D10" => "Pagos por servicios educativos (colegiaturas)",
                "S01" => "Sin efectos fiscales",
                "CP01" => "Pagos",
                "CN01" => "Nómina",
                _ => clave ?? ""
            };
        }

        public static string NombreExportacion(string? clave)
        {
            return clave switch
            {
                "01" => "No aplica",
                "02" => "Definitiva",
                "03" => "Temporal",
                "04" => "Definitiva con clave distinta a A1",
                _ => clave ?? ""
            };
        }

        public static string NombreMetodoPago(string? clave)
        {
            return clave switch
            {
                "PUE" => "Pago en una sola exhibición",
                "PPD" => "Pago en parcialidades o diferido",
                _ => clave ?? ""
            };
        }

        public static string NombreFormaPago(string? clave)
        {
            return clave switch
            {
                "01" => "Efectivo",
                "02" => "Cheque nominativo",
                "03" => "Transferencia electrónica de fondos",
                "04" => "Tarjeta de crédito",
                "05" => "Monedero electrónico",
                "06" => "Dinero electrónico",
                "08" => "Vales de despensa",
                "12" => "Dación en pago",
                "13" => "Pago por subrogación",
                "14" => "Pago por consignación",
                "15" => "Condonación",
                "17" => "Compensación",
                "23" => "Novación",
                "24" => "Confusión",
                "25" => "Remisión de deuda",
                "26" => "Prescripción o caducidad",
                "27" => "A satisfacción del acreedor",
                "28" => "Tarjeta de débito",
                "29" => "Tarjeta de servicios",
                "30" => "Aplicación de anticipos",
                "31" => "Intermediario pagos",
                "99" => "Por definir",
                _ => clave ?? ""
            };
        }

        public static string NombreRegimenFiscal(string? clave)
        {
            return clave switch
            {
                "601" => "General de Ley Personas Morales",
                "603" => "Personas Morales con Fines no Lucrativos",
                "605" => "Sueldos y Salarios e Ingresos Asimilados a Salarios",
                "606" => "Arrendamiento",
                "607" => "Régimen de Enajenación o Adquisición de Bienes",
                "608" => "Demás ingresos",
                "609" => "Consolidación",
                "610" => "Residentes en el Extranjero sin Establecimiento Permanente en México",
                "611" => "Ingresos por Dividendos (socios y accionistas)",
                "612" => "Personas Físicas con Actividades Empresariales y Profesionales",
                "614" => "Ingresos por intereses",
                "615" => "Régimen de los ingresos por obtención de premios",
                "616" => "Sin obligaciones fiscales",
                "620" => "Sociedades Cooperativas de Producción que optan por diferir sus ingresos",
                "621" => "Incorporación Fiscal",
                "622" => "Actividades Agrícolas, Ganaderas, Silvícolas y Pesqueras",
                "623" => "Opcional para Grupos de Sociedades",
                "624" => "Coordinados",
                "625" => "Régimen de las Actividades Empresariales con ingresos a través de Plataformas Tecnológicas",
                "626" => "Régimen Simplificado de Confianza",
                _ => clave ?? ""
            };
        }

        /// <summary>
        /// Diccionario RFC → nombre comercial del PAC (Proveedor Autorizado de Certificación).
        /// Las claves deben estar en MAYÚSCULAS. Amplía esta lista con los PACs que tu organización utilice.
        /// </summary>
        private static readonly System.Collections.Generic.Dictionary<string, string> PacsConocidos =
            new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "SST060807KU0", "Buzón E" },
                { "SED1102088J7", "InvoiceOne" },
                { "SAT970701NN3", "SAT (pruebas)" },
                // Agrega aquí los RFCs de los PACs adicionales con los que trabajes.
            };

        /// <summary>
        /// Traduce el RFC del PAC (atributo RfcProvCertif del TimbreFiscalDigital)
        /// a su nombre comercial. Si el RFC no está en el diccionario, se devuelve
        /// "PAC no identificado" como fallback visible para el lector.
        /// </summary>
        public static string NombrePac(string? rfcProvCertif)
        {
            if (string.IsNullOrWhiteSpace(rfcProvCertif))
                return "PAC no identificado";

            return PacsConocidos.TryGetValue(rfcProvCertif, out var nombre)
                ? nombre
                : "PAC no identificado";
        }

        /// <summary>
        /// Traduce la clave SAT del catálogo c_ObjetoImp a su descripción corta.
        /// Si la clave no corresponde a un valor conocido, se devuelve tal cual.
        /// </summary>
        public static string NombreObjetoImp(string? clave)
        {
            return clave switch
            {
                "01" => "No objeto de impuesto",
                "02" => "Sí objeto de impuesto",
                "03" => "Sí objeto, sin desglose",
                "04" => "Sí objeto, no causa",
                "05" => "Sí objeto, IVA crédito PODEBI",
                "06" => "Sí objeto IVA, no traslado",
                "07" => "No traslado IVA, sí IEPS",
                "08" => "No traslado IVA, no IEPS",
                _ => clave ?? ""
            };
        }

        /// <summary>
        /// Traduce el código SAT de impuesto a su nombre corto (001=ISR, 002=IVA, 003=IEPS).
        /// Si el valor no corresponde a un código conocido, se devuelve tal cual.
        /// </summary>
        public static string NombreImpuesto(string? codigo)
        {
            return codigo switch
            {
                "001" => "ISR",
                "002" => "IVA",
                "003" => "IEPS",
                _ => codigo ?? ""
            };
        }

        /// <summary>
        /// Formatea una tasa o cuota como porcentaje (0.160000 → "16.00%").
        /// Para TipoFactor "Cuota" devuelve el valor con 6 decimales sin símbolo.
        /// </summary>
        public static string FormatTasaOCuota(decimal tasaOCuota, string? tipoFactor)
        {
            if (string.Equals(tipoFactor, "Cuota", StringComparison.OrdinalIgnoreCase))
                return tasaOCuota.ToString("F6", CultureInfo.InvariantCulture);

            return (tasaOCuota * 100m).ToString("F2", CultureInfo.InvariantCulture) + "%";
        }

        /// <summary>
        /// Traduce la clave SAT del catálogo c_FiguraTransporte a su descripción.
        /// Devuelve el formato "clave - Descripción" (ej. "01 - Operador").
        /// Si la clave no corresponde a un valor conocido, se devuelve tal cual.
        /// </summary>
        public static string NombreTipoFigura(string? clave)
        {
            var descripcion = clave switch
            {
                "01" => "Operador",
                "02" => "Propietario",
                "03" => "Arrendador",
                "04" => "Notificado",
                _ => null
            };

            if (descripcion == null)
                return clave ?? "";

            return $"{clave} - {descripcion}";
        }

        /// <summary>
        /// Traduce la clave SAT del catálogo c_CveTransporte (vía de entrada/salida)
        /// a su descripción. Devuelve el formato "clave - Descripción" (ej. "01 - Autotransporte").
        /// Si la clave no corresponde a un valor conocido, se devuelve tal cual.
        /// </summary>
        public static string NombreCveTransporte(string? clave)
        {
            var descripcion = clave switch
            {
                "01" => "Autotransporte",
                "02" => "Transporte Marítimo",
                "03" => "Transporte Aéreo",
                "04" => "Transporte Ferroviario",
                _ => null
            };

            if (descripcion == null)
                return clave ?? "";

            return $"{clave} - {descripcion}";
        }

        /// <summary>
        /// Traduce la clave SAT del catálogo c_ClaveUnidad (unidad de medida) a su descripción.
        /// Cubre las unidades más utilizadas en CFDI. Si la clave no corresponde a un valor conocido,
        /// se devuelve tal cual (fallback seguro para el catálogo completo).
        /// </summary>
        public static string NombreClaveUnidad(string? clave)
        {
            return clave switch
            {
                // Múltiplos / fracciones / decimales
                "H87" => "Pieza",
                "XUN" => "Unidad",
                "EA"  => "Elemento",
                "E48" => "Unidad de Servicio",
                "ACT" => "Actividad",
                "E51" => "Trabajo",
                "A9"  => "Tarifa",
                "E54" => "Viaje",
                "E46" => "Elemento de consumo eléctrico",
                "E01" => "Pedazo",

                // Peso / masa
                "KGM" => "Kilogramo",
                "GRM" => "Gramo",
                "MGM" => "Miligramo",
                "TNE" => "Tonelada métrica",
                "LBR" => "Libra",
                "ONZ" => "Onza",

                // Longitud
                "MTR" => "Metro",
                "CMT" => "Centímetro",
                "MMT" => "Milímetro",
                "KMT" => "Kilómetro",
                "INH" => "Pulgada",
                "FOT" => "Pie",

                // Área
                "MTK" => "Metro cuadrado",
                "CMK" => "Centímetro cuadrado",
                "HAR" => "Hectárea",

                // Volumen
                "MTQ" => "Metro cúbico",
                "LTR" => "Litro",
                "MLT" => "Mililitro",
                "CMQ" => "Centímetro cúbico",
                "GLL" => "Galón",
                "BLL" => "Barril",

                // Tiempo
                "HUR" => "Hora",
                "MIN" => "Minuto",
                "SEC" => "Segundo",
                "DAY" => "Día",
                "WEE" => "Semana",
                "MON" => "Mes",
                "ANN" => "Año",

                // Empaque / presentación
                "XBX" => "Caja",
                "XBG" => "Bolsa",
                "XPK" => "Paquete",
                "XPA" => "Cajón",
                "XPL" => "Tarima",
                "XRO" => "Rollo",
                "XLT" => "Lote",
                "XTK" => "Tanque",
                "XCT" => "Cartón",
                "XCS" => "Estuche",
                "XCR" => "Cajón de cartón",
                "XCU" => "Cubo",
                "XDR" => "Tambor",
                "XST" => "Hoja",
                "XBA" => "Barril (contenedor)",
                "BB"  => "Caja base",
                "KT"  => "Kit",
                "SET" => "Conjunto",
                "PR"  => "Par",
                "DPC" => "Docenas de piezas",
                "11"  => "Equipos",
                "10"  => "Grupos",
                "4G"  => "Variedad",

                // Energía
                "KWH" => "Kilowatt-hora",
                "WTT" => "Watt",

                // Otros
                "BG"  => "Bolsa",
                "ROL" => "Rollo",

                _ => clave ?? ""
            };
        }

        /// <summary>
        /// Traduce la clave SAT del catálogo c_TipoPermiso (permiso SCT / SICT) a su descripción.
        /// Devuelve el formato "clave - Descripción" (ej. "TPAF01 - Autotransporte Federal de carga general").
        /// Si la clave no corresponde a un valor conocido, se devuelve tal cual.
        /// </summary>
        public static string NombrePermisoSCT(string? clave)
        {
            var descripcion = clave switch
            {
                "TPAF01" => "Autotransporte Federal de carga general",
                "TPAF02" => "Transporte privado de carga",
                "TPAF03" => "Autotransporte Federal de Carga Especializada de materiales y residuos peligrosos",
                "TPAF04" => "Transporte de automóviles sin rodar en vehículo tipo góndola",
                "TPAF05" => "Transporte de carga de gran peso y/o volumen de hasta 90 toneladas",
                "TPAF06" => "Transporte de carga especializada de gran peso y/o volumen de más 90 toneladas",
                "TPAF07" => "Transporte de madera en rollo",
                "TPAF08" => "Autotransporte Internacional de carga de largo recorrido",
                "TPAF09" => "Autotransporte Internacional de carga especializada de materiales y residuos peligrosos de largo recorrido",
                "TPAF10" => "Autotransporte de Carga General cuyo ámbito de aplicación comprende la franja fronteriza con Estados Unidos",
                "TPAF11" => "Autotransporte de Carga Especializada cuyo ámbito de aplicación comprende la franja fronteriza con Estados Unidos",
                "TPAF12" => "Servicio auxiliar de arrastre en las vías generales de comunicación",
                "TPAF13" => "Servicio auxiliar de servicios de arrastre, salvamento y depósito de vehículos en las vías generales de comunicación",
                "TPAF14" => "Servicio de paquetería y mensajería en las vías generales de comunicación",
                "TPAF15" => "Transporte especial para el tránsito de grúas industriales con peso máximo de 90 toneladas",
                "TPAF16" => "Empresas trasladistas de vehículos nuevos",
                "TPAF17" => "Empresas fabricantes o distribuidoras de vehículos nuevos",
                "TPAF18" => "Autorización expresa para circular en los caminos y puentes de jurisdicción federal con configuraciones de tractocamión doblemente articulado",
                "TPAF19" => "Permiso Especial para Autotransporte de Carga Especializada de Cabotaje",
                "TPAF20" => "Permiso Temporal para Navegación de Cabotaje",
                "TPTM01" => "Permiso temporal para navegación de cabotaje",
                "TPTM02" => "Concesión y/o autorización para el servicio regular nacional e internacional para empresas mexicanas",
                "TPTA01" => "Permisos para el servicio aéreo regular de empresas extranjeras",
                "TPTA02" => "Permisos para el servicio aéreo nacional e internacional no regular de fletamento",
                "TPTA03" => "Permisos para el servicio aéreo nacional e internacional no regular de taxis aéreos",
                "TPXX00" => "Permiso no comprendido en el catálogo",
                _ => null
            };

            if (descripcion == null)
                return clave ?? "";

            return $"{clave} - {descripcion}";
        }

        /// <summary>
        /// Traduce la clave SAT del catálogo c_ConfigAutotransporte (configuración vehicular)
        /// a su descripción. Devuelve el formato "clave - Descripción" (ej. "C2 - Camión Unitario").
        /// Si la clave no corresponde a un valor conocido, se devuelve tal cual.
        /// </summary>
        public static string NombreConfigVehicular(string? clave)
        {
            var descripcion = clave switch
            {
                "VL" => "Vehículo ligero de carga",
                "C2" => "Camión Unitario (2 ejes)",
                "C3" => "Camión Unitario (3 ejes)",
                "C2R2" => "Camión-Remolque (2-2 ejes)",
                "C3R2" => "Camión-Remolque (3-2 ejes)",
                "C2R3" => "Camión-Remolque (2-3 ejes)",
                "C3R3" => "Camión-Remolque (3-3 ejes)",
                "T2S1" => "Tractocamión Articulado (2-1 ejes)",
                "T2S2" => "Tractocamión Articulado (2-2 ejes)",
                "T2S3" => "Tractocamión Articulado (2-3 ejes)",
                "T3S1" => "Tractocamión Articulado (3-1 ejes)",
                "T3S2" => "Tractocamión Articulado (3-2 ejes)",
                "T3S3" => "Tractocamión Articulado (3-3 ejes)",
                "T2S1R2" => "Tractocamión Semirremolque-Remolque (2-1-2 ejes)",
                "T2S2R2" => "Tractocamión Semirremolque-Remolque (2-2-2 ejes)",
                "T2S1R3" => "Tractocamión Semirremolque-Remolque (2-1-3 ejes)",
                "T3S1R2" => "Tractocamión Semirremolque-Remolque (3-1-2 ejes)",
                "T3S1R3" => "Tractocamión Semirremolque-Remolque (3-1-3 ejes)",
                "T3S2R2" => "Tractocamión Semirremolque-Remolque (3-2-2 ejes)",
                "T3S2R3" => "Tractocamión Semirremolque-Remolque (3-2-3 ejes)",
                "T3S2R4" => "Tractocamión Semirremolque-Remolque (3-2-4 ejes)",
                "T2S2S2" => "Tractocamión Semirremolque-Semirremolque (2-2-2 ejes)",
                "T3S2S2" => "Tractocamión Semirremolque-Semirremolque (3-2-2 ejes)",
                "T3S3S2" => "Tractocamión Semirremolque-Semirremolque (3-3-2 ejes)",
                "OTROEVGP" => "Especializado de carga voluminosa y/o gran peso",
                "OTROSG" => "Servicio de grúas",
                "GPLUTA" => "Grúa Industrial",
                "GPLUTB" => "Grúa Tipo Canasta",
                "GPLUTC" => "Grúa Tipo Plataforma",
                "GPLUTD" => "Grúa Tipo Telescópica",
                _ => null
            };

            if (descripcion == null)
                return clave ?? "";

            return $"{clave} - {descripcion}";
        }

        /// <summary>
        /// Traduce la clave SAT del catálogo c_SubTipoRem (subtipo de remolque / semirremolque)
        /// a su descripción. Devuelve el formato "clave - Descripción" (ej. "CTR001 - Caballete").
        /// Si la clave no corresponde a un valor conocido, se devuelve tal cual.
        /// </summary>
        public static string NombreSubTipoRemolque(string? clave)
        {
            var descripcion = clave switch
            {
                "CTR001" => "Caballete",
                "CTR002" => "Caja",
                "CTR003" => "Caja Abierta",
                "CTR004" => "Caja Cerrada",
                "CTR005" => "Caja De Recolección Con Cargador Frontal",
                "CTR006" => "Caja Refrigerada",
                "CTR007" => "Caja Seca",
                "CTR008" => "Caja Transferencia",
                "CTR009" => "Cama Baja o Cuello Ganso",
                "CTR010" => "Chasis Portacontenedor",
                "CTR011" => "Convencional De Chasis",
                "CTR012" => "Equipo Especial",
                "CTR013" => "Estacas",
                "CTR014" => "Góndola Madrina",
                "CTR015" => "Grúa Industrial",
                "CTR016" => "Grúas",
                "CTR017" => "Integrales",
                "CTR018" => "Jaula",
                "CTR019" => "Media Redila",
                "CTR020" => "Pallet o Celdillas",
                "CTR021" => "Plataforma",
                "CTR022" => "Plataforma Con Grúas",
                "CTR023" => "Plataforma Encortinada",
                "CTR024" => "Redilas",
                "CTR025" => "Refrigerador",
                "CTR026" => "Revolvedora",
                "CTR027" => "Semieje",
                "CTR028" => "Tanque",
                "CTR029" => "Tolva",
                "CTR030" => "Tolva Grano",
                "CTR031" => "Volteo",
                "CTR032" => "Volteo Desmontable",
                _ => null
            };

            if (descripcion == null)
                return clave ?? "";

            return $"{clave} - {descripcion}";
        }
    }
}
