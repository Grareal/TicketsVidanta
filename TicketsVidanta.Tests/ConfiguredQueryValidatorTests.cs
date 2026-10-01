using TicketsVidanta.Features.DatabaseConfiguration;

namespace TicketsVidanta.Tests;

public sealed class ConfiguredQueryValidatorTests
{
    [Fact]
    public void Select_WithTicketParameters_IsAccepted()
    {
        var sql = "SELECT codigo AS ItemDescription FROM hotayb WHERE codigo=@CheckNumber;";

        var result = ConfiguredQueryValidator.ValidateAndNormalize(sql);

        Assert.DoesNotContain(';', result);
        Assert.Contains("@CheckNumber", result);
    }

    [Theory]
    [InlineData("DELETE FROM hotche")]
    [InlineData("SELECT * INTO copia FROM hotche")]
    [InlineData("SELECT * FROM hotche; SELECT * FROM hotcom")]
    [InlineData("EXEC dbo.algo")]
    public void NonReadOnlyOrMultipleStatements_AreRejected(string sql) =>
        Assert.Throws<ArgumentException>(() => ConfiguredQueryValidator.ValidateAndNormalize(sql));
}
