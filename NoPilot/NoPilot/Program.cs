using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using NoPilot.Configuration;
using NoPilot.Plugins;
using NoPilot.Services;

// ── Configuración ────────────────────────────────────────────────────────────
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

var settings = configuration.Get<AppSettings>()
    ?? throw new InvalidOperationException("No se pudo leer appsettings.json");

// ── Inyección de dependencias ─────────────────────────────────────────────────
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
    builder.AddOllamaTextEmbeddingGeneration(cfg.Ollama.EmbeddingModel, new Uri(cfg.Ollama.Endpoint));
    var kernel = builder.Build();
    kernel.Plugins.AddFromObject(sp.GetRequiredService<CodebasePlugin>());
    return kernel;
});

var sp = services.BuildServiceProvider();

// ── Inicialización del vector store ──────────────────────────────────────────
var vectorStore = sp.GetRequiredService<IVectorStoreService>();
await vectorStore.InitializeAsync();

var chunkCount = await vectorStore.GetChunkCountAsync();
PrintBanner(settings, chunkCount);

// ── Cancelación con Ctrl+C ────────────────────────────────────────────────────
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

// ── Bucle principal de la consola ─────────────────────────────────────────────
var chatService = sp.GetRequiredService<ChatService>();

while (!cts.IsCancellationRequested)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write(">> ");
    Console.ResetColor();

    var input = Console.ReadLine()?.Trim();
    if (string.IsNullOrEmpty(input)) continue;

    switch (input.ToUpperInvariant())
    {
        case "SALIR":
            cts.Cancel();
            break;

        case "INGESTAR":
            var ingestionService = sp.GetRequiredService<IIngestionService>();
            await ingestionService.IngestAsync(cts.Token);
            break;

        case "LIMPIAR":
            await vectorStore.ClearAsync();
            chatService.ClearHistory();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[INFO] Base de datos y historial de chat limpiados.");
            Console.ResetColor();
            break;

        case "AYUDA":
            PrintHelp();
            break;

        default:
            try
            {
                await chatService.ChatAsync(input, cts.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] {ex.Message}");
                Console.ResetColor();
            }
            break;
    }
}

Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine("Hasta luego.");
Console.ResetColor();

// ── Funciones auxiliares ───────────────────────────────────────────────────────
static void PrintBanner(AppSettings cfg, long chunkCount)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("╔══════════════════════════════════════════════════╗");
    Console.WriteLine("║       NoPilot  ·  Asistente de Código Local      ║");
    Console.WriteLine("╚══════════════════════════════════════════════════╝");
    Console.ResetColor();
    Console.WriteLine($"  Chat model   : {cfg.Ollama.ChatModel}");
    Console.WriteLine($"  Embeddings   : {cfg.Ollama.EmbeddingModel}  ({cfg.Ollama.EmbeddingDimension}d)");
    Console.WriteLine($"  Ollama       : {cfg.Ollama.Endpoint}");
    Console.WriteLine($"  Carpeta      : {cfg.Ingestion.SourceFolder}");
    Console.WriteLine($"  Base de datos: {cfg.VectorStore.DatabasePath}  ({chunkCount} chunks indexados)");
    Console.WriteLine();
    PrintHelp();
}

static void PrintHelp()
{
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("  INGESTAR  → Indexa todos los archivos de la carpeta configurada");
    Console.WriteLine("  LIMPIAR   → Elimina el índice y el historial de chat");
    Console.WriteLine("  AYUDA     → Muestra este mensaje");
    Console.WriteLine("  SALIR     → Cierra la aplicación");
    Console.WriteLine("  <texto>   → Pregunta sobre el código indexado");
    Console.ResetColor();
    Console.WriteLine();
}
