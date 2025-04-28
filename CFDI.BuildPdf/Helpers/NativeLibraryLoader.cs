using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CFDI.BuildPdf.Helpers
{
    public static class NativeLibraryLoader
    {
        private static bool _loaded = false;
        private static readonly object _lock = new();

        public static void EnsureNativeLibraryLoaded()
        {
            if (_loaded)
                return;

            lock (_lock)
            {
                if (_loaded)
                    return;

                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "CFDI.BuildPdf.NativeBinaries.libwkhtmltox.dll";

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
                    }

                    // Ruta donde lo vamos a extraer
                    var tempFolder = Path.Combine(Path.GetTempPath(), "CFDI.BuildPdf.Native");
                    Directory.CreateDirectory(tempFolder);

                    var dllPath = Path.Combine(tempFolder, "libwkhtmltox.dll");

                    if (!File.Exists(dllPath))
                    {
                        using var outputFile = File.Create(dllPath);
                        stream.CopyTo(outputFile);
                    }

                    // 🔥 MOSTRAR versión detectada antes de cargar
                    var versionInfo = FileVersionInfo.GetVersionInfo(dllPath);
                    Console.WriteLine($"✅ Detected libwkhtmltox.dll Version: {versionInfo.FileVersion}");


                    // Cargar la librería nativa
                    var handle = NativeLibrary.Load(dllPath);

                    if (handle == IntPtr.Zero)
                    {
                        throw new Exception("No se pudo cargar libwkhtmltox.dll correctamente.");
                    }
                }

                _loaded = true;
            }
        }
    }
}
