namespace TicketsVidanta.Features.DatabaseConfiguration;

using TicketsVidanta.Shared.TicketGeneration;

internal interface IConfigurationRepository
{
    Task<IReadOnlyList<DatabaseConnectionSummary>> GetConnectionsAsync(CancellationToken cancellationToken);
    Task<StoredDatabaseConnection?> GetConnectionAsync(Guid id, CancellationToken cancellationToken);
    Task<Guid> SaveConnectionAsync(SaveDatabaseConnectionRequest request, CancellationToken cancellationToken);
    Task DeleteConnectionAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<TicketProfile>> GetProfilesAsync(CancellationToken cancellationToken);
    Task<Guid> SaveProfileAsync(SaveTicketProfileRequest request, CancellationToken cancellationToken);
    Task DeleteProfileAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<TransactionRoute>> GetRoutesAsync(CancellationToken cancellationToken);
    Task<Guid> SaveRouteAsync(SaveTransactionRouteRequest request, CancellationToken cancellationToken);
    Task DeleteRouteAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<TicketTemplate>> GetTemplatesAsync(CancellationToken cancellationToken);
    Task<Guid> SaveTemplateAsync(SaveTicketTemplateRequest request, CancellationToken cancellationToken);
    Task DeleteTemplateAsync(Guid id, CancellationToken cancellationToken);
    Task<RuntimeConfigurationSnapshot> GetRuntimeSnapshotAsync(CancellationToken cancellationToken);
}
