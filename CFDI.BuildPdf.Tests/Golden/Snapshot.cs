using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace CFDI.BuildPdf.Tests.Golden
{
    /// <summary>
    /// Snapshot por archivo: compara un texto actual contra un baseline commiteado.
    /// Si el baseline no existe, lo crea y falla pidiendo revisión (primera ejecución).
    /// El baseline se ubica junto a este archivo de test, en la carpeta Snapshots/.
    /// </summary>
    internal static class Snapshot
    {
        public static void Match(string actual, string snapshotFileName, [CallerFilePath] string callerFilePath = "")
        {
            var snapshotDir = Path.Combine(Path.GetDirectoryName(callerFilePath)!, "Snapshots");
            Directory.CreateDirectory(snapshotDir);
            var path = Path.Combine(snapshotDir, snapshotFileName);

            var normalizedActual = Normalize(actual);

            if (!File.Exists(path))
            {
                File.WriteAllText(path, normalizedActual);
                Assert.Fail($"Snapshot baseline creado: {path}. Revísalo, confírmalo y vuelve a ejecutar el test.");
            }

            var expected = Normalize(File.ReadAllText(path));
            Assert.Equal(expected, normalizedActual);
        }

        private static string Normalize(string text) => text.Replace("\r\n", "\n").TrimEnd();
    }
}
