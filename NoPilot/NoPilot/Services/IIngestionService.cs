namespace NoPilot.Services;

public interface IIngestionService
{
    Task IngestAsync(CancellationToken cancellationToken = default);
}
