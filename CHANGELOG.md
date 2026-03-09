# 📝 Changelog

Todos los cambios notables de este proyecto se documentarán en este archivo.

El formato se basa en [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/),
y este proyecto adhiere a [Semantic Versioning](https://semver.org/lang/es/).

---

## [Unreleased]

### Planeado

- Ingesta incremental (detectar archivos modificados)
- Progress bar durante ingesta
- Interfaz web con Blazor Server
- Docker compose con Ollama preconfigurado

---

## [0.1.0] - 2025-01-XX

### 🎉 Release Inicial

Primera versión funcional de NoPilot con todas las características core.

### ✨ Características Añadidas

- **Pipeline RAG completo** con Semantic Kernel + Ollama
- **Indexación de codebase** con chunking inteligente y overlap
- **Vector store** usando SQLite + sqlite-vec (extensión vec0)
- **Chat con streaming** y mantenimiento de historial (5 rondas)
- **Búsqueda semántica KNN** con embeddings de mxbai-embed-large
- **Configuración centralizada** en appsettings.json
- **Comandos de consola**: INGESTAR, LIMPIAR, AYUDA, SALIR
- **Cross-platform**: Windows, Linux, macOS (x64 y ARM64)
- **Resolución automática** de ruta nativa vec0 según RID
- **Plugin de Semantic Kernel** (CodebasePlugin) para function calling

### 🛠️ Stack Tecnológico

- .NET 10.0 (C# 14)
- Semantic Kernel 1.73.0
- Microsoft.SemanticKernel.Connectors.Ollama 1.73.0-alpha
- Microsoft.Data.Sqlite 9.0.0
- sqlite-vec 0.1.7-alpha.2
- Modelos: deepseek-coder:6.7b + mxbai-embed-large

### 📖 Documentación

- README completo con badges, ejemplos y guías
- ARCHITECTURE.md con diagramas técnicos
- QUICKSTART.md con instalación paso a paso
- EXAMPLES.md con casos de uso reales
- CONTRIBUTING.md con guía de contribución
- LICENSE (MIT)

### 🏗️ Arquitectura

```
NoPilot/
├── Configuration/AppSettings.cs
├── Models/DocumentChunk.cs, SearchResult.cs
├── Services/VectorStoreService.cs, IngestionService.cs, ChatService.cs
├── Plugins/CodebasePlugin.cs
└── Program.cs
```

---

## Formato de Versiones

- **MAJOR**: Cambios incompatibles en la API
- **MINOR**: Nuevas funcionalidades compatibles hacia atrás
- **PATCH**: Correcciones de bugs compatibles hacia atrás

---

[Unreleased]: https://github.com/ArmandIsCoding/NoPilot/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/ArmandIsCoding/NoPilot/releases/tag/v0.1.0
