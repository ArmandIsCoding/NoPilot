using System.ComponentModel;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using NoPilot.Services;

namespace NoPilot.Plugins;

/// <summary>
/// Plugin de Semantic Kernel que expone la búsqueda semántica sobre el codebase indexado.
/// Puede ser invocado por el kernel en escenarios de function calling con modelos que lo soporten.
/// </summary>
public sealed class CodebasePlugin
{
    private readonly IVectorStoreService _vectorStore;

    public CodebasePlugin(IVectorStoreService vectorStore)
    {
        _vectorStore = vectorStore;
    }

    [KernelFunction("buscar_codigo")]
    [Description("Busca fragmentos de código relevantes en el repositorio indexado según una consulta en lenguaje natural.")]
    public async Task<string> SearchCodebaseAsync(
        [Description("La consulta de búsqueda sobre el código fuente")] string query,
        Kernel kernel,
        [Description("Número máximo de resultados a devolver (por defecto 5)")] int topK = 5)
    {
        var embeddingService = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
        var embeddings = await embeddingService.GenerateAsync([query]);
        var vector = embeddings[0].Vector.ToArray();

        var results = await _vectorStore.SearchAsync(vector, topK);

        if (results.Count == 0)
            return "No se encontraron fragmentos de código relevantes para la consulta.";

        var sb = new System.Text.StringBuilder();
        foreach (var result in results)
        {
            sb.AppendLine($"// {result.FilePath} (distancia: {result.Distance:F4})");
            sb.AppendLine(result.Content);
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
