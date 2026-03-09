using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using NoPilot.Configuration;
using NoPilot.Models;

namespace NoPilot.Services;

public sealed class IngestionService : IIngestionService
{
    private readonly Kernel _kernel;
    private readonly IVectorStoreService _vectorStore;
    private readonly AppSettings _settings;

    public IngestionService(Kernel kernel, IVectorStoreService vectorStore, AppSettings settings)
    {
        _kernel = kernel;
        _vectorStore = vectorStore;
        _settings = settings;
    }

    public async Task IngestAsync(CancellationToken cancellationToken = default)
    {
        var sourceFolder = _settings.Ingestion.SourceFolder;

        if (!Directory.Exists(sourceFolder))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] La carpeta '{sourceFolder}' no existe. Revisa SourceFolder en appsettings.json.");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("[INGESTAR] Limpiando datos anteriores...");
        Console.ResetColor();
        await _vectorStore.ClearAsync();

        var files = GetSupportedFiles(sourceFolder);
        Console.WriteLine($"[INGESTAR] {files.Count} archivos encontrados para indexar.");

        if (files.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[INGESTAR] No se encontraron archivos con las extensiones configuradas.");
            Console.ResetColor();
            return;
        }

        var embeddingService = _kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        int processedFiles = 0;
        int processedChunks = 0;
        int skippedFiles = 0;

        foreach (var filePath in files)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                var content = await File.ReadAllTextAsync(filePath, cancellationToken);
                if (string.IsNullOrWhiteSpace(content))
                {
                    skippedFiles++;
                    continue;
                }

                var relativePath = Path.GetRelativePath(sourceFolder, filePath);
                var chunks = SplitIntoChunks(content, relativePath).ToList();

                foreach (var (chunkText, chunkIndex) in chunks)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    var embeddings = await embeddingService.GenerateEmbeddingsAsync(
                        [chunkText], cancellationToken: cancellationToken);

                    await _vectorStore.UpsertChunkAsync(new DocumentChunk
                    {
                        FilePath = relativePath,
                        Content = chunkText,
                        ChunkIndex = chunkIndex,
                        Embedding = embeddings[0].ToArray()
                    });

                    processedChunks++;
                }

                processedFiles++;
                Console.Write($"\r[INGESTAR] {processedFiles}/{files.Count} archivos | {processedChunks} chunks | {skippedFiles} omitidos     ");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                skippedFiles++;
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"\n[AVISO] No se pudo procesar '{filePath}': {ex.Message}");
                Console.ResetColor();
            }
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[INGESTAR] Completado: {processedFiles} archivos, {processedChunks} chunks indexados, {skippedFiles} omitidos.");
        Console.ResetColor();
    }

    private List<string> GetSupportedFiles(string folder)
    {
        var extensions = new HashSet<string>(
            _settings.Ingestion.SupportedExtensions,
            StringComparer.OrdinalIgnoreCase);
        var maxSize = _settings.Ingestion.MaxFileSizeBytes;

        return Directory
            .EnumerateFiles(folder, "*", SearchOption.AllDirectories)
            .Where(f => extensions.Contains(Path.GetExtension(f)))
            .Where(f => new FileInfo(f).Length <= maxSize)
            .OrderBy(f => f)
            .ToList();
    }

    private IEnumerable<(string text, int index)> SplitIntoChunks(string content, string relativePath)
    {
        var chunkSize = _settings.Ingestion.ChunkSize;
        var overlap = _settings.Ingestion.ChunkOverlap;
        var header = $"// Archivo: {relativePath}\n";
        var lines = content.Split('\n');

        var currentChunk = new System.Text.StringBuilder(header);
        int chunkIndex = 0;
        int currentSize = header.Length;

        foreach (var line in lines)
        {
            if (currentSize + line.Length + 1 > chunkSize && currentSize > header.Length)
            {
                var chunkText = currentChunk.ToString().TrimEnd();
                if (!string.IsNullOrWhiteSpace(chunkText))
                    yield return (chunkText, chunkIndex++);

                var overlapText = ExtractOverlap(chunkText, overlap);
                currentChunk.Clear();
                currentChunk.Append(header);
                currentChunk.Append(overlapText);
                currentSize = header.Length + overlapText.Length;
            }

            currentChunk.AppendLine(line);
            currentSize += line.Length + 1;
        }

        if (currentSize > header.Length)
        {
            var lastChunk = currentChunk.ToString().TrimEnd();
            if (!string.IsNullOrWhiteSpace(lastChunk))
                yield return (lastChunk, chunkIndex);
        }
    }

    private static string ExtractOverlap(string text, int overlapLength)
    {
        if (text.Length <= overlapLength) return text;
        var cutPoint = text.Length - overlapLength;
        var newlinePos = text.IndexOf('\n', cutPoint);
        return newlinePos >= 0 ? text[(newlinePos + 1)..] : text[cutPoint..];
    }
}
