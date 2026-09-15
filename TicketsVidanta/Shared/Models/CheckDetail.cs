namespace TicketsVidanta.Shared.Models;

/// <summary>Detalle normalizado devuelto por un resolver.</summary>
public sealed record CheckDetail(IReadOnlyList<CheckItem> Items, decimal? Total, string? Currency);
