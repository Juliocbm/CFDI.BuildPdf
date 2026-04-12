# 🖼️ Logos de Compañías

Esta carpeta contiene los logos que se incluirán en los PDFs de CFDI generados.

## 📁 Archivos Requeridos

Coloca los siguientes archivos de logo en formato PNG:

| Base de Datos | Archivo | Descripción |
|--------------|---------|-------------|
| `hgdb_lis` | `hg.png` | Logo de HG Transportaciones |
| `chdb_lis` | `ch.png` | Logo de CH |
| `rldb_lis` | `rl.png` | Logo de RL |
| `lindadb` | `ld.png` | Logo de Linda |

## ⚙️ Cómo Funciona

1. **File System** (Prioridad): El sistema busca primero en esta carpeta
   - Ruta: `{AppDirectory}/Assets/Logos/{archivo}.png`
   - **Ventaja**: Fácil de actualizar sin recompilar

2. **Recursos Embebidos** (Fallback): Si no encuentra en file system, busca recursos embebidos
   - **Ventaja**: Portable, incluido en el ejecutable

3. **Sin Logo**: Si no encuentra el logo, genera PDF sin logo
   - No causa error, solo warning en logs

## 📋 Especificaciones Técnicas

- **Formato**: PNG (recomendado) o JPG
- **Tamaño recomendado**: 200x80px o similar (aspect ratio ~2.5:1)
- **Peso máximo**: 100KB por archivo
- **Transparencia**: Soportada (PNG)

## 🚀 Deployment

### Desarrollo
Copia los archivos a esta carpeta y reinicia la aplicación.

### Producción
Asegúrate de que la carpeta `Assets/Logos` y sus archivos se copien al directorio de publicación:
```bash
{PublishDir}/Assets/Logos/*.png
```

O configura como recursos embebidos en `HgCfdi.Infrastructure.csproj`:
```xml
<ItemGroup>
  <EmbeddedResource Include="Assets\Logos\*.png" />
</ItemGroup>
```

## 🔍 Logs

Verifica en los logs si los logos se cargan correctamente:
- ✅ `📁 Cargando logo desde file system: {FilePath}`
- ✅ `📦 Cargando logo desde recurso embebido: {ResourceName}`
- ⚠️ `⚠️ Logo no encontrado | Database: {Database} | Archivo: {File}...`

## 📝 Notas

- Los logos se cachean en memoria por 24 horas
- El mapeo database → archivo está en `LogoService.cs`
- Para agregar nuevas compañías, actualiza el diccionario `_mapaBasesDatos`
