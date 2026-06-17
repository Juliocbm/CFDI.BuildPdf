using System;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Service;
using CFDI.BuildPdf.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

            // Idempotente: no pisar una licencia ya configurada (evita degradación silenciosa).
            if (QuestPDF.Settings.License is null)
                QuestPDF.Settings.License = CfdiPdf.MapLicense(licenseType);

            if (configure != null)
                services.Configure(configure);

            // Orquestador construido por el composition root compartido (usa el ILoggerFactory del contenedor si está).
            services.AddTransient<ICfdiPdfGenerator>(sp => CfdiPdfFactory.CreateGenerator(sp.GetService<ILoggerFactory>()));

            // Utilidad pública de detección de tipo (no usada por el orquestador; su visibilidad se decide en F4).
            services.AddTransient<ICfdiTypeDetector, CfdiTypeDetector>();

            return services;
        }
    }
}
