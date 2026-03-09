# 🤝 Guía de Contribución

¡Gracias por tu interés en contribuir a **NoPilot**! Este documento te guiará en el proceso.

---

## 🐛 Reportar Bugs

Si encuentras un bug, por favor abre un [issue](https://github.com/ArmandIsCoding/NoPilot/issues) con:

- **Título descriptivo**: "Error al ingestar archivos .ts"
- **Pasos para reproducir**: Secuencia exacta de comandos
- **Comportamiento esperado vs actual**
- **Logs o mensajes de error**: Copia el stack trace completo
- **Entorno**:
  - Versión de .NET: `dotnet --version`
  - Versión de Ollama: `ollama --version`
  - Sistema operativo: Windows 11, Ubuntu 22.04, macOS 14, etc.

---

## ✨ Proponer Features

Para nuevas características:

1. Abre un **issue** con el tag `enhancement`
2. Describe **el problema** que resuelve la feature
3. Propón **la solución** con ejemplos de uso
4. Opcional: boceto de implementación técnica

---

## 💻 Contribuir Código

### Setup inicial

```bash
# 1. Haz fork del repo en GitHub

# 2. Clona tu fork
git clone https://github.com/TU_USUARIO/NoPilot.git
cd NoPilot

# 3. Añade el repo original como upstream
git remote add upstream https://github.com/ArmandIsCoding/NoPilot.git

# 4. Restaura dependencias
cd NoPilot
dotnet restore
```

### Flujo de trabajo

```bash
# 1. Actualiza tu fork
git checkout main
git pull upstream main

# 2. Crea una rama para tu feature
git checkout -b feature/mi-feature

# 3. Haz tus cambios
# Edita archivos, añade tests, actualiza docs...

# 4. Verifica que compile sin errores
dotnet build

# 5. Commitea con mensajes descriptivos
git add .
git commit -m "feat: añadir soporte para archivos .jsx y .tsx"

# 6. Push a tu fork
git push origin feature/mi-feature

# 7. Abre un Pull Request desde GitHub
```

### Estilo de commits

Seguimos [Conventional Commits](https://www.conventionalcommits.org/):

- `feat:` Nueva funcionalidad
- `fix:` Corrección de bug
- `docs:` Cambios en documentación
- `refactor:` Refactorización sin cambios de comportamiento
- `perf:` Mejoras de rendimiento
- `test:` Añadir o modificar tests
- `chore:` Cambios en build, CI, dependencias, etc.

Ejemplos:
```
feat: añadir soporte para ingesta incremental
fix: corregir encoding UTF-8 en archivos con BOM
docs: actualizar guía de instalación para Linux
refactor: extraer lógica de chunking a clase separada
perf: optimizar búsqueda vectorial con índice HNSW
```

---

## 📐 Estándares de Código

### C# Style Guide

- **Usar C# 14** y características modernas de .NET 10
- **Nullable reference types** habilitados (`<Nullable>enable</Nullable>`)
- **File-scoped namespaces** cuando sea posible
- **Primary constructors** para clases simples
- **Collection expressions** `[]` en lugar de `new List<T>()`
- **String interpolation** en lugar de concatenación
- **Async/await** para todas las operaciones I/O
- **Naming conventions**:
  - `PascalCase` para tipos, métodos, propiedades
  - `camelCase` para parámetros, variables locales
  - `_camelCase` para campos privados
  - `SCREAMING_CASE` para constantes

### Estructura de archivos

```csharp
// 1. Usings (ordenados alfabéticamente)
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using NoPilot.Configuration;

// 2. Namespace (file-scoped)
namespace NoPilot.Services;

// 3. XML doc para tipos públicos
/// <summary>
/// Servicio que gestiona la ingesta de archivos...
/// </summary>
public sealed class IngestionService : IIngestionService
{
    // 4. Campos privados
    private readonly Kernel _kernel;

    // 5. Constructor
    public IngestionService(Kernel kernel) { ... }

    // 6. Métodos públicos
    public async Task IngestAsync() { ... }

    // 7. Métodos privados
    private void Helper() { ... }
}
```

---

## 🧪 Tests (WIP)

Actualmente el proyecto no tiene tests, pero sería una **gran contribución** añadirlos:

```bash
# Estructura sugerida
NoPilot.Tests/
├── Services/
│   ├── VectorStoreServiceTests.cs
│   ├── IngestionServiceTests.cs
│   └── ChatServiceTests.cs
├── Plugins/
│   └── CodebasePluginTests.cs
└── Integration/
    └── EndToEndTests.cs
```

Usa **xUnit** + **FluentAssertions** + **NSubstitute** para mocks.

---

## 📖 Documentación

Si añades una nueva feature:

1. **Actualiza `README.md`** con ejemplos de uso
2. **Añade XML doc comments** en el código público
3. **Actualiza `CONTRIBUTING.md`** si cambias el flujo de desarrollo

---

## ❓ Preguntas

¿Dudas? Abre un [issue](https://github.com/ArmandIsCoding/NoPilot/issues) con el tag `question` o contáctame vía GitHub.

---

<div align="center">

**¡Gracias por contribuir a NoPilot! 🚀**

</div>
