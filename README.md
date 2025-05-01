
# 📦 CFDI.BuildPdf

[![NuGet](https://img.shields.io/nuget/v/CFDI.BuildPdf.svg?style=flat-square)](https://www.nuget.org/packages/CFDI.BuildPdf/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/CFDI.BuildPdf.svg?style=flat-square)](https://www.nuget.org/packages/CFDI.BuildPdf/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)](https://opensource.org/licenses/MIT)
[![GitHub stars](https://img.shields.io/github/stars/Juliocbm/CFDI.BuildPdf?style=flat-square)](https://github.com/Juliocbm/CFDI.BuildPdf/stargazers)


## Descripción general
**CFDI.BuildPdf** es una librería .NET que permite generar de forma sencilla un PDF estilizado a partir de un XML CFDI 4.0 con complemento Carta Porte 3.1. Se enfoca en facilitar la integración en APIs o sistemas backend, permitiendo trabajar desde una ruta local, una cadena de texto XML o un arreglo de bytes.



## 📥 Instalación
### Desde NuGet:

```bash
dotnet add package CFDI.BuildPdf
```
## 🚀 Características

- ✔️ Soporte completo para **CFDI 4.0** con **Complemento Carta Porte 3.1**
- ✔️ Generación de PDF desde:
  - ✔️ Ruta física al archivo `.xml`
  - ✔️ String de XML
  - ✔️ Arreglo de bytes (`byte[]`)
- ✔️ Inclusión opcional de logotipo
- ✔️ Control sobre visualización de mercancías

## 📁 Estructura del PDF generado
**El PDF incluye secciones como**

- Datos del emisor y receptor
- UUID, fecha de certificación, certificados
- Totales del CFDI e impuestos
- Addenda genérica (si aplica)
- Complemento Carta Porte:
  - Ubicaciones
  - Mercancías (detalle o resumen)
  - Transporte, seguro, remolques
- QR y sellos digitales

## 📚 Requisitos
✔️ .NET 6.0 o superior

✔️ Requisitos del proyecto consumidor
Para que la generación de PDF funcione correctamente (especialmente al publicar o en entornos sin contexto de compilación como Azure o Docker), agrega lo siguiente en tu archivo .csproj del proyecto que consume la librería:

```xml
<PropertyGroup>
  <PreserveCompilationContext>true</PreserveCompilationContext>
  <CopyRefAssembliesToPublishDirectory>true</CopyRefAssembliesToPublishDirectory>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
</PropertyGroup>
```
✔️ Además, agrega estas dependencias para evitar errores de downgrade al publicar:
```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Win32.Primitives" Version="4.3.0" />
  <PackageReference Include="System.Net.Primitives" Version="4.3.0" />
</ItemGroup>
```



## 🧰 Métodos disponibles
La clase CfdiPdf expone métodos estáticos para generar el PDF a partir del XML del CFDI:

### DesdeRutaAsync
```csharp
Task<byte[]> DesdeRutaAsync(string rutaXml, bool mostrarMercancias = true, string? logoBase64 = null)
```
- ✔️ rutaXml: Ruta absoluta del archivo XML timbrado.
- ✔️ mostrarMercancias (opcional): Indica si se desea mostrar el detalle de mercancías en el PDF. Default: true.
- ✔️ logoBase64 (opcional): Cadena en base64 del logotipo a mostrar en el PDF.

### DesdeXmlStringAsync
```csharp
Task<byte[]> DesdeXmlStringAsync(string xmlContent, bool esContenidoXml, bool mostrarMercancias = true, string? logoBase64 = null)
```
- ✔️ xmlContent: Contenido del CFDI en string. Puede ser XML puro o una ruta.
- ✔️ esContenidoXml: Si es true, el valor de xmlContent es un XML en texto plano. Si es false, se trata de una ruta.
- ✔️ mostrarMercancias (opcional): Mostrar o no el detalle de mercancías. Default: true.
- ✔️ logoBase64 (opcional): Logotipo en base64.

### DesdeXmlBytesAsync
```csharp
Task<byte[]> DesdeXmlBytesAsync(byte[] xmlBytes, bool mostrarMercancias = true, string? logoBase64 = null)
```
- ✔️ xmlBytes: Contenido del XML como arreglo de bytes.
- ✔️ mostrarMercancias (opcional): Muestra o no mercancías. Default: true.
- ✔️ logoBase64 (opcional): Logotipo en base64.
## 🔵 Uso basico

### 📜 Desde un string XML
```csharp
using CFDI.BuildPdf;

var bytes = await CfdiPdf.DesdeXmlStringAsync(xmlString, esContenidoXml: true);

File.WriteAllBytes("cfdi_output.pdf", bytesPdf);
```

### 📁 Desde una ruta
```csharp
using CFDI.BuildPdf;

await CfdiPdf.DesdeRutaAsync("C:\\Users\\Test\\archivo.xml", mostrarMercancias: true);

File.WriteAllBytes("cfdi_output.pdf", bytesPdf);
```

### 📦 Desde un byte[]
```csharp
using CFDI.BuildPdf;

var bytes = await CfdiPdf.DesdeXmlBytesAsync(xmlBytes);

File.WriteAllBytes("cfdi_output.pdf", bytesPdf);
```

### ✨ Con logotipo personalizado (base64):
```csharp
using CFDI.BuildPdf;

var logoBytes = await File.ReadAllBytesAsync("C:\\Users\\Test\\logo.png");
var logoBase64 = Convert.ToBase64String(logoBytes);

await CfdiPdf.DesdeRutaAsync("C:\\Users\\Test\\archivo.xml", mostrarMercancias: true, logoBase64: logo);

File.WriteAllBytes("cfdi_output.pdf", bytesPdf);
```
## Author

- [@Juliocbm](https://github.com/Juliocbm)

