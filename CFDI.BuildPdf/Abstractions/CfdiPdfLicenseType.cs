namespace CFDI.BuildPdf
{
    /// <summary>
    /// Tipo de licencia QuestPDF a aplicar en el proceso host.
    /// QuestPDF requiere que se declare explícitamente la licencia antes de generar cualquier documento.
    /// Consulta los términos comerciales en https://www.questpdf.com/license/ antes de desplegar en producción.
    /// </summary>
    public enum CfdiPdfLicenseType
    {
        /// <summary>
        /// Licencia Community (gratuita). Aplicable a proyectos open-source y a empresas que cumplan
        /// los requisitos de elegibilidad de QuestPDF (consulta la política vigente del proveedor).
        /// Es el valor por defecto si no se configura otra cosa.
        /// </summary>
        Community = 0,

        /// <summary>
        /// Licencia Professional (de pago). Requerida por QuestPDF para empresas que no califican para Community.
        /// El consumidor es responsable de adquirirla; esta librería solo la declara.
        /// </summary>
        Professional = 1,

        /// <summary>
        /// Licencia Enterprise (de pago). Requerida por QuestPDF para organizaciones grandes.
        /// El consumidor es responsable de adquirirla; esta librería solo la declara.
        /// </summary>
        Enterprise = 2
    }
}
