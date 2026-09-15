namespace TicketsVidanta.Shared.Models;

/// <summary>Renglón genérico de un cheque, sin depender del esquema de un comercio.</summary>
public sealed record CheckItem(string Description, decimal Quantity, decimal Amount);
