using TicketsVidanta.Features.DatabaseConfiguration;

namespace TicketsVidanta.Tests;

public sealed class ReadOnlySqlTemplateValidatorTests
{
    [Fact]
    public void Validate_AcceptsParameterizedLimitedSelect()
    {
        var sql = "SELECT TOP (@MaxRows) folio AS CheckNumber FROM dbo.hotche WHERE cargar_a=@ReservationId";

        Assert.Equal(sql, ReadOnlySqlTemplateValidator.Validate(sql));
    }

    [Theory]
    [InlineData("UPDATE dbo.hotche SET folio='x' WHERE cargar_a=@ReservationId")]
    [InlineData("SELECT folio AS CheckNumber FROM dbo.hotche WHERE cargar_a=@ReservationId")]
    [InlineData("SELECT TOP (@MaxRows) folio FROM dbo.hotche; DELETE dbo.hotche WHERE cargar_a=@ReservationId")]
    public void Validate_RejectsUnsafeOrUnlimitedSql(string sql)
    {
        Assert.Throws<ArgumentException>(() => ReadOnlySqlTemplateValidator.Validate(sql));
    }
}
