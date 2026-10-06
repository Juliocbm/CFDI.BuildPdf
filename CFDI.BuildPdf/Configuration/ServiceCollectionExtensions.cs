using System;
using CFDI.BuildPdf;
using CFDI.BuildPdf.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
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
        /// <param name="configure">
        /// Acción opcional para configurar las <see cref="CfdiPdfOptions"/> por defecto. Se aplican cuando una
        /// llamada a <see cref="ICfdiPdfGenerator"/> no trae opciones; si la llamada trae las suyas, se usan ésas
        /// completas (salvo <see cref="CfdiPdfOptions.NombresPac"/>, que se combina). También se respetan las registradas por otra vía, por ejemplo
        /// <c>services.Configure&lt;CfdiPdfOptions&gt;(configuration.GetSection("CfdiPdf"))</c>.
        /// </param>
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

            // Orquestador construido por el composition root compartido (usa el ILoggerFactory del contenedor si está)
            // con las opciones por defecto del contenedor: antes se registraban pero nunca se leían.
            services.AddTransient<ICfdiPdfGenerator>(sp => CfdiPdfFactory.CreateGenerator(
                sp.GetService<ILoggerFactory>(),
                sp.GetService<IOptions<CfdiPdfOptions>>()?.Value));

            return services;
        }
    }
}
