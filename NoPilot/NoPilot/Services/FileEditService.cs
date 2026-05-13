using System.Text;
using System.Text.RegularExpressions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using NoPilot.Configuration;

namespace NoPilot.Services;

public sealed class FileEditService(Kernel kernel, AppSettings settings)
{
    private const int DiffContextLines = 3;
    private const int MaxPreviewDiffLines = 300;

    private static readonly Regex FileTokenRegex = new(
        @"(?<path>[A-Za-z0-9_\-./\\]+\.[A-Za-z0-9_\-]+)",
        RegexOptions.Compiled);

    private readonly HashSet<string> _allowedExtensions = new(settings.Ingestion.SupportedExtensions, StringComparer.OrdinalIgnoreCase);
    private readonly string _sourceFolderFullPath = Path.GetFullPath(settings.Ingestion.SourceFolder);

    public async Task<bool> TryHandleEditRequestAsync(string userMessage, CancellationToken cancellationToken)
    {
        // Intenta manejar solicitud de creacion de nuevo archivo primero
        if (await TryHandleCreateRequestAsync(userMessage, cancellationToken))
            return true;

        // Luego intenta edicion de archivos existentes
        if (!LooksLikeEditRequest(userMessage))
            return false;

        var filePaths = ResolveTargetFilePaths(userMessage).ToList();
        if (filePaths.Count == 0)
        {
            WriteInfo("No detecte un archivo valido para editar en tu solicitud.");
            WriteInfo("Incluye la ruta (por ejemplo: 'traduce docs/README.md al ingles').");
            return true;
        }

        var proposals = new List<FileEditProposal>();

        foreach (var filePath in filePaths)
        {
            if (!TryValidateFile(filePath, out var validationError))
            {
                WriteInfo($"{Path.GetRelativePath(_sourceFolderFullPath, filePath)} omitido: {validationError}");
                continue;
            }

            var originalContent = await File.ReadAllTextAsync(filePath, cancellationToken);
            if (string.IsNullOrWhiteSpace(originalContent))
            {
                WriteInfo($"{Path.GetRelativePath(_sourceFolderFullPath, filePath)} esta vacio; se omite.");
                continue;
            }

            WriteInfo($"Preparando propuesta para '{Path.GetRelativePath(_sourceFolderFullPath, filePath)}'...");
            var updatedContent = await GenerateUpdatedContentAsync(userMessage, filePath, originalContent, cancellationToken);

            if (string.Equals(originalContent, updatedContent, StringComparison.Ordinal))
            {
                WriteInfo($"Sin cambios sugeridos en '{Path.GetRelativePath(_sourceFolderFullPath, filePath)}'.");
                continue;
            }

            proposals.Add(new FileEditProposal(filePath, originalContent, updatedContent));
        }

        if (proposals.Count == 0)
        {
            WriteInfo("No hay cambios aplicables en los archivos detectados.");
            return true;
        }

        foreach (var proposal in proposals)
        {
            var relativePath = Path.GetRelativePath(_sourceFolderFullPath, proposal.AbsolutePath);
            PrintUnifiedDiffPreview(relativePath, proposal.OriginalContent, proposal.UpdatedContent);
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"Aplicar cambios en {proposals.Count} archivo(s)? (s/N): ");
        Console.ResetColor();
        var confirmation = Console.ReadLine()?.Trim().ToLowerInvariant();

        if (confirmation is not ("s" or "si" or "y" or "yes"))
        {
            WriteInfo("Cambios cancelados.");
            return true;
        }

        foreach (var proposal in proposals)
        {
            var backupPath = await CreateBackupAsync(proposal.AbsolutePath, proposal.OriginalContent, cancellationToken);
            await File.WriteAllTextAsync(proposal.AbsolutePath, proposal.UpdatedContent, cancellationToken);
            WriteSuccess($"Archivo actualizado: {Path.GetRelativePath(_sourceFolderFullPath, proposal.AbsolutePath)}");
            WriteInfo($"Backup: {backupPath}");
        }

        WriteInfo("Sugerencia: ejecuta INGESTAR para refrescar el indice semantico con este cambio.");
        return true;
    }

    public async Task<bool> TryHandleCreateRequestAsync(string userMessage, CancellationToken cancellationToken)
    {
        if (!LooksLikeCreateRequest(userMessage))
            return false;

        var filePath = ResolveNewFilePath(userMessage);
        if (filePath is null)
        {
            WriteInfo("No detecte ruta de archivo para crear. Incluye el nombre (ej: 'crea config.json con...').");
            return true;
        }

        if (!TryValidateFile(filePath, out var validationError))
        {
            WriteInfo($"No se puede crear el archivo: {validationError}");
            return true;
        }

        if (File.Exists(filePath))
        {
            WriteInfo($"El archivo '{Path.GetRelativePath(_sourceFolderFullPath, filePath)}' ya existe. Usa edicion para modificarlo.");
            return true;
        }

        WriteInfo($"Generando contenido para nuevo archivo '{Path.GetRelativePath(_sourceFolderFullPath, filePath)}'...");
        var newContent = await GenerateNewFileContentAsync(userMessage, filePath, cancellationToken);

        if (string.IsNullOrWhiteSpace(newContent))
        {
            WriteInfo("No se genero contenido para el nuevo archivo.");
            return true;
        }

        PrintNewFilePreview(Path.GetRelativePath(_sourceFolderFullPath, filePath), newContent);

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("Crear archivo? (s/N): ");
        Console.ResetColor();
        var confirmation = Console.ReadLine()?.Trim().ToLowerInvariant();

        if (confirmation is not ("s" or "si" or "y" or "yes"))
        {
            WriteInfo("Creacion cancelada.");
            return true;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? _sourceFolderFullPath);
        await File.WriteAllTextAsync(filePath, newContent, cancellationToken);
        WriteSuccess($"Archivo creado: {Path.GetRelativePath(_sourceFolderFullPath, filePath)}");
        WriteInfo("Sugerencia: ejecuta INGESTAR para indexar este nuevo archivo.");
        return true;
    }

    private bool TryValidateFile(string filePath, out string error)
    {
        var extension = Path.GetExtension(filePath);
        if (!_allowedExtensions.Contains(extension))
        {
            error = $"extension '{extension}' no permitida para edicion";
            return false;
        }

        // Solo valida tamaño si el archivo existe (para edición)
        if (File.Exists(filePath))
        {
            var fileSize = new FileInfo(filePath).Length;
            if (fileSize > settings.Ingestion.MaxFileSizeBytes)
            {
                error = $"archivo excede tamano maximo permitido ({settings.Ingestion.MaxFileSizeBytes} bytes)";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    private async Task<string> GenerateUpdatedContentAsync(
        string instruction,
        string absolutePath,
        string originalContent,
        CancellationToken cancellationToken)
    {
        var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
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

    private async Task<string> GenerateNewFileContentAsync(string instruction, string absolutePath, CancellationToken cancellationToken)
    {
        var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory(
            "Eres un asistente que crea archivos de texto. " +
            "Debes devolver UNICAMENTE el contenido completo del archivo, sin markdown, sin backticks y sin explicaciones.");

        history.AddUserMessage($$"""
            Instruccion del usuario:
            {{instruction}}

            Ruta del nuevo archivo:
            {{absolutePath}}

            Genera el contenido completo del archivo.
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

    private static bool LooksLikeCreateRequest(string message)
    {
        var lower = message.ToLowerInvariant();
        return (lower.Contains("crea ") || lower.Contains("crear ") || lower.Contains("create ")
                || lower.Contains("nuevo archivo") || lower.Contains("new file"))
               && FileTokenRegex.IsMatch(message);
    }

    private IEnumerable<string> ResolveTargetFilePaths(string userMessage)
    {
        var candidates = FileTokenRegex
            .Matches(userMessage)
            .Select(m => m.Groups["path"].Value.Trim('"', '\'', '`', '.', ',', ';', ':', ')', '(', '[', ']'))
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var resolved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in candidates)
        {
            var fullPath = ResolvePathToken(token);
            if (fullPath is not null)
                resolved.Add(fullPath);
        }

        return resolved;
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

    private string? ResolveNewFilePath(string userMessage)
    {
        var candidates = FileTokenRegex
            .Matches(userMessage)
            .Select(m => m.Groups["path"].Value.Trim('"', '\'', '`', '.', ',', ';', ':', ')', '(', '[', ']'))
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var token in candidates)
        {
            string fullPath;

            if (Path.IsPathRooted(token))
                fullPath = Path.GetFullPath(token);
            else
                fullPath = Path.GetFullPath(Path.Combine(_sourceFolderFullPath, token.Replace('\\', Path.DirectorySeparatorChar)));

            if (!fullPath.StartsWith(_sourceFolderFullPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(fullPath, _sourceFolderFullPath, StringComparison.OrdinalIgnoreCase))
                continue;

            return fullPath;
        }

        return null;
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

    private async Task<string> CreateBackupAsync(string absolutePath, string originalContent, CancellationToken cancellationToken)
    {
        var relativePath = Path.GetRelativePath(_sourceFolderFullPath, absolutePath);
        var backupRoot = Path.Combine(_sourceFolderFullPath, ".nopilot-backups");
        var relativeDirectory = Path.GetDirectoryName(relativePath) ?? string.Empty;
        var originalFileName = Path.GetFileName(relativePath);
        var stamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        var backupDirectory = Path.Combine(backupRoot, relativeDirectory);

        Directory.CreateDirectory(backupDirectory);

        var backupFileName = $"{originalFileName}.{stamp}.bak";
        var backupPath = Path.Combine(backupDirectory, backupFileName);
        await File.WriteAllTextAsync(backupPath, originalContent, cancellationToken);
        return backupPath;
    }

    private static void PrintUnifiedDiffPreview(string relativePath, string original, string updated)
    {
        var ops = ComputeDiffOperations(SplitLines(original), SplitLines(updated));
        var diff = BuildUnifiedDiff(relativePath, ops, DiffContextLines, MaxPreviewDiffLines);

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"\n[Diff] {relativePath}");
        Console.ResetColor();
        Console.WriteLine(diff);
    }

    private static void PrintNewFilePreview(string relativePath, string content)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"\n[Nuevo archivo] {relativePath}");
        Console.ResetColor();

        var lines = SplitLines(content);
        var maxPreviewLines = 50;
        var displayLines = Math.Min(lines.Length, maxPreviewLines);

        for (int i = 0; i < displayLines; i++)
        {
            Console.WriteLine(lines[i]);
        }

        if (lines.Length > maxPreviewLines)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"...truncado ({lines.Length - maxPreviewLines} lineas mas)...");
            Console.ResetColor();
        }

        Console.WriteLine();
    }

    private static List<DiffOperation> ComputeDiffOperations(string[] oldLines, string[] newLines)
    {
        int n = oldLines.Length;
        int m = newLines.Length;
        int max = n + m;
        var v = new Dictionary<int, int> { [1] = 0 };
        var trace = new List<Dictionary<int, int>>();

        for (int d = 0; d <= max; d++)
        {
            trace.Add(new Dictionary<int, int>(v));

            for (int k = -d; k <= d; k += 2)
            {
                int x;
                if (k == -d || (k != d && GetV(v, k - 1) < GetV(v, k + 1)))
                    x = GetV(v, k + 1);
                else
                    x = GetV(v, k - 1) + 1;

                int y = x - k;
                while (x < n && y < m && string.Equals(oldLines[x], newLines[y], StringComparison.Ordinal))
                {
                    x++;
                    y++;
                }

                v[k] = x;

                if (x >= n && y >= m)
                    return BacktrackDiff(oldLines, newLines, trace, d);
            }
        }

        return [];
    }

    private static List<DiffOperation> BacktrackDiff(
        IReadOnlyList<string> oldLines,
        IReadOnlyList<string> newLines,
        IReadOnlyList<Dictionary<int, int>> trace,
        int maxDepth)
    {
        int x = oldLines.Count;
        int y = newLines.Count;
        var diff = new List<DiffOperation>();

        for (var d = maxDepth; d >= 0; d--)
        {
            var v = trace[d];
            int k = x - y;

            int prevK;
            if (k == -d || (k != d && GetV(v, k - 1) < GetV(v, k + 1)))
                prevK = k + 1;
            else
                prevK = k - 1;

            int prevX = GetV(v, prevK);
            int prevY = prevX - prevK;

            while (x > prevX && y > prevY)
            {
                diff.Add(new DiffOperation(DiffOpKind.Equal, oldLines[x - 1]));
                x--;
                y--;
            }

            if (d == 0)
                break;

            if (x == prevX)
            {
                diff.Add(new DiffOperation(DiffOpKind.Insert, newLines[y - 1]));
                y--;
            }
            else
            {
                diff.Add(new DiffOperation(DiffOpKind.Delete, oldLines[x - 1]));
                x--;
            }
        }

        diff.Reverse();
        return diff;
    }

    private static string BuildUnifiedDiff(string relativePath, IReadOnlyList<DiffOperation> ops, int contextLines, int lineLimit)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"--- a/{relativePath}");
        sb.AppendLine($"+++ b/{relativePath}");

        var changedIndices = Enumerable
            .Range(0, ops.Count)
            .Where(i => ops[i].Kind != DiffOpKind.Equal)
            .ToList();

        if (changedIndices.Count == 0)
        {
            sb.AppendLine("(sin diferencias)");
            return sb.ToString();
        }

        var oldPos = new int[ops.Count + 1];
        var newPos = new int[ops.Count + 1];
        int oldLine = 1;
        int newLine = 1;

        for (int i = 0; i < ops.Count; i++)
        {
            oldPos[i] = oldLine;
            newPos[i] = newLine;

            if (ops[i].Kind != DiffOpKind.Insert)
                oldLine++;
            if (ops[i].Kind != DiffOpKind.Delete)
                newLine++;
        }

        oldPos[ops.Count] = oldLine;
        newPos[ops.Count] = newLine;

        int printedLines = 0;
        int changeCursor = 0;

        while (changeCursor < changedIndices.Count && printedLines < lineLimit)
        {
            int firstChange = changedIndices[changeCursor];
            int hunkStart = Math.Max(0, firstChange - contextLines);
            int hunkEnd = Math.Min(ops.Count - 1, firstChange + contextLines);

            while (changeCursor + 1 < changedIndices.Count)
            {
                int nextChange = changedIndices[changeCursor + 1];
                if (nextChange <= hunkEnd + contextLines)
                {
                    hunkEnd = Math.Min(ops.Count - 1, nextChange + contextLines);
                    changeCursor++;
                    continue;
                }
                break;
            }

            int oldStart = oldPos[hunkStart];
            int newStart = newPos[hunkStart];
            int oldCount = 0;
            int newCount = 0;

            for (int i = hunkStart; i <= hunkEnd; i++)
            {
                if (ops[i].Kind != DiffOpKind.Insert)
                    oldCount++;
                if (ops[i].Kind != DiffOpKind.Delete)
                    newCount++;
            }

            sb.AppendLine($"@@ -{oldStart},{oldCount} +{newStart},{newCount} @@");

            for (int i = hunkStart; i <= hunkEnd && printedLines < lineLimit; i++)
            {
                var prefix = ops[i].Kind switch
                {
                    DiffOpKind.Equal => ' ',
                    DiffOpKind.Delete => '-',
                    DiffOpKind.Insert => '+',
                    _ => ' '
                };

                sb.Append(prefix);
                sb.AppendLine(ops[i].Text);
                printedLines++;
            }

            changeCursor++;
        }

        if (printedLines >= lineLimit)
            sb.AppendLine("...diff truncado para mantener la salida legible...");

        return sb.ToString();
    }

    private static int GetV(IReadOnlyDictionary<int, int> map, int key)
        => map.TryGetValue(key, out var value) ? value : 0;

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

    private sealed record FileEditProposal(string AbsolutePath, string OriginalContent, string UpdatedContent);

    private sealed record DiffOperation(DiffOpKind Kind, string Text);

    private enum DiffOpKind
    {
        Equal,
        Delete,
        Insert
    }
}

