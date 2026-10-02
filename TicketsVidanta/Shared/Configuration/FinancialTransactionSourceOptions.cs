using System.ComponentModel.DataAnnotations;

namespace TicketsVidanta.Shared.Configuration;

public sealed class FinancialTransactionSourceOptions
{
    public const string SectionName = "FinancialTransactionSource";
    public bool Enabled { get; init; }
    public string ConnectionStringName { get; init; } = "FinancialTransactions";
    public string Schema { get; init; } = "dbo";
    public string Table { get; init; } = "FINANCIAL_TRANSACTIONS_P_DET_CLOUD";
    public string ResortColumn { get; init; } = "RESORT";
    public string TransactionDateColumn { get; init; } = "TRX_DATE";
    public string BusinessDateColumn { get; init; } = "BUSINESS_DATE";
    public string TransactionNumberColumn { get; init; } = "TRX_NO";
    public string TcGroupColumn { get; init; } = "TC_GROUP";
    public string TcSubGroupColumn { get; init; } = "TC_SUBGROUP";

    public string resort { get; init; } = "RESORTT";


    public string TrxCodeColumn { get; init; } = "TRX_CODE";
    public string CheckNumberColumn { get; init; } = "CHEQUE_NUMBER";
    public string ReservationIdColumn { get; init; } = "RESV_NAME_ID";
    public string RoomColumn { get; init; } = "ROOM";
    public string ReferenceColumn { get; init; } = "REFERENCE";
    public string RemarkColumn { get; init; } = "REMARK";
    [Range(1, 10000)] public int BatchSize { get; init; } = 1000;
    [Range(1, 365)] public int LookbackDays { get; init; } = 7;
    [Range(5, 3600)] public int IntervalSeconds { get; init; } = 60;
}
