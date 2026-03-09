namespace NoPilot.Configuration;

public sealed class AppSettings
{
    public OllamaSettings Ollama { get; set; } = new();
    public IngestionSettings Ingestion { get; set; } = new();
    public VectorStoreSettings VectorStore { get; set; } = new();
}

public sealed class OllamaSettings
{
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string ChatModel { get; set; } = "deepseek-coder:6.7b";
    public string EmbeddingModel { get; set; } = "mxbai-embed-large";
    /// <summary>
    /// Dimensión del vector de embeddings. mxbai-embed-large = 1024.
    /// Cambiar este valor requiere eliminar la base de datos y reingestar.
    /// </summary>
    public int EmbeddingDimension { get; set; } = 1024;
}

public sealed class IngestionSettings
{
    public string SourceFolder { get; set; } = string.Empty;
    public string[] SupportedExtensions { get; set; } = [".cs", ".md", ".txt"];
    public int ChunkSize { get; set; } = 1500;
    public int ChunkOverlap { get; set; } = 200;
    public long MaxFileSizeBytes { get; set; } = 1_048_576;
}

public sealed class VectorStoreSettings
{
    public string DatabasePath { get; set; } = "nopilot.db";
}
