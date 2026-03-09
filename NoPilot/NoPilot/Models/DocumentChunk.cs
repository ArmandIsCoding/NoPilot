namespace NoPilot.Models;

public sealed class DocumentChunk
{
    public long Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public float[] Embedding { get; set; } = [];
}
