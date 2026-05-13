using System.Text;
using System.Text.RegularExpressions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using NoPilot.Configuration;

namespace NoPilot.Services;

public sealed class FileEditService
{
    private static readonly Regex FileTokenRegex = new(
        @"(?<path>[A-Za-z0-9_\-./\\]+\.[A-Za-z0-9_\-]+)",
        RegexOptions.Compiled);

    private readonly Kernel _kernel;
    private readonly AppSettings _settings;
    private readonly HashSet<string> _allowedExtensions;
    private readonly string _sourceFolderFullPath;

    public FileEditService(Kernel kernel, AppSettings settings)
    {
        _kernel = kernel;
        _settings = settings;
        _allowedExtensions = new HashSet<string>(settings.Ingestion.SupportedExtensions, StringComparer.OrdinalIgnoreCase);
        _sourceFolderFullPath = Path.GetFullPath(settings.Ingestion.SourceFolder);
    }

    public async Task<bool> TryHandleEditRequestAsync(string userMessage, CancellationToken cancellationToken)
    {
        if (!LooksLikeEditRequest(userMessage))
            return false;

        var filePath = ResolveTargetFilePath(userMessage);
        if (filePath is null)
        {
            WriteInfo("No detecte un archivo valido para editar en tu solicitud.");
            WriteInfo("Incluye la ruta (por ejemplo: 'traduce docs/README.md al ingles').");
            return true;
        }

        var extension = Path.GetExtension(filePath);
        if (!_allowedExtensions.Contains(extension))
        {
            WriteInfo($"La extension '{extension}' no esta permitida para edicion.");
            return true;
        }

        var fileSize = new FileInfo(filePath).Length;
        if (fileSize > _settings.Ingestion.MaxFileSizeBytes)
        {
            WriteInfo($"El archivo excede el tamano maximo permitido ({_settings.Ingestion.MaxFileSizeBytes} bytes).");
            return true;
        }

        var originalContent = await File.ReadAllTextAsync(filePath, cancellationToken);
        if (string.IsNullOrWhiteSpace(originalContent))
        {
            WriteInfo("El archivo esta vacio; no hay contenido para modificar.");
            return true;
        }

        WriteInfo($"Preparando propuesta de cambios para '{Path.GetRelativePath(_sourceFolderFullPath, filePath)}'...");
        var updatedContent = await GenerateUpdatedContentAsync(userMessage, filePath, originalContent, cancellationToken);

        if (string.Equals(originalContent, updatedContent, StringComparison.Ordinal))
        {
            WriteInfo("No hay cambios sugeridos por el modelo.");
            return true;
        }

        PrintDiffPreview(originalContent, updatedContent);

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("Aplicar cambios? (s/N): ");
        Console.ResetColor();
        var confirmation = Console.ReadLine()?.Trim().ToLowerInvariant();

        if (confirmation is not ("s" or "si" or "y" or "yes"))
        {
            WriteInfo("Cambios cancelados.");
            return true;
        }

        await File.WriteAllTextAsync(filePath, updatedContent, cancellationToken);
        WriteSuccess($"Archivo actualizado: {Path.GetRelativePath(_sourceFolderFullPath, filePath)}");
        WriteInfo("Sugerencia: ejecuta INGESTAR para refrescar el indice semantico con este cambio.");
        return true;
    }

    private async Task<string> GenerateUpdatedContentAsync(
        string instruction,
        string absolutePath,
        string originalContent,
        CancellationToken cancellationToken)
    {
        var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory(
            "Eres un asistente que edita archivos de texto de forma exacta. " +
            "Debes devolver UNICAMENTE el contenido final completo del archivo, sin markdown, sin backticks y sin explicaciones.");

        history.AddUserMessage($$"""
            Instruccion del usuario:
            {{instruction}}

            Ruta del archivo:
            {{absolutePath}}

            Contenido actual del archivo (entre etiquetas):
            <archivo>
            {{originalContent}}
            </archivo>

            Devuelve solo el contenido final completo.
            """);

        var sb = new StringBuilder();
        await foreach (var chunk in chatCompletion.GetStreamingChatMessageContentsAsync(history, cancellationToken: cancellationToken))
        {
            sb.Append(chunk.Content);
        }

        return CleanupAssistantContent(sb.ToString());
    }

    private static bool LooksLikeEditRequest(string message)
    {
        if (!FileTokenRegex.IsMatch(message))
            return false;

        var lower = message.ToLowerInvariant();
        return lower.Contains("edita")
               || lower.Contains("modifica")
               || lower.Contains("cambia")
               || lower.Contains("actualiza")
               || lower.Contains("traduce")
               || lower.Contains("corrige")
               || lower.Contains("refactoriza")
               || lower.Contains("rewrite")
               || lower.Contains("translate");
    }

    private string? ResolveTargetFilePath(string userMessage)
    {
        var candidates = FileTokenRegex
            .Matches(userMessage)
            .Select(m => m.Groups["path"].Value.Trim('"', '\'', '`', '.', ',', ';', ':', ')', '(', '[', ']'))
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var token in candidates)
        {
            var resolved = ResolvePathToken(token);
            if (resolved is not null)
                return resolved;
        }

        return null;
    }

    private string? ResolvePathToken(string token)
    {
        string fullPath;

        if (Path.IsPathRooted(token))
            fullPath = Path.GetFullPath(token);
        else
            fullPath = Path.GetFullPath(Path.Combine(_sourceFolderFullPath, token.Replace('\\', Path.DirectorySeparatorChar)));

        if (!fullPath.StartsWith(_sourceFolderFullPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(fullPath, _sourceFolderFullPath, StringComparison.OrdinalIgnoreCase))
            return null;

        return File.Exists(fullPath) ? fullPath : null;
    }

    private static string CleanupAssistantContent(string content)
    {
        var cleaned = content.Trim();

        if (cleaned.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewLine = cleaned.IndexOf('\n');
            if (firstNewLine >= 0)
                cleaned = cleaned[(firstNewLine + 1)..];

            var closingFence = cleaned.LastIndexOf("```", StringComparison.Ordinal);
            if (closingFence >= 0)
                cleaned = cleaned[..closingFence];
        }

        return cleaned.Trim();
    }

    private static void PrintDiffPreview(string original, string updated)
    {
        var oldLines = SplitLines(original);
        var newLines = SplitLines(updated);
        var maxLines = Math.Max(oldLines.Length, newLines.Length);
        const int previewLimit = 120;

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("\n[Vista previa de cambios]");
        Console.ResetColor();

        int printed = 0;
        for (var i = 0; i < maxLines && printed < previewLimit; i++)
        {
            var oldLine = i < oldLines.Length ? oldLines[i] : null;
            var newLine = i < newLines.Length ? newLines[i] : null;

            if (string.Equals(oldLine, newLine, StringComparison.Ordinal))
                continue;

            if (oldLine is not null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"- {oldLine}");
                Console.ResetColor();
                printed++;
            }

            if (newLine is not null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"+ {newLine}");
                Console.ResetColor();
                printed++;
            }
        }

        if (printed >= previewLimit)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("...diff truncado para mantener la salida legible...");
            Console.ResetColor();
        }

        Console.WriteLine();
    }

    private static string[] SplitLines(string content)
        => content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

    private static void WriteInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"[INFO] {message}");
        Console.ResetColor();
    }

    private static void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[OK] {message}");
        Console.ResetColor();
    }
}

