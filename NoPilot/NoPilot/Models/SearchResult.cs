namespace NoPilot.Models;

public sealed class SearchResult
{
    public long ChunkId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public double Distance { get; set; }
}
