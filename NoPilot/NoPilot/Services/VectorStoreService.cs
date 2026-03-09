using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using NoPilot.Configuration;
using NoPilot.Models;

namespace NoPilot.Services;

public sealed class VectorStoreService : IVectorStoreService, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppSettings _settings;

    public VectorStoreService(AppSettings settings)
    {
        _settings = settings;
        _connection = new SqliteConnection($"Data Source={settings.VectorStore.DatabasePath}");
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();

        // sqlite-vec coloca vec0.dll en runtimes/{rid}/native/ dentro del output.
        // LoadExtension("vec0") lo busca en el directorio raíz, por eso resolvemos la ruta absoluta.
        _connection.LoadExtension(ResolveVec0Path());

        var dim = _settings.Ollama.EmbeddingDimension;

        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"""
            CREATE TABLE IF NOT EXISTS chunks (
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                file_path   TEXT    NOT NULL,
                content     TEXT    NOT NULL,
                chunk_index INTEGER NOT NULL DEFAULT 0,
                created_at  TEXT    DEFAULT (datetime('now'))
            );
            CREATE VIRTUAL TABLE IF NOT EXISTS vec_chunks USING vec0(
                embedding float[{dim}]
            );
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpsertChunkAsync(DocumentChunk chunk)
    {
        using var transaction = _connection.BeginTransaction();

        long id;
        using (var metaCmd = _connection.CreateCommand())
        {
            metaCmd.Transaction = transaction;
            metaCmd.CommandText = """
                INSERT INTO chunks (file_path, content, chunk_index)
                VALUES (@filePath, @content, @chunkIndex)
                RETURNING id;
                """;
            metaCmd.Parameters.AddWithValue("@filePath", chunk.FilePath);
            metaCmd.Parameters.AddWithValue("@content", chunk.Content);
            metaCmd.Parameters.AddWithValue("@chunkIndex", chunk.ChunkIndex);
            id = (long)(await metaCmd.ExecuteScalarAsync())!;
        }

        using (var vecCmd = _connection.CreateCommand())
        {
            vecCmd.Transaction = transaction;
            vecCmd.CommandText = "INSERT INTO vec_chunks(rowid, embedding) VALUES (@id, @embedding);";
            vecCmd.Parameters.AddWithValue("@id", id);
            vecCmd.Parameters.AddWithValue("@embedding", SerializeEmbedding(chunk.Embedding));
            await vecCmd.ExecuteNonQueryAsync();
        }

        transaction.Commit();
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(float[] queryEmbedding, int topK = 5)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT c.id, c.file_path, c.content, v.distance
            FROM vec_chunks v
            JOIN chunks c ON c.id = v.rowid
            WHERE v.embedding MATCH @embedding
              AND k = @k
            ORDER BY v.distance;
            """;
        cmd.Parameters.AddWithValue("@embedding", SerializeEmbedding(queryEmbedding));
        cmd.Parameters.AddWithValue("@k", topK);

        var results = new List<SearchResult>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new SearchResult
            {
                ChunkId = reader.GetInt64(0),
                FilePath = reader.GetString(1),
                Content = reader.GetString(2),
                Distance = reader.GetDouble(3)
            });
        }
        return results;
    }

    public async Task ClearAsync()
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM chunks; DELETE FROM vec_chunks;";
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<long> GetChunkCountAsync()
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM chunks;";
        return (long)(await cmd.ExecuteScalarAsync())!;
    }

    private static string SerializeEmbedding(float[] embedding) =>
        JsonSerializer.Serialize(embedding);

    /// <summary>
    /// Resuelve la ruta absoluta a la librería nativa vec0 según la plataforma y arquitectura actuales.
    /// NuGet despliega los binarios nativos en runtimes/{rid}/native/ dentro del directorio de salida.
    /// </summary>
    private static string ResolveVec0Path()
    {
        var baseDir = AppContext.BaseDirectory;

        string rid, libName;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            libName = "vec0.dll";
            rid = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64   => "win-x64",
                Architecture.X86   => "win-x86",
                Architecture.Arm64 => "win-arm64",
                _                  => "win-x64"
            };
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            libName = "vec0.so";
            rid = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64   => "linux-x64",
                Architecture.Arm64 => "linux-arm64",
                _                  => "linux-x64"
            };
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            libName = "vec0.dylib";
            rid = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64   => "osx-x64",
                Architecture.Arm64 => "osx-arm64",
                _                  => "osx-arm64"
            };
        }
        else
        {
            throw new PlatformNotSupportedException("sqlite-vec no soporta esta plataforma.");
        }

        // Ruta en el layout de NuGet dentro del output de build/publish
        var runtimePath = Path.Combine(baseDir, "runtimes", rid, "native", libName);
        if (File.Exists(runtimePath))
            return runtimePath;

        // Fallback: publicación self-contained donde las libs quedan en el directorio raíz
        var localPath = Path.Combine(baseDir, libName);
        if (File.Exists(localPath))
            return localPath;

        throw new FileNotFoundException(
            $"No se encontró la extensión nativa sqlite-vec '{libName}'." +
            $" Ruta esperada: {runtimePath}",
            runtimePath);
    }

    public void Dispose() => _connection.Dispose();
}
