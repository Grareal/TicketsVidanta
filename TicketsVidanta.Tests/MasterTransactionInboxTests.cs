using TicketsVidanta.Shared.Database.Master;

namespace TicketsVidanta.Tests;

public sealed class MasterTransactionInboxTests
{
    [Fact]
    public void Deduplicate_RemovesDuplicatesWithSameBusinessKey()
    {
        var now = DateTime.UtcNow;
        var transactions = new List<FinancialTransactionCandidate>
        {
            new("VINV", now, now, "TRX-1", "4SPA", "400106", "CHK-100", "RESV-01", "101", null, null, "SPA"),
            new("VINV", now, now, "TRX-2", "4SPA", "400106", "CHK-100", "RESV-01", "101", null, null, "SPA"),
            new("VINV", now, now, "TRX-3", "4SPA", "400106", "CHK-100", "RESV-01", "101", null, null, "SPA"),
            new("VINV", now, now, "TRX-4", "4SPA", "400106", "CHK-200", "RESV-02", "102", null, null, "SPA")
        };

        var result = SqlMasterTransactionInbox.Deduplicate(transactions);

        Assert.Equal(2, result.Count);
        Assert.Equal("CHK-100", result[0].CheckNumber);
        Assert.Equal("CHK-200", result[1].CheckNumber);
    }

    [Fact]
    public void Deduplicate_HandlesWhitespaceAndCaseInsensitivity()
    {
        var now = DateTime.UtcNow;
        var transactions = new List<FinancialTransactionCandidate>
        {
            new("vinv", now, now, "TRX-1", "4SPA", "400106", "45023648      OLN120261015094340", "45023648", "101", null, null, "SPA"),
            new("VINV ", now, now, "TRX-2", "4SPA", "400106", " 45023648      OLN120261015094340 ", " 45023648 ", "101", null, null, "SPA")
        };

        var result = SqlMasterTransactionInbox.Deduplicate(transactions);

        Assert.Single(result);
    }

    [Fact]
    public void Deduplicate_ReturnsEmpty_WhenInputIsEmpty()
    {
        var result = SqlMasterTransactionInbox.Deduplicate([]);
        Assert.Empty(result);
    }
}

