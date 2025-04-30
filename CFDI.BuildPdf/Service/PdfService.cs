using RazorLight;
using DinkToPdf;
using DinkToPdf.Contracts;
using System.Xml.Linq;
using CFDI.BuildPdf.Helpers;
using CFDI.BuildPdf.Mappers;
using CFDI.BuildPdf.Models;
using Microsoft.AspNetCore.Razor.Language;
using System.Dynamic;
using System.Text;
using System;

namespace CFDI.BuildPdf.Service
{
    public class PdfService
    {
        private readonly IRazorLightEngine _razorEngine;
        private readonly IConverter _pdfConverter;

        public PdfService()
        {
            // Cargamos la DLL nativa libwkhtmltox desde EmbeddedResource
            NativeLibraryLoader.EnsureNativeLibraryLoaded();

            _razorEngine = new RazorLightEngineBuilder()
                .UseEmbeddedResourcesProject(typeof(CFDI.BuildPdf.Templates.TemplateFacturaCartaPorte))
                .UseMemoryCachingProvider()
                .EnableDebugMode()
                .Build();

            _pdfConverter = new SynchronizedConverter(new PdfTools());
        }

        //public async Task<byte[]> GenerarPdfDesdeXmlAsync(string rutaXml, bool mostrarMercancias = true, string? logoBase64 = null)
        //{
        //    using var reader = new StreamReader(rutaXml, System.Text.Encoding.UTF8);
        //    var xdoc = XDocument.Load(reader);

        //    var model = XmlToModelMapper.Map(xdoc);

        //    if (!string.IsNullOrEmpty(logoBase64))
        //    {
        //        model.LogoBase64 = logoBase64;
        //    }

        //    dynamic viewBag = new ExpandoObject();
        //    viewBag.MostrarMercancias = mostrarMercancias;

        //    var htmlCfdi = await _razorEngine.CompileRenderAsync("TemplateFacturaCartaPorte", model, (ExpandoObject)viewBag);
        //    var htmlCondiciones = await _razorEngine.CompileRenderAsync("TemplateCondicionesContrato", model);

        //    // 🔥 Forzar que el HTML tenga DOCTYPE
        //    var htmlFinal = "<!DOCTYPE html>\n" + htmlCfdi + "<div style='page-break-before: always;'></div>" + htmlCondiciones;

        //    var pdfBytes = ConvertHtmlToPdf(htmlFinal);

        //    return pdfBytes;
        //}

        // Método principal: desde ruta física
        public async Task<byte[]> GenerarPdfDesdeXmlAsync(string rutaXml, bool mostrarMercancias = true)
        {
            var xdoc = XDocument.Load(rutaXml);
            return await RenderPdf(xdoc, mostrarMercancias);
        }

        // Desde byte[]
        public async Task<byte[]> GenerarPdfDesdeXmlAsync(byte[] xmlBytes, bool mostrarMercancias = true)
        {
            using var ms = new MemoryStream(xmlBytes);
            var xdoc = XDocument.Load(ms);
            return await RenderPdf(xdoc, mostrarMercancias);
        }

        // Desde string XML en memoria
        public async Task<byte[]> GenerarPdfDesdeXmlAsync(string xmlContenido, bool esContenidoXml, bool mostrarMercancias = true)
        {
            if (!esContenidoXml)
                throw new ArgumentException("Si usas esta sobrecarga, 'esContenidoXml' debe ser true.");

            using var reader = new StringReader(xmlContenido);
            var xdoc = XDocument.Load(reader);
            return await RenderPdf(xdoc, mostrarMercancias);
        }

        // Método central reutilizado por todas las sobrecargas
        private async Task<byte[]> RenderPdf(XDocument xdoc, bool mostrarMercancias)
        {
            var model = XmlToModelMapper.Map(xdoc);

            dynamic viewBag = new System.Dynamic.ExpandoObject();
            viewBag.MostrarMercancias = mostrarMercancias;

            var htmlCfdi = await _razorEngine.CompileRenderAsync("TemplateFacturaCartaPorte", model, (ExpandoObject)viewBag);
            var htmlCondiciones = await _razorEngine.CompileRenderAsync("TemplateCondicionesContrato", model);

            string htmlFinal = htmlCfdi + "<div style='page-break-before: always;'></div>" + htmlCondiciones;
            return ConvertHtmlToPdf(htmlFinal);
        }

        private byte[] ConvertHtmlToPdf(string html)
        {

            var tempHtmlPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.html");

            File.WriteAllText(tempHtmlPath, html, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            var document = new HtmlToPdfDocument
            {
                GlobalSettings = {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.Letter,
                Margins = new MarginSettings { Top = 10, Bottom = 15 }
                 },
                    Objects = {
                        new ObjectSettings {
                            Page = tempHtmlPath, // 🔥 NO usamos HtmlContent, usamos archivo temp
                            WebSettings = {
                                DefaultEncoding = "utf-8"
                            },
                             FooterSettings = {
                                FontSize = 7,
                                Right = "Página [page] de [topage]",
                                Center = "ESTE DOCUMENTO ES UNA REPRESENTACIÓN IMPRESA DE UN CFDI",
                                //Line = true, // Línea arriba del footer
                                Spacing = 5
                            }
                        }
                }
            };


            var pdfBytes = _pdfConverter.Convert(document);

            // 🔥 Eliminar el archivo temporal después de convertir
            if (File.Exists(tempHtmlPath))
            {
                File.Delete(tempHtmlPath);
            }

            return pdfBytes;
        }
    }
}
