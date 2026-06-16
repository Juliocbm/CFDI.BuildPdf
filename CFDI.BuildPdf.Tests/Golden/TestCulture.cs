using System.Globalization;
using System.Runtime.CompilerServices;

namespace CFDI.BuildPdf.Tests.Golden
{
    /// <summary>
    /// Fija una cultura invariante para todos los tests del ensamblado, de modo que
    /// el formato de números/fechas sea reproducible entre máquinas de desarrollo y CI.
    /// </summary>
    internal static class TestCulture
    {
        [ModuleInitializer]
        internal static void Init()
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        }
    }
}
