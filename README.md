# 🚀 NoPilot

<div align="center">

**Asistente de código local potenciado por IA que indexa tu codebase y responde preguntas usando Retrieval-Augmented Generation (RAG)**

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Semantic Kernel](https://img.shields.io/badge/Semantic_Kernel-1.73.0-00A4EF?logo=microsoft)](https://github.com/microsoft/semantic-kernel)
[![Ollama](https://img.shields.io/badge/Ollama-Local_AI-000000?logo=llama)](https://ollama.ai/)
[![sqlite-vec](https://img.shields.io/badge/sqlite--vec-0.1.7-003B57?logo=sqlite)](https://github.com/asg017/sqlite-vec)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

[🚀 Quick Start](docs/QUICKSTART.md) • [📖 Architecture](docs/ARCHITECTURE.md) • [💬 Examples](docs/EXAMPLES.md) • [🤝 Contributing](CONTRIBUTING.md)

</div>

---

## 📋 Tabla de Contenidos

- [¿Qué es NoPilot?](#que-es-nopilot)
- [Demo](#demo)
- [Características](#caracteristicas)
- [Requisitos](#requisitos)
- [Instalación Rápida](#instalacion-rapida)
- [Configuración](#configuracion)
- [Uso](#uso)
- [Arquitectura](#arquitectura)
- [Cómo Funciona](#como-funciona)
- [Casos de Uso](#casos-de-uso)
- [Troubleshooting](#troubleshooting)
- [Roadmap](#roadmap)
- [Contribuir](#contribuir)
- [Licencia](#licencia)

### 📂 Documentación Completa

- [⚡ Quick Start — Guía de Inicio Rápido](docs/QUICKSTART.md)
- [🏗️ Arquitectura Técnica Detallada](docs/ARCHITECTURE.md)
- [💬 Ejemplos de Conversaciones](docs/EXAMPLES.md)
- [📚 Índice de Documentación](docs/README.md)

---

<a name="que-es-nopilot"></a>

## 🤔 ¿Qué es NoPilot?

**NoPilot** es una aplicación de consola .NET que convierte tu codebase local en una base de conocimiento semántica. Utiliza **Semantic Kernel**, **Ollama** (ejecución local de LLMs), y **sqlite-vec** para ofrecer un chatbot experto en tu código sin enviar datos a la nube.

### El problema que resuelve

- ❌ No quieres compartir tu código con servicios cloud
- ❌ GitHub Copilot no conoce tu codebase privada completa
- ❌ Buscar código con `grep` no entiende semántica ni contexto
- ✅ **NoPilot indexa tu proyecto y responde preguntas con contexto completo**

---

<a name="demo"></a>

## 🎬 Demo

```plaintext
╔══════════════════════════════════════════════════╗
║       NoPilot  ·  Asistente de Código Local      ║
╚══════════════════════════════════════════════════╝
  Chat model   : deepseek-coder:6.7b
  Embeddings   : mxbai-embed-large  (1024d)
  Ollama       : http://localhost:11434
  Carpeta      : C:\MyProject
  Base de datos: nopilot.db  (2891 chunks indexados)

>> ¿Cómo está estructurado el patrón de inyección de dependencias?
[Buscando contexto relevante...]
[NoPilot]: El proyecto utiliza Microsoft.Extensions.DependencyInjection 
con un patrón estándar. En Program.cs se registran:

1. AppSettings como singleton desde appsettings.json
2. VectorStoreService para gestión de SQLite + vec0
3. IngestionService para procesamiento de archivos
4. ChatService para orquestación RAG
5. Semantic Kernel configurado con Ollama para chat y embeddings...

>> ¿Qué hace VectorStoreService?
[NoPilot]: VectorStoreService encapsula toda la interacción con SQLite
y sqlite-vec. Sus responsabilidades principales son:
- Inicializar el esquema de base de datos (chunks + vec_chunks)
- Cargar la extensión nativa vec0 dinámicamente según la plataforma...
```

**[📺 Ver más ejemplos de uso →](docs/EXAMPLES.md)**

---

<a name="caracteristicas"></a>

## ✨ Características

| Característica | Descripción |
|---|---|
| 🏠 **100% Local** | Todo corre en tu máquina. Sin APIs externas, sin telemetría. |
| 🧠 **RAG Semántico** | Búsqueda vectorial con embeddings para encontrar código relevante. |
| 💬 **Chat con Historial** | Mantiene contexto de conversación (últimas 5 rondas). |
| ⚡ **Streaming** | Respuestas en tiempo real token por token. |
| 🔌 **Configurable** | Modelos, carpetas, extensiones de archivo — todo en `appsettings.json`. |
| 📦 **SQLite + sqlite-vec** | Base de datos embebida con soporte nativo de vectores (KNN search). |
| 🎯 **Chunking Inteligente** | División de archivos con overlap para preservar contexto. |
| 🛠️ **Semantic Kernel** | Orquestación con plugins y function calling. |
| 🌍 **Cross-Platform** | Windows, Linux, macOS (x64 y ARM64). |

---

<a name="requisitos"></a>

## 📦 Requisitos

### Software

- **.NET 10 SDK** ([Descargar aquí](https://dotnet.microsoft.com/download/dotnet/10.0))
- **Ollama** instalado y corriendo localmente ([Guía de instalación](https://ollama.ai/download))

### Modelos de Ollama

Ejecuta estos comandos para descargar los modelos necesarios:

```bash
# Modelo de chat (6.7B parámetros, especializado en código)
ollama pull deepseek-coder:6.7b

# Modelo de embeddings (1024 dimensiones, optimizado para RAG)
ollama pull mxbai-embed-large
```

**[🚀 Guía completa de instalación paso a paso →](docs/QUICKSTART.md)**

---

<a name="instalacion-rapida"></a>

## 🚀 Instalación Rápida

```bash
# 1. Clona el repositorio
git clone https://github.com/ArmandIsCoding/NoPilot.git
cd NoPilot/NoPilot

# 2. Configura la carpeta de origen
cp appsettings.example.json appsettings.json
# Edita appsettings.json y cambia "SourceFolder" a tu proyecto

# 3. Compila y ejecuta
dotnet restore
dotnet build
dotnet run

# 4. Indexa tu código
>> INGESTAR

# 5. ¡Empieza a preguntar!
>> ¿Cómo funciona el sistema de autenticación?
```

**[📖 Instalación detallada con troubleshooting →](docs/QUICKSTART.md)**

---

<a name="configuracion"></a>

## ⚙️ Configuración

Toda la configuración está centralizada en `NoPilot/appsettings.json`:

```json
{
  "Ollama": {
    "Endpoint": "http://localhost:11434",
    "ChatModel": "deepseek-coder:6.7b",
    "EmbeddingModel": "mxbai-embed-large",
    "EmbeddingDimension": 1024
  },
  "Ingestion": {
    "SourceFolder": "C:\\MisProyectos",
    "SupportedExtensions": [".cs", ".ts", ".js", ".py", ".go", ".md"],
    "ChunkSize": 1500,
    "ChunkOverlap": 200,
    "MaxFileSizeBytes": 1048576
  },
  "VectorStore": {
    "DatabasePath": "nopilot.db"
  }
}
```

### 🔄 Cambiar modelos

Si cambias `EmbeddingModel` o `EmbeddingDimension`, **elimina `nopilot.db`** y ejecuta `INGESTAR` de nuevo (el esquema de la tabla vectorial depende de la dimensión).

**Modelos alternativos:**

| Modelo | Dimensión | Velocidad | Calidad | Uso recomendado |
|--------|-----------|-----------|---------|-----------------|
| `mxbai-embed-large` | 1024 | Media | Alta | **Recomendado** (equilibrado) |
| `nomic-embed-text` | 768 | Alta | Media | Proyectos grandes (>50K archivos) |
| `all-minilm` | 384 | Muy alta | Baja | Pruebas rápidas |

---

<a name="uso"></a>

## 💻 Uso

### Comandos disponibles

| Comando | Descripción | Ejemplo |
|---------|-------------|---------|
| `INGESTAR` | Indexa todos los archivos de `SourceFolder` | Primera vez o después de cambios grandes |
| `LIMPIAR` | Elimina el índice completo y el historial | Antes de cambiar modelos |
| `AYUDA` | Muestra la lista de comandos | - |
| `SALIR` | Cierra la aplicación | Ctrl+C también funciona |
| *cualquier texto* | Chatea con el asistente sobre tu código | "¿Qué hace la clase UserService?" |

### Flujo típico de trabajo

```bash
# Terminal 1: Ollama Server
ollama serve

# Terminal 2: NoPilot
cd NoPilot
dotnet run

# Primera vez: indexar
>> INGESTAR
[INGESTAR] 347 archivos | 2891 chunks indexados

# Chatear
>> Explica la arquitectura general del proyecto
>> ¿Dónde se define la conexión a la base de datos?
>> Lista todos los servicios registrados en DI
>> ¿Qué patrones de diseño se usan en el módulo de autenticación?

# Si modificas muchos archivos: reindexa
>> LIMPIAR
>> INGESTAR
```

**[💬 Ver ejemplos completos de conversaciones →](docs/EXAMPLES.md)**

---

<a name="arquitectura"></a>

## 🏗️ Arquitectura

```
NoPilot/
├── 📄 appsettings.json          # Configuración central
├── 📁 Configuration/
│   └── AppSettings.cs           # POCOs fuertemente tipados
├── 📁 Models/
│   ├── DocumentChunk.cs         # Fragmento de código + embedding
│   └── SearchResult.cs          # Resultado de búsqueda semántica
├── 📁 Services/
│   ├── VectorStoreService.cs    # SQLite + sqlite-vec (búsqueda KNN)
│   ├── IngestionService.cs      # Lectura, chunking, generación de embeddings
│   └── ChatService.cs           # Pipeline RAG con historial
├── 📁 Plugins/
│   └── CodebasePlugin.cs        # SK Plugin para function calling
└── 📄 Program.cs                # DI, inicialización, bucle de consola
```

### Stack Tecnológico

| Componente | Tecnología | Versión |
|------------|------------|---------|
| **Framework** | .NET | 10.0 |
| **AI Orchestration** | [Semantic Kernel](https://github.com/microsoft/semantic-kernel) | 1.73.0 |
| **LLM Local** | [Ollama](https://ollama.ai/) + [deepseek-coder](https://ollama.ai/library/deepseek-coder) | 6.7b |
| **Embeddings** | [mxbai-embed-large](https://ollama.ai/library/mxbai-embed-large) | 1024d |
| **Vector Store** | [SQLite](https://www.sqlite.org/) + [sqlite-vec](https://github.com/asg017/sqlite-vec) | 0.1.7-alpha.2 |
| **Dependency Injection** | Microsoft.Extensions.DependencyInjection | 10.0.2 |

**[🏗️ Arquitectura técnica completa con diagramas →](docs/ARCHITECTURE.md)**

---

<a name="como-funciona"></a>

## 🔍 Cómo Funciona

### Pipeline de Ingesta

```
📁 Archivos en disco
    ↓ Lectura + filtrado por extensión (.cs, .ts, .py...)
📝 Chunking con overlap
    ↓ División inteligente (1500 chars, overlap 200)
🧠 Generación de embeddings
    ↓ mxbai-embed-large → 1024 dimensiones
💾 SQLite + sqlite-vec
    ↓ Almacenamiento con índice KNN optimizado
✅ Listo para búsqueda
```

### Pipeline de Chat RAG

```
💬 Pregunta del usuario
    ↓ "¿Cómo funciona la autenticación?"
🔢 Embedding de la pregunta
    ↓ mxbai-embed-large → float[1024]
🔍 Búsqueda vectorial KNN
    ↓ sqlite-vec: Top 5 chunks más similares
📋 Construcción del contexto
    ↓ System prompt + Historial + Chunks relevantes
🤖 Generación con LLM
    ↓ deepseek-coder:6.7b
⚡ Streaming de respuesta
    ↓ Token por token
📺 Respuesta con contexto
```

### Tecnología Vector Store

`sqlite-vec` extiende SQLite con capacidades de búsqueda vectorial eficiente:

```sql
-- Tabla de metadatos
CREATE TABLE chunks (
    id INTEGER PRIMARY KEY,
    file_path TEXT,
    content TEXT,
    chunk_index INTEGER,
    created_at TEXT
);

-- Tabla virtual con índice vectorial
CREATE VIRTUAL TABLE vec_chunks USING vec0(
    embedding float[1024]
);

-- Búsqueda KNN (K-Nearest Neighbors)
SELECT c.id, c.file_path, c.content, v.distance
FROM vec_chunks v
JOIN chunks c ON c.id = v.rowid
WHERE v.embedding MATCH @query_vector
  AND k = 5
ORDER BY v.distance ASC;
```

**Performance:** ~5-10ms para búsqueda en 10K vectores, ~15-30ms en 100K vectores.

**[🔬 Pipeline técnico detallado →](docs/ARCHITECTURE.md)**

---

<a name="casos-de-uso"></a>

## 🎯 Casos de Uso

### 👨‍💻 Para Desarrolladores

- **Onboarding**: "¿Cómo funciona el sistema de autenticación?"
- **Debugging**: "¿Dónde se maneja el error 'NullReferenceException' en el servicio de usuarios?"
- **Refactoring**: "¿Qué archivos usan la interfaz `IRepository`?"
- **Arquitectura**: "Explica el patrón de inyección de dependencias usado en este proyecto"
- **API Discovery**: "Lista todos los endpoints HTTP del proyecto"

### 🔍 Para Code Review

- "¿Hay validaciones de entrada en los endpoints de la API?"
- "¿Se usa async/await correctamente en las llamadas a base de datos?"
- "Lista todos los lugares donde se accede directamente a `HttpContext`"
- "¿Hay código duplicado en los controladores?"

### 📚 Para Documentación

- "Genera una descripción de los servicios principales del proyecto"
- "¿Qué patrones de diseño se utilizan?"
- "Documenta el flujo de autenticación paso a paso"

### 🧪 Para Testing

- "¿Qué clases no tienen tests unitarios?"
- "¿Cómo se mockea la base de datos en los tests?"

**[💡 Ver ejemplos completos de conversaciones →](docs/EXAMPLES.md)**

---

<a name="troubleshooting"></a>

## 🔧 Troubleshooting

### ❌ Error: `No se puede encontrar el módulo especificado` (sqlite-vec)

**Causa:** La extensión nativa `vec0.dll` no se cargó correctamente.

**Solución:**
```bash
dotnet clean
dotnet build
```

Verifica que `vec0.dll` existe en:
- Windows: `bin/Debug/net10.0/runtimes/win-x64/native/vec0.dll`
- Linux: `bin/Debug/net10.0/runtimes/linux-x64/native/vec0.so`
- macOS: `bin/Debug/net10.0/runtimes/osx-arm64/native/vec0.dylib`

### ❌ Error: Connection refused (Ollama)

**Causa:** El servicio de Ollama no está corriendo.

**Solución:**
```bash
# Terminal 1: Inicia Ollama
ollama serve

# Terminal 2: Verifica conectividad
curl http://localhost:11434/api/tags

# Verifica que los modelos estén descargados
ollama list
```

### ❌ Embeddings con dimensiones incorrectas

**Causa:** Cambiaste el modelo de embeddings pero no actualizaste `EmbeddingDimension`.

**Solución:**
1. Consulta la dimensión del modelo:
   ```bash
   ollama show mxbai-embed-large | grep "embedding"
   ```
2. Actualiza `Ollama.EmbeddingDimension` en `appsettings.json`
3. Elimina la base de datos y reindexa:
   ```bash
   rm nopilot.db
   dotnet run
   >> INGESTAR
   ```

### ⚠️ Ingesta muy lenta

**Causas posibles:**
- Carpeta muy grande (>1000 archivos)
- Ollama corriendo solo en CPU
- Modelo de embeddings pesado

**Optimizaciones:**
1. Reduce `ChunkSize` a 1000 en `appsettings.json`
2. Limita `SupportedExtensions` a solo las extensiones críticas
3. Reduce `MaxFileSizeBytes` a 512KB
4. Usa un modelo de embeddings más rápido: `nomic-embed-text` (768d)
5. Si tienes GPU: configura Ollama para usar CUDA

**[🔧 Troubleshooting completo →](docs/QUICKSTART.md#-troubleshooting-rápido)**

---

<a name="roadmap"></a>

## 🧪 Roadmap

### v0.2 (Próximamente)

- [ ] **Ingesta incremental**: Detectar archivos modificados y reindexar solo esos
- [ ] **Progress bar**: Visualización del progreso de ingesta
- [ ] **Config watcher**: Recargar `appsettings.json` sin reiniciar

### v0.3

- [ ] **Interfaz web**: Blazor Server o REST API
- [ ] **Múltiples carpetas**: Indexar varios proyectos en la misma base
- [ ] **Export/Import**: Backup y restauración de índices

### v1.0

- [ ] **Git integration**: Indexar solo commits específicos o ramas
- [ ] **Tests unitarios**: Cobertura >80%
- [ ] **Docker**: Imagen con Ollama preconfigurado
- [ ] **Azure OpenAI**: Soporte como alternativa a Ollama

### Ideas futuras

- [ ] Plugin de VSCode
- [ ] Métricas de relevancia (feedback loop)
- [ ] Soporte para documentación externa (URLs, PDFs)
- [ ] Multi-language RAG (cambiar idioma del asistente)

**[🗳️ Vota por features](https://github.com/ArmandIsCoding/NoPilot/discussions) o propón nuevas ideas!**

---

<a name="contribuir"></a>

## 🤝 Contribuir

¡Las contribuciones son bienvenidas! Aquí hay varias formas de ayudar:

### 🐛 Reportar Bugs

Abre un [issue](https://github.com/ArmandIsCoding/NoPilot/issues) con:
- Descripción del problema
- Pasos para reproducir
- Logs de error
- Versiones de software (.NET, Ollama, OS)

### ✨ Proponer Features

Abre un [discussion](https://github.com/ArmandIsCoding/NoPilot/discussions) o [issue](https://github.com/ArmandIsCoding/NoPilot/issues) con el tag `enhancement`.

### 💻 Contribuir Código

1. 🍴 Fork el proyecto
2. 🌿 Crea tu rama (`git checkout -b feature/AmazingFeature`)
3. 💾 Commitea tus cambios (`git commit -m "feat: add amazing feature"`)
4. 📤 Push a la rama (`git push origin feature/AmazingFeature`)
5. 🎯 Abre un Pull Request

### Áreas prioritarias

- 🧪 **Tests**: Añadir cobertura de tests unitarios e integración
- ⚡ **Performance**: Optimizar ingesta (paralelización, batch embeddings)
- 🎨 **UI**: Crear interfaz web con Blazor o React
- 📝 **Docs**: Mejorar documentación con más ejemplos
- 🌍 **i18n**: Traducir mensajes de consola

**[📖 Guía completa de contribución →](CONTRIBUTING.md)**

---

<a name="licencia"></a>

## 📜 Licencia

Este proyecto está bajo la **licencia MIT**. Puedes usarlo, modificarlo y distribuirlo libremente. Ver [LICENSE](LICENSE) para más detalles.

---

## 🙏 Agradecimientos

Este proyecto se construye sobre los hombros de gigantes:

- [**Semantic Kernel**](https://github.com/microsoft/semantic-kernel) - Framework de orquestación de IA de Microsoft
- [**Ollama**](https://ollama.ai/) - Ejecución local de LLMs sin complicaciones
- [**sqlite-vec**](https://github.com/asg017/sqlite-vec) - Extensión de vectores para SQLite por Alex Garcia
- [**DeepSeek Coder**](https://github.com/deepseek-ai/DeepSeek-Coder) - Modelo especializado en código
- [**mxbai-embed-large**](https://huggingface.co/mixedbread-ai/mxbai-embed-large-v1) - Modelo de embeddings de mixedbread.ai

Mención especial a la comunidad open source de .NET y IA.

---

## 📬 Contacto y Comunidad

- **GitHub**: [@ArmandIsCoding](https://github.com/ArmandIsCoding)
- **Discussions**: [Foro de discusión](https://github.com/ArmandIsCoding/NoPilot/discussions)
- **Issues**: [Reportar bugs](https://github.com/ArmandIsCoding/NoPilot/issues)

---

## ⭐ Muestra tu apoyo

Si **NoPilot** te resulta útil:

1. ⭐ Dale una estrella al repositorio
2. 🐦 Compártelo en redes sociales
3. 📝 Escribe un artículo sobre tu experiencia
4. 🤝 Contribuye con código o documentación

---

## 📊 Stats del Proyecto

![GitHub stars](https://img.shields.io/github/stars/ArmandIsCoding/NoPilot?style=social)
![GitHub forks](https://img.shields.io/github/forks/ArmandIsCoding/NoPilot?style=social)
![GitHub issues](https://img.shields.io/github/issues/ArmandIsCoding/NoPilot)
![GitHub pull requests](https://img.shields.io/github/issues-pr/ArmandIsCoding/NoPilot)

---

<div align="center">

**[⬆ Volver arriba](#-nopilot)**

---

Made with ❤️ and .NET 10 | Powered by Semantic Kernel & Ollama

</div>
