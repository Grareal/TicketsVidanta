namespace TicketsVidanta.Shared.Database.Master;

public sealed record FinancialTransactionCandidate(
    string Resort,
    DateTime? TransactionDate,
    DateTime? BusinessDate,
    string? TransactionNumber,
    string TcGroup,
    string TrxCode,
    string CheckNumber,
    string ReservationId,
    string? Room,
    string? Reference,
    string? Remark,
    string SourceSystem);
