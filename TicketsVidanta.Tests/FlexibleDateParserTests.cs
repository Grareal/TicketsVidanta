using TicketsVidanta.Shared.Database;
using Xunit;

namespace TicketsVidanta.Tests;

public class FlexibleDateParserTests
{
    [Theory]
    [InlineData("20261015", 2026, 10, 15)]
    [InlineData("20261002", 2026, 10, 2)]
    [InlineData("20110806", 2011, 8, 6)]
    [InlineData("2026-10-15", 2026, 10, 15)]
    [InlineData("2026/10/15", 2026, 10, 15)]
    [InlineData("15/10/2026", 2026, 10, 15)]
    public void ParseNullable_StringFormats_ReturnsCorrectDate(string input, int year, int month, int day)
    {
        var result = FlexibleDateParser.ParseNullable(input);
        Assert.NotNull(result);
        Assert.Equal(new DateTime(year, month, day), result.Value.Date);
    }

    [Theory]
    [InlineData(20261015, 2026, 10, 15)]
    [InlineData(20261002, 2026, 10, 2)]
    [InlineData(20110806, 2011, 8, 6)]
    public void ParseNullable_IntegerFormats_ReturnsCorrectDate(int input, int year, int month, int day)
    {
        var result = FlexibleDateParser.ParseNullable(input);
        Assert.NotNull(result);
        Assert.Equal(new DateTime(year, month, day), result.Value.Date);
    }

    [Fact]
    public void ParseNullable_DateTimeObject_ReturnsExactDateTime()
    {
        var expected = new DateTime(2026, 10, 15, 14, 30, 0);
        var result = FlexibleDateParser.ParseNullable(expected);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ParseNullable_DateOnlyObject_ReturnsDateTimeAtMidnight()
    {
        var input = new DateOnly(2026, 10, 15);
        var result = FlexibleDateParser.ParseNullable(input);
        Assert.NotNull(result);
        Assert.Equal(new DateTime(2026, 10, 15), result.Value);
    }

    [Fact]
    public void Parse_NullOrDbNull_ReturnsFallback()
    {
        var fallback = new DateTime(2026, 7, 1);
        Assert.Equal(fallback, FlexibleDateParser.Parse(null, fallback));
        Assert.Equal(fallback, FlexibleDateParser.Parse(DBNull.Value, fallback));
        Assert.Equal(fallback, FlexibleDateParser.Parse("   ", fallback));
    }

    [Fact]
    public void Parse_InvalidValue_ReturnsFallbackWithoutException()
    {
        var fallback = new DateTime(2026, 7, 1);
        Assert.Equal(fallback, FlexibleDateParser.Parse("not-a-date", fallback));
        Assert.Equal(fallback, FlexibleDateParser.Parse(999999999999L, fallback));
    }
}

