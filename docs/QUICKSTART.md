# ⚡ Guía de Inicio Rápido

Pon en marcha **NoPilot** en menos de 5 minutos.

---

## Paso 1: Instalar Ollama

### Windows

```powershell
# Descarga el instalador desde https://ollama.ai/download
# O usa winget:
winget install Ollama.Ollama
```

### macOS

```bash
brew install ollama
```

### Linux

```bash
curl -fsSL https://ollama.ai/install.sh | sh
```

---

## Paso 2: Descargar Modelos

```bash
# Inicia el servidor Ollama (si no está corriendo)
ollama serve

# En otra terminal, descarga los modelos (esto puede tardar unos minutos)
ollama pull deepseek-coder:6.7b      # ~3.8 GB
ollama pull mxbai-embed-large        # ~669 MB

# Verifica que se descargaron correctamente
ollama list
```

**Salida esperada:**
```
NAME                        ID              SIZE      MODIFIED
deepseek-coder:6.7b         a18a4a4          3.8 GB    2 minutes ago
mxbai-embed-large:latest    468836162        669 MB    1 minute ago
```

---

## Paso 3: Clonar y Configurar NoPilot

```bash
# Clona el repositorio
git clone https://github.com/ArmandIsCoding/NoPilot.git
cd NoPilot/NoPilot

# Copia el archivo de configuración de ejemplo
cp appsettings.example.json appsettings.json

# Edita appsettings.json y cambia la ruta de SourceFolder
# Windows: "C:\\Ruta\\A\\Tu\\Proyecto"
# Linux/Mac: "/home/usuario/mi-proyecto"
```

### Ejemplo `appsettings.json` (Windows):

```json
{
  "Ollama": {
    "Endpoint": "http://localhost:11434",
    "ChatModel": "deepseek-coder:6.7b",
    "EmbeddingModel": "mxbai-embed-large",
    "EmbeddingDimension": 1024
  },
  "Ingestion": {
    "SourceFolder": "D:\\MiProyecto\\src",
    "SupportedExtensions": [".cs", ".js", ".ts", ".py", ".md"],
    "ChunkSize": 1500,
    "ChunkOverlap": 200,
    "MaxFileSizeBytes": 1048576
  },
  "VectorStore": {
    "DatabasePath": "nopilot.db"
  }
}
```

---

## Paso 4: Compilar y Ejecutar

```bash
# Restaura paquetes NuGet
dotnet restore

# Compila el proyecto
dotnet build

# Ejecuta NoPilot
dotnet run
```

**Salida esperada:**

```
╔══════════════════════════════════════════════════╗
║       NoPilot  ·  Asistente de Código Local      ║
╚══════════════════════════════════════════════════╝
  Chat model   : deepseek-coder:6.7b
  Embeddings   : mxbai-embed-large  (1024d)
  Ollama       : http://localhost:11434
  Carpeta      : D:\MiProyecto\src
  Base de datos: nopilot.db  (0 chunks indexados)

  INGESTAR  → Indexa todos los archivos de la carpeta configurada
  LIMPIAR   → Elimina el índice y el historial de chat
  AYUDA     → Muestra este mensaje
  SALIR     → Cierra la aplicación
  <texto>   → Pregunta sobre el código indexado

>>
```

---

## Paso 5: Indexar tu Código

```
>> INGESTAR
[INGESTAR] Limpiando datos anteriores...
[INGESTAR] 143 archivos encontrados para indexar.
[INGESTAR] 143/143 archivos | 1247 chunks | 0 omitidos
[INGESTAR] Completado: 143 archivos, 1247 chunks indexados, 0 omitidos.
```

⏱️ **Tiempo estimado:**
- 10 archivos → ~30 segundos
- 100 archivos → ~5 minutos
- 500 archivos → ~20 minutos

---

## Paso 6: ¡Chatea con tu Código!

```
>> ¿Qué hace la clase VectorStoreService?
[Buscando contexto relevante...]
[NoPilot]: VectorStoreService es responsable de gestionar el almacenamiento 
y búsqueda de embeddings usando SQLite con la extensión vec0...

>> ¿Cómo puedo añadir soporte para archivos .jsx?
[NoPilot]: Para añadir soporte para archivos .jsx, actualiza el array 
SupportedExtensions en appsettings.json:

```json
"SupportedExtensions": [".cs", ".js", ".jsx", ".ts", ".tsx", ...]
```

Luego ejecuta LIMPIAR e INGESTAR de nuevo para reindexar con los nuevos tipos.
```

---

## 🚨 Troubleshooting Rápido

### "Connection refused" al conectar con Ollama

```bash
# Verifica que Ollama está corriendo
curl http://localhost:11434/api/tags

# Si no responde, inicia el servidor:
ollama serve
```

### "No se encontraron archivos para indexar"

- Revisa que `SourceFolder` en `appsettings.json` sea una ruta válida
- Verifica que haya archivos con las extensiones en `SupportedExtensions`
- Windows: usa rutas con doble backslash `C:\\Ruta\\Al\\Proyecto`

### Ingesta muy lenta

- Reduce `ChunkSize` a 1000 (menos tokens por embedding)
- Limita `SupportedExtensions` a solo las extensiones importantes
- Añade más exclusiones en `MaxFileSizeBytes` (ej: 512KB en lugar de 1MB)

### El modelo responde en inglés aunque pregunto en español

- Reformula la pregunta con más contexto en español
- El model `deepseek-coder` es multilingüe pero puede preferir inglés en contextos técnicos
- Alternativa: usa `deepseek-coder:33b` o `codellama:13b` para mejor multilingüismo

---

## 🎉 ¡Listo!

Ya tienes NoPilot corriendo. Ahora prueba:

```
>> Explica la arquitectura de este proyecto

>> ¿Dónde se maneja la configuración de la base de datos?

>> ¿Qué patrones de diseño se usan?

>> Lista todos los servicios registrados en DI
```

---

## 📚 Siguientes Pasos

- Lee la [Arquitectura Técnica](ARCHITECTURE.md) para entender los detalles internos
- Revisa [EXAMPLES.md](EXAMPLES.md) para más casos de uso
- Contribuye en [GitHub](https://github.com/ArmandIsCoding/NoPilot) — ver [CONTRIBUTING.md](../CONTRIBUTING.md)

---

<div align="center">

**[🏠 Volver al inicio](../README.md)**

</div>
