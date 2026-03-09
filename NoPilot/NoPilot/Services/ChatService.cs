using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using NoPilot.Models;

namespace NoPilot.Services;

public sealed class ChatService
{
    private readonly Kernel _kernel;
    private readonly IVectorStoreService _vectorStore;

    private readonly List<(string Role, string Message)> _conversationHistory = [];

    private const string SystemPrompt = """
        Eres NoPilot, un asistente especializado en analizar y responder preguntas sobre código fuente.
        Cuando se te proporciona contexto con fragmentos de código, basa tus respuestas en ese contexto.
        Si el contexto no contiene suficiente información para responder, indícalo claramente.
        Sé conciso, preciso y técnicamente riguroso.
        Responde siempre en el mismo idioma que usa el usuario.
        """;

    public ChatService(Kernel kernel, IVectorStoreService vectorStore)
    {
        _kernel = kernel;
        _vectorStore = vectorStore;
    }

    public async Task ChatAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        var embeddingService = _kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
        var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("[Buscando contexto relevante...]");
        Console.ResetColor();

        var embeddings = await embeddingService.GenerateAsync(
            [userMessage], cancellationToken: cancellationToken);
        var queryVector = embeddings[0].Vector.ToArray();
        var searchResults = await _vectorStore.SearchAsync(queryVector, topK: 5);

        Console.Write("\r                                      \r");

        var context = BuildContext(searchResults);
        var callHistory = BuildCallHistory(userMessage, context);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("[NoPilot]: ");
        Console.ResetColor();

        var fullResponse = new System.Text.StringBuilder();

        await foreach (var chunk in chatCompletion.GetStreamingChatMessageContentsAsync(
            callHistory, cancellationToken: cancellationToken))
        {
            Console.Write(chunk.Content);
            fullResponse.Append(chunk.Content);
        }

        Console.WriteLine("\n");

        _conversationHistory.Add(("user", userMessage));
        _conversationHistory.Add(("assistant", fullResponse.ToString()));
    }

    private ChatHistory BuildCallHistory(string userMessage, string context)
    {
        var history = new ChatHistory(SystemPrompt);

        const int maxHistoryPairs = 5;
        var startIndex = Math.Max(0, _conversationHistory.Count - maxHistoryPairs * 2);

        for (int i = startIndex; i < _conversationHistory.Count; i++)
        {
            var (role, msg) = _conversationHistory[i];
            if (role == "user") history.AddUserMessage(msg);
            else history.AddAssistantMessage(msg);
        }

        var augmentedMessage = string.IsNullOrEmpty(context)
            ? userMessage
            : $"""
              CONTEXTO DEL CÓDIGO FUENTE:
              ---
              {context}
              ---

              PREGUNTA: {userMessage}
              """;

        history.AddUserMessage(augmentedMessage);
        return history;
    }

    private static string BuildContext(IReadOnlyList<SearchResult> results)
    {
        if (results.Count == 0) return string.Empty;

        var sb = new System.Text.StringBuilder();
        foreach (var result in results)
        {
            sb.AppendLine($"[{result.FilePath}]");
            sb.AppendLine(result.Content);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public void ClearHistory() => _conversationHistory.Clear();
}
