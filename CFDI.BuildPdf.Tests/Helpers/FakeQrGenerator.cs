using CFDI.BuildPdf.Abstractions;

namespace CFDI.BuildPdf.Tests.Helpers
{
    /// <summary>
    /// Implementación fake de IQrGenerator para tests unitarios.
    /// Evita la dependencia de QRCoder en los tests de mappers.
    /// </summary>
    internal class FakeQrGenerator : IQrGenerator
    {
        public string GenerateBase64(string url) => "FAKE_QR_BASE64";
    }
}
