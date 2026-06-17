using System;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Complements;
using CFDI.BuildPdf.Helpers;
using CFDI.BuildPdf.Mappers.CartaPorte;
using CFDI.BuildPdf.Mappers.Nomina;
using CFDI.BuildPdf.Models;
using CFDI.BuildPdf.PdfBuilders.CartaPorte;
using CFDI.BuildPdf.PdfBuilders.Nomina;
using CFDI.BuildPdf.Service;
using CFDI.BuildPdf.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CFDI.BuildPdf.Configuration
{
    /// <summary>
    /// Extensiones para registrar los servicios de CFDI.BuildPdf en un contenedor de DI.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registra todos los servicios necesarios para la generación de PDFs CFDI
        /// y establece la licencia QuestPDF a aplicar en el proceso.
        /// </summary>
        /// <param name="services">Colección de servicios del contenedor.</param>
        /// <param name="configure">Acción opcional para configurar <see cref="CfdiPdfOptions"/>.</param>
        /// <param name="licenseType">
        /// Tipo de licencia QuestPDF (Community por defecto). El consumidor es responsable
        /// de cumplir los términos comerciales de QuestPDF; consulta https://www.questpdf.com/license/.
        /// </param>
        /// <returns>La misma colección de servicios para encadenamiento.</returns>
        public static IServiceCollection AddCfdiPdfServices(
            this IServiceCollection services,
            Action<CfdiPdfOptions>? configure = null,
            CfdiPdfLicenseType licenseType = CfdiPdfLicenseType.Community)
        {
            if (services is null)
                throw new ArgumentNullException(nameof(services));

            QuestPDF.Settings.License = CfdiPdf.MapLicense(licenseType);

            if (configure != null)
                services.Configure(configure);

            // QR (Singleton: thread-safe, reutilizable)
            services.AddSingleton<IQrGenerator, QrGeneratorService>();

            // Document builders (Singleton: sin estado, reutilizables)
            services.AddSingleton<IPdfDocumentBuilder<CfdiCartaPorteViewModel>, CartaPorteDocumentBuilder>();
            services.AddSingleton<IPdfDocumentBuilder<CfdiNominaViewModel>, NominaDocumentBuilder>();

            // Servicios de dominio (Transient: sin estado, ligeros)
            services.AddTransient<ICfdiTypeDetector, CfdiTypeDetector>();
            services.AddTransient<ICfdiModelMapper<CfdiCartaPorteViewModel>, CartaPorteMapper>();
            services.AddTransient<ICfdiModelMapper<CfdiNominaViewModel>, NominaMapper>();

            // Handlers de complemento (Transient: sin estado, ligeros)
            services.AddTransient<ICfdiComplementHandler, CartaPorteComplementHandler>();
            services.AddTransient<ICfdiComplementHandler, NominaComplementHandler>();

            // Orquestador
            services.AddTransient<ICfdiPdfGenerator, CfdiPdfGenerator>();

            return services;
        }
    }
}
