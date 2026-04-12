# Catálogos SAT mapeados — CFDI.BuildPdf

Inventario completo de los campos del PDF donde se resuelve una **clave SAT → descripción legible**. Cada campo usa un helper estático en `CfdiPdfSections.cs` que traduce la clave del catálogo SAT a su nombre descriptivo. Si la clave no está en el catálogo soportado, se devuelve tal cual (fallback seguro, sin excepción).

---

## Resumen

- **11 helpers** distintos
- **22 campos** del PDF donde se aplica traducción clave → descripción
- Aplica tanto a **Carta Porte** como a **Nómina**

---

## Tabla de campos mapeados

| Helper | Campo del PDF | Sección | Catálogo SAT |
|---|---|---|---|
| `NombreClaveUnidad` | Columna **Clave Unidad** de Conceptos Facturados | Conceptos | `c_ClaveUnidad` |
| `NombreClaveUnidad` | Columna **Clave Unidad** de Mercancías del complemento | Complemento Carta Porte → Mercancías | `c_ClaveUnidad` |
| `NombreImpuesto` | Columna **Impuesto** de la mini-tabla de traslados por concepto | Conceptos → Impuestos Trasladados | `c_Impuesto` (001=ISR, 002=IVA, 003=IEPS) |
| `NombreImpuesto` | Etiqueta de cada traslado en el **panel de totales** | Resumen de Totales → Impuestos Trasladados | `c_Impuesto` |
| `NombreImpuesto` | Etiqueta de cada retención en el **panel de totales** | Resumen de Totales → Impuestos Retenidos | `c_Impuesto` |
| `NombreObjetoImp` | Columna **Objeto Imp.** de Conceptos | Conceptos | `c_ObjetoImp` |
| `NombreUsoCFDI` | Campo **Uso del CFDI** del bloque Cliente | Cliente | `c_UsoCFDI` |
| `NombreUsoCFDI` | Campo **Uso CFDI** en Datos del Comprobante (Nómina) | Datos del Comprobante de Nómina | `c_UsoCFDI` |
| `NombreRegimenFiscal` | Campo **Régimen Fiscal** del Emisor en encabezado | Encabezado | `c_RegimenFiscal` |
| `NombreRegimenFiscal` | Campo **Régimen Fiscal** del Receptor en bloque Cliente | Cliente | `c_RegimenFiscal` |
| `NombreRegimenFiscal` | Campo **Régimen Fiscal** del Emisor (Nómina) | Encabezado Nómina | `c_RegimenFiscal` |
| `NombreRegimenFiscal` | Campo **Régimen Fiscal Receptor** (Nómina) | Datos del Comprobante de Nómina | `c_RegimenFiscal` |
| `NombreFormaPago` | Campo **Forma de Pago** | Forma / Método de Pago | `c_FormaPago` |
| `NombreMetodoPago` | Campo **Método de Pago** | Forma / Método de Pago | `c_MetodoPago` |
| `NombreExportacion` | Campo **Exportación** en Datos de Emisión | Datos de Emisión | `c_Exportacion` |
| `NombrePac` | Campo **PAC que timbró** en bloque fiscal | Encabezado → Datos fiscales | RFC del PAC (`RfcProvCertif`) |
| `NombrePac` | Campo **PAC que timbró** (Nómina) | Datos del Comprobante de Nómina | RFC del PAC (`RfcProvCertif`) |
| `NombreCveTransporte` | Campo **Vía de Entrada/Salida** | Complemento Carta Porte → Datos generales | `c_CveTransporte` |
| `NombrePermisoSCT` | Campo **Permiso SCT** | Complemento Carta Porte → Autotransporte Federal de Carga | `c_TipoPermiso` |
| `NombreConfigVehicular` | Campo **Configuración Vehicular** | Complemento Carta Porte → Autotransporte Federal de Carga | `c_ConfigAutotransporte` |
| `NombreSubTipoRemolque` | Campo **SubTipo Remolque** | Complemento Carta Porte → Remolques | `c_SubTipoRem` |
| `NombreTipoFigura` | Columna **Tipo Figura** de Figuras de Transporte | Complemento Carta Porte → Figuras de Transporte | `c_FiguraTransporte` |

---

## Formato de presentación en el PDF

Todos los campos siguen el patrón `clave - descripción`. Ejemplos:

- **Régimen Fiscal:** `624 - Coordinados`
- **Forma de Pago:** `99 - Por definir`
- **Método de Pago:** `PPD - Pago en parcialidades o diferido`
- **Exportación:** `01 - No aplica`
- **Uso del CFDI:** `G03 - Gastos en general`
- **PAC que timbró:** `Buzón E (SST060807KU0)` (nombre + RFC entre paréntesis)
- **Objeto Imp.:** `Sí objeto de impuesto` (solo descripción, sin clave)
- **Clave Unidad:** `Unidad de Servicio` (solo descripción, sin clave)
- **Impuesto:** `IVA` (nombre corto, sin clave)

---

## Cómo agregar un nuevo mapeo

Los helpers están centralizados en `CFDI.BuildPdf/PdfBuilders/Common/CfdiPdfSections.cs`. Para agregar una nueva entrada a un catálogo existente, simplemente agrega un caso al `switch`:

```csharp
public static string NombreFormaPago(string? clave)
{
    return clave switch
    {
        "01" => "Efectivo",
        // ... entradas existentes ...
        "32" => "Nueva forma de pago",  // ← agregar aquí
        _ => clave ?? ""
    };
}
```

Para el diccionario de PACs, agrega una entrada al `Dictionary`:

```csharp
private static readonly Dictionary<string, string> PacsConocidos = new(StringComparer.OrdinalIgnoreCase)
{
    { "SST060807KU0", "Buzón E" },
    { "SED1102088J7", "InvoiceOne" },
    { "SAT970701NN3", "SAT (pruebas)" },
    { "NUEVO_RFC_PAC", "Nombre del PAC" },  // ← agregar aquí
};
```

---

## Ubicación del código

| Archivo | Contenido |
|---|---|
| `CFDI.BuildPdf/PdfBuilders/Common/CfdiPdfSections.cs` | Todos los helpers `Nombre*` y el diccionario `PacsConocidos` |
| `CFDI.BuildPdf/PdfBuilders/CartaPorte/CartaPorteDocumentBuilder.cs` | Uso de los helpers en el PDF de Carta Porte |
| `CFDI.BuildPdf/PdfBuilders/Nomina/NominaDocumentBuilder.cs` | Uso de los helpers en el PDF de Nómina |
| `CFDI.BuildPdf/Models/CfdiViewModelBase.cs` | Propiedad `RfcProvCertif` del view model |
| `CFDI.BuildPdf/Mappers/Common/BaseCfdiMapper.cs` | Mapeo de `RfcProvCertif` desde el XML |
