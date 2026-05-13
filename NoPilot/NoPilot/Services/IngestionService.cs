using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using NoPilot.Configuration;
using NoPilot.Models;

namespace NoPilot.Services;

public sealed class IngestionService : IIngestionService
{
    private const int MinChunkBodyChars = 200;
    private const int MaxSplitRetries = 3;

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

        var embeddingService = _kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
        int processedFiles = 0;
        int processedChunks = 0;
        int skippedFiles = 0;
        int skippedChunks = 0;

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
                int nextChunkIndex = 0;

                foreach (var chunkText in chunks)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    var processedFromChunk = await ProcessChunkWithRetryAsync(
                        embeddingService,
                        relativePath,
                        chunkText,
                        nextChunkIndex,
                        0,
                        cancellationToken);

                    nextChunkIndex += processedFromChunk;
                    processedChunks += processedFromChunk;

                    if (processedFromChunk == 0)
                        skippedChunks++;
                }

                if (nextChunkIndex > 0)
                    processedFiles++;
                else
                    skippedFiles++;

                Console.Write($"\r[INGESTAR] {processedFiles}/{files.Count} archivos | {processedChunks} chunks | {skippedFiles} omitidos ({skippedChunks} chunks)     ");
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
        Console.WriteLine($"[INGESTAR] Completado: {processedFiles} archivos, {processedChunks} chunks indexados, {skippedFiles} archivos omitidos, {skippedChunks} chunks omitidos.");
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

    private async Task<int> ProcessChunkWithRetryAsync(
        IEmbeddingGenerator<string, Embedding<float>> embeddingService,
        string relativePath,
        string chunkText,
        int chunkIndex,
        int retryDepth,
        CancellationToken cancellationToken)
    {
        try
        {
            var embeddings = await embeddingService.GenerateAsync(
                [chunkText], cancellationToken: cancellationToken);

            await _vectorStore.UpsertChunkAsync(new DocumentChunk
            {
                FilePath = relativePath,
                Content = chunkText,
                ChunkIndex = chunkIndex,
                Embedding = embeddings[0].Vector.ToArray()
            });

            return 1;
        }
        catch (Exception ex) when (IsContextLengthError(ex) && retryDepth < MaxSplitRetries)
        {
            var header = BuildChunkHeader(relativePath);
            var body = ExtractBody(chunkText, header);
            var currentBodyBudget = Math.Max(MinChunkBodyChars, chunkText.Length - header.Length);
            var splitBodyBudget = Math.Max(MinChunkBodyChars, currentBodyBudget / 2);

            if (splitBodyBudget >= currentBodyBudget)
            {
                ReportSkippedChunk(relativePath, chunkIndex, retryDepth, ex.Message);
                return 0;
            }

            var splitOverlap = Math.Min(_settings.Ingestion.ChunkOverlap, Math.Max(0, splitBodyBudget / 4));
            var subChunks = SplitContentWithOverlap(body, header, splitBodyBudget, splitOverlap).ToList();

            if (subChunks.Count <= 1)
            {
                ReportSkippedChunk(relativePath, chunkIndex, retryDepth, ex.Message);
                return 0;
            }

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"\n[AVISO] Chunk de '{relativePath}' excedio contexto; subdividiendo en {subChunks.Count} partes (reintento {retryDepth + 1}/{MaxSplitRetries}).");
            Console.ResetColor();

            int processed = 0;
            foreach (var subChunk in subChunks)
            {
                processed += await ProcessChunkWithRetryAsync(
                    embeddingService,
                    relativePath,
                    subChunk,
                    chunkIndex + processed,
                    retryDepth + 1,
                    cancellationToken);
            }

            return processed;
        }
        catch (Exception ex) when (IsContextLengthError(ex))
        {
            ReportSkippedChunk(relativePath, chunkIndex, retryDepth, ex.Message);
            return 0;
        }
    }

    private IEnumerable<string> SplitIntoChunks(string content, string relativePath)
    {
        var header = BuildChunkHeader(relativePath);
        var normalized = content.Replace("\r\n", "\n");
        var configuredChunkSize = Math.Max(MinChunkBodyChars, _settings.Ingestion.ChunkSize);
        var safeLimit = Math.Max(MinChunkBodyChars, _settings.Ingestion.MaxEmbeddingInputChars - _settings.Ingestion.EmbeddingInputSafetyMarginChars);
        var effectiveChunkSize = Math.Max(MinChunkBodyChars, Math.Min(configuredChunkSize, safeLimit));
        var bodyBudget = Math.Max(MinChunkBodyChars, effectiveChunkSize - header.Length);
        var overlap = Math.Min(_settings.Ingestion.ChunkOverlap, Math.Max(0, bodyBudget - 1));

        return SplitContentWithOverlap(normalized, header, bodyBudget, overlap);
    }

    private static IEnumerable<string> SplitContentWithOverlap(string content, string header, int bodyBudget, int overlap)
    {
        if (string.IsNullOrWhiteSpace(content))
            yield break;

        int start = 0;

        while (start < content.Length)
        {
            var maxEnd = Math.Min(start + bodyBudget, content.Length);
            var end = maxEnd;

            if (maxEnd < content.Length)
            {
                var candidate = FindNaturalSplit(content, start, maxEnd);
                if (candidate > start)
                    end = candidate;
            }

            var body = content[start..end].Trim('\n', '\r');
            if (!string.IsNullOrWhiteSpace(body))
                yield return header + body;

            if (end >= content.Length)
                break;

            var nextStart = Math.Max(0, end - overlap);
            if (nextStart <= start)
                nextStart = end;

            start = nextStart;
        }
    }

    private static int FindNaturalSplit(string content, int start, int proposedEnd)
    {
        var lookBack = Math.Min(300, proposedEnd - start);
        var minSearch = Math.Max(start + 1, proposedEnd - lookBack);

        for (int i = proposedEnd - 1; i >= minSearch; i--)
        {
            if (content[i] == '\n')
                return i + 1;
        }

        for (int i = proposedEnd - 1; i >= minSearch; i--)
        {
            if (char.IsWhiteSpace(content[i]))
                return i + 1;
        }

        return proposedEnd;
    }

    private static string BuildChunkHeader(string relativePath) => $"// Archivo: {relativePath}\n";

    private static string ExtractBody(string chunkText, string header)
    {
        return chunkText.StartsWith(header, StringComparison.Ordinal) ? chunkText[header.Length..] : chunkText;
    }

    private static bool IsContextLengthError(Exception ex)
        => ex.Message.Contains("context length", StringComparison.OrdinalIgnoreCase)
           || ex.Message.Contains("input length exceeds", StringComparison.OrdinalIgnoreCase);

    private static void ReportSkippedChunk(string relativePath, int chunkIndex, int retryDepth, string message)
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine($"\n[AVISO] Chunk omitido en '{relativePath}' (indice {chunkIndex}, reintentos {retryDepth}): {message}");
        Console.ResetColor();
    }
}
