using System.Collections.Concurrent;
using TicketsVidanta.Shared.Models;

namespace TicketsVidanta.Shared.Processing;

/// <summary>Registro no persistente destinado a desarrollo y pruebas.</summary>
public sealed class InMemoryProcessingRegistry : IProcessingRegistry
{
    private readonly ConcurrentDictionary<ProcessingKey, ProcessingRecord> _records = new();

    public Task<bool> HasAlreadyBeenProcessedAsync(ProcessingKey key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_records.TryGetValue(key, out var record) && record.Status == ProcessingStatus.Completed);
    }

    public Task<bool> TryRegisterStartedAsync(ProcessingKey key, Guid correlationId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var record = new ProcessingRecord(key, correlationId, ProcessingStatus.Resolving, DateTimeOffset.UtcNow, null, null);
        return Task.FromResult(_records.TryAdd(key, record));
    }

    public Task RegisterCompletedAsync(ProcessingKey key, Guid correlationId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _records[key] = _records[key] with
        {
            Status = ProcessingStatus.Completed,
            CompletedAt = DateTimeOffset.UtcNow,
            ErrorMessage = null
        };
        return Task.CompletedTask;
    }

    public Task RegisterFailedAsync(ProcessingKey key, Guid correlationId, string errorMessage, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_records.TryGetValue(key, out var current))
        {
            _records[key] = current with
            {
                Status = ProcessingStatus.Failed,
                CompletedAt = DateTimeOffset.UtcNow,
                ErrorMessage = errorMessage
            };
        }
        return Task.CompletedTask;
    }

    public Task<ProcessingRecord?> GetAsync(Guid correlationId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_records.Values.FirstOrDefault(x => x.CorrelationId == correlationId));
    }
}
