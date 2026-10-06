using System.Diagnostics;
using CFDI.BuildPdf;

CfdiPdf.ConfigureQuestPdfLicense(CfdiPdfLicenseType.Community);

// Opciones nombradas (en cualquier posición): --pie "<texto>" y --pac RFC=Nombre (repetible);
// el resto son argumentos posicionales.
string? textoPie = null;
var nombresPac = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
var posicionales = new List<string>();
for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--pie" || args[i] == "--pac")
    {
        if (i + 1 >= args.Length)
        {
            Console.Error.WriteLine($"ERROR: {args[i]} requiere un valor.");
            return 1;
        }
        var valor = args[++i];
        if (args[i - 1] == "--pie")
        {
            textoPie = valor;
            continue;
        }
        var separador = valor.IndexOf('=');
        if (separador <= 0)
        {
            Console.Error.WriteLine($"ERROR: --pac espera RFC=Nombre, se recibió: {valor}");
            return 1;
        }
        nombresPac[valor[..separador].Trim()] = valor[(separador + 1)..].Trim();
        continue;
    }
    posicionales.Add(args[i]);
}

if (posicionales.Count == 0)
{
    PrintUsage();
    return 1;
}

var xmlPath = Path.GetFullPath(posicionales[0]);
if (!File.Exists(xmlPath))
{
    Console.Error.WriteLine($"ERROR: No existe el archivo XML: {xmlPath}");
    return 2;
}

var pdfPath = posicionales.Count > 1
    ? Path.GetFullPath(posicionales[1])
    : Path.ChangeExtension(xmlPath, ".pdf");

var logoPath = posicionales.Count > 2 ? Path.GetFullPath(posicionales[2]) : null;
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
if (textoPie != null)
    Console.WriteLine($"  Texto pie   : {textoPie}");
foreach (var (rfc, nombre) in nombresPac)
    Console.WriteLine($"  PAC         : {rfc} = {nombre}");
Console.WriteLine();

var options = new CfdiPdfOptions
{
    LogoBase64 = logoBase64,
    TextoPiePagina = textoPie,
    NombresPac = nombresPac
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
    Console.Error.WriteLine("  CFDI.BuildPdf.ConsoleDemo <ruta-xml> [ruta-pdf-salida] [ruta-logo] [--pie \"texto\"] [--pac RFC=Nombre ...]");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Ejemplos:");
    Console.Error.WriteLine("  CFDI.BuildPdf.ConsoleDemo ./ejemplo.xml");
    Console.Error.WriteLine("  CFDI.BuildPdf.ConsoleDemo ./ejemplo.xml ./salida.pdf");
    Console.Error.WriteLine("  CFDI.BuildPdf.ConsoleDemo ./ejemplo.xml ./salida.pdf ./logo.png");
    Console.Error.WriteLine("  CFDI.BuildPdf.ConsoleDemo ./ejemplo.xml ./salida.pdf --pie \"FOLIO INTERNO: HG-123456\"");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Si no se indica la ruta del PDF, se genera junto al XML con extensión .pdf.");
    Console.Error.WriteLine("El logo (PNG/JPG) se inyecta como LogoBase64 en CfdiPdfOptions.");
    Console.Error.WriteLine("--pie se inyecta como TextoPiePagina: se imprime en el pie de todas las hojas.");
    Console.Error.WriteLine("--pac (repetible) se inyecta en NombresPac: nombre del PAC por RFC para \"PAC QUE TIMBRÓ\".");
}
