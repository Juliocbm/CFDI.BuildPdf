using System.Diagnostics;
using CFDI.BuildPdf.Abstractions;
using CFDI.BuildPdf.Service;

CfdiPdf.ConfigureQuestPdfLicense(CfdiPdfLicenseType.Community);

if (args.Length == 0)
{
    PrintUsage();
    return 1;
}

var xmlPath = Path.GetFullPath(args[0]);
if (!File.Exists(xmlPath))
{
    Console.Error.WriteLine($"ERROR: No existe el archivo XML: {xmlPath}");
    return 2;
}

var pdfPath = args.Length > 1
    ? Path.GetFullPath(args[1])
    : Path.ChangeExtension(xmlPath, ".pdf");

var logoPath = args.Length > 2 ? Path.GetFullPath(args[2]) : null;
string? logoBase64 = null;
if (!string.IsNullOrWhiteSpace(logoPath))
{
    if (!File.Exists(logoPath))
    {
        Console.Error.WriteLine($"ERROR: No existe el archivo de logo: {logoPath}");
        return 3;
    }
    logoBase64 = Convert.ToBase64String(await File.ReadAllBytesAsync(logoPath));
}

Console.WriteLine("CFDI.BuildPdf — Demo de consola");
Console.WriteLine($"  XML entrada : {xmlPath}");
Console.WriteLine($"  PDF salida  : {pdfPath}");
if (logoPath != null)
    Console.WriteLine($"  Logo        : {logoPath}");
Console.WriteLine();

var options = new CfdiPdfOptions
{
    LogoBase64 = logoBase64
};

try
{
    var sw = Stopwatch.StartNew();
    await CfdiPdf.GuardarDesdeRutaAsync(xmlPath, pdfPath, options);
    sw.Stop();

    var size = new FileInfo(pdfPath).Length;
    Console.WriteLine($"OK — PDF generado en {sw.ElapsedMilliseconds} ms ({size:N0} bytes).");
    return 0;
}
catch (CfdiXmlInvalidoException ex)
{
    Console.Error.WriteLine($"[XML inválido] {ex.Message}");
    return 10;
}
catch (CfdiComplementoNoSoportadoException ex)
{
    Console.Error.WriteLine($"[Complemento no soportado] {ex.Message}");
    return 11;
}
catch (FileNotFoundException ex)
{
    Console.Error.WriteLine($"[Archivo no encontrado] {ex.Message}");
    return 2;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[Error inesperado] {ex.GetType().Name}: {ex.Message}");
    return 99;
}

static void PrintUsage()
{
    Console.Error.WriteLine("Uso:");
    Console.Error.WriteLine("  CFDI.BuildPdf.ConsoleDemo <ruta-xml> [ruta-pdf-salida] [ruta-logo]");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Ejemplos:");
    Console.Error.WriteLine("  CFDI.BuildPdf.ConsoleDemo ./ejemplo.xml");
    Console.Error.WriteLine("  CFDI.BuildPdf.ConsoleDemo ./ejemplo.xml ./salida.pdf");
    Console.Error.WriteLine("  CFDI.BuildPdf.ConsoleDemo ./ejemplo.xml ./salida.pdf ./logo.png");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Si no se indica la ruta del PDF, se genera junto al XML con extensión .pdf.");
    Console.Error.WriteLine("El logo (PNG/JPG) se inyecta como LogoBase64 en CfdiPdfOptions.");
}
