using NoPilot.Models;

namespace NoPilot.Services;

public interface IVectorStoreService
{
    Task InitializeAsync();
    Task UpsertChunkAsync(DocumentChunk chunk);
    Task<IReadOnlyList<SearchResult>> SearchAsync(float[] queryEmbedding, int topK = 5);
    Task ClearAsync();
    Task<long> GetChunkCountAsync();
}
