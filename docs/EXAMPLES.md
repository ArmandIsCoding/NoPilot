# 📸 Screenshots & Ejemplos

## Inicio de la aplicación

```
╔══════════════════════════════════════════════════╗
║       NoPilot  ·  Asistente de Código Local      ║
╚══════════════════════════════════════════════════╝
  Chat model   : deepseek-coder:6.7b
  Embeddings   : mxbai-embed-large  (1024d)
  Ollama       : http://localhost:11434
  Carpeta      : D:\Developer\MiProyecto
  Base de datos: nopilot.db  (0 chunks indexados)

  INGESTAR  → Indexa todos los archivos de la carpeta configurada
  LIMPIAR   → Elimina el índice y el historial de chat
  AYUDA     → Muestra este mensaje
  SALIR     → Cierra la aplicación
  <texto>   → Pregunta sobre el código indexado
```

## Proceso de ingesta

```
>> INGESTAR
[INGESTAR] Limpiando datos anteriores...
[INGESTAR] 347 archivos encontrados para indexar.
[INGESTAR] 347/347 archivos | 2891 chunks | 0 omitidos     
[INGESTAR] Completado: 347 archivos, 2891 chunks indexados, 0 omitidos.
```

## Ejemplo de chat con contexto

```
>> ¿Cómo está estructurada la inyección de dependencias?
[Buscando contexto relevante...]
[NoPilot]: El proyecto utiliza Microsoft.Extensions.DependencyInjection con un patrón 
estándar de registro de servicios. En `Program.cs` se configuran los siguientes servicios:

1. **AppSettings**: Singleton que encapsula la configuración desde appsettings.json
2. **VectorStoreService**: Singleton que gestiona la conexión a SQLite con sqlite-vec
3. **IngestionService**: Servicio para indexación de archivos
4. **ChatService**: Orquestador del pipeline RAG
5. **Kernel de Semantic Kernel**: Configurado con Ollama para chat y embeddings

El flujo es:
- ServiceCollection → registrar servicios
- BuildServiceProvider → resolver dependencias
- GetRequiredService<T> → inyección en construcción

¿Necesitas ver algún archivo específico del sistema de DI?

>> Sí, muéstrame el código de registro de servicios
[NoPilot]: Aquí está el código de `Program.cs` donde se registran todos los servicios:

```csharp
var services = new ServiceCollection();

services.AddSingleton(settings);
services.AddSingleton<IVectorStoreService, VectorStoreService>();
services.AddSingleton<IIngestionService, IngestionService>();
services.AddSingleton<ChatService>();
services.AddSingleton<CodebasePlugin>();

services.AddSingleton(sp =>
{
    var cfg = sp.GetRequiredService<AppSettings>();
    var builder = Kernel.CreateBuilder();
    builder.AddOllamaChatCompletion(cfg.Ollama.ChatModel, new Uri(cfg.Ollama.Endpoint));
    builder.AddOllamaEmbeddingGenerator(cfg.Ollama.EmbeddingModel, new Uri(cfg.Ollama.Endpoint));
    var kernel = builder.Build();
    kernel.Plugins.AddFromObject(sp.GetRequiredService<CodebasePlugin>());
    return kernel;
});
```

Este patrón permite:
- Configuración centralizada desde AppSettings
- Lifetime management automático (todos son singletons aquí)
- Semantic Kernel integrado al contenedor de DI
```

## Ejemplo de búsqueda técnica

```
>> ¿Dónde se serializa el embedding antes de guardarlo en SQLite?
[NoPilot]: El embedding se serializa a JSON en el método `SerializeEmbedding` de 
`VectorStoreService.cs`:

```csharp
private static string SerializeEmbedding(float[] embedding) =>
    JsonSerializer.Serialize(embedding);
```

Este método se usa en `UpsertChunkAsync` para convertir el array de floats en un string 
JSON que sqlite-vec almacena como BLOB. La biblioteca vec0 acepta tanto arrays JSON como 
formato binario, pero JSON es más portable para debugging.

El proceso completo es:
1. Ollama genera `float[]` (1024 elementos)
2. `SerializeEmbedding` → JSON string
3. SQLite parameter binding → BLOB
4. sqlite-vec indexa el vector para búsquedas KNN
```

## Historial de conversación

```
>> ¿Qué es sqlite-vec?
[NoPilot]: sqlite-vec es una extensión de SQLite que añade soporte nativo para vectores...

>> ¿Cómo se integra en este proyecto?
[NoPilot]: [Usa contexto de la pregunta anterior] En NoPilot, sqlite-vec se integra 
mediante el paquete NuGet `sqlite-vec` versión 0.1.7-alpha.2...

>> Muéstrame el código de inicialización
[NoPilot]: [Mantiene contexto] Claro, aquí está el método `InitializeAsync` de 
VectorStoreService.cs...
```

---

## 🎨 Personalización visual

El proyecto usa colores ANSI para feedback visual:

| Color | Uso |
|-------|-----|
| 🟢 Verde | Éxito, completado |
| 🟡 Amarillo | Avisos, información |
| 🔴 Rojo | Errores críticos |
| 🔵 Cyan | Respuestas del asistente |
| ⚪ Gris oscuro | Mensajes de sistema |

---

<div align="center">

**Más ejemplos y casos de uso en el [README principal](README.md)**

</div>
