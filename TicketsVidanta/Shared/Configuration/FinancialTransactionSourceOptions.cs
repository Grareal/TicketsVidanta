using System.ComponentModel.DataAnnotations;

namespace TicketsVidanta.Shared.Configuration;

public sealed class FinancialTransactionSourceOptions
{
    public const string SectionName = "FinancialTransactionSource";
    public bool Enabled { get; init; }
    public string ConnectionStringName { get; init; } = "FinancialTransactions";
    [Range(1, 10000)] public int BatchSize { get; init; } = 1000;
    [Range(1, 365)] public int LookbackDays { get; init; } = 7;
    [Range(5, 3600)] public int IntervalSeconds { get; init; } = 60;
}
