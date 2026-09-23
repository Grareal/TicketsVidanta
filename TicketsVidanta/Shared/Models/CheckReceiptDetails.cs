namespace TicketsVidanta.Shared.Models;

public sealed record CheckReceiptDetails(
    string? GuestName = null,
    string? Room = null,
    string? PointOfSale = null,
    string? CheckNumber = null,
    DateTime? BusinessDate = null,
    TimeSpan? Time = null,
    decimal? Subtotal = null,
    decimal? Tip = null,
    decimal? Tax = null,
    string? Header = null,
    string? Footer = null);
