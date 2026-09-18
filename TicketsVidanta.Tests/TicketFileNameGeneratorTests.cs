using TicketsVidanta.Features.Tickets.ProcesarCheque;
using TicketsVidanta.Shared.Naming;

namespace TicketsVidanta.Tests;

public sealed class TicketFileNameGeneratorTests
{
    [Fact]
    public void Generate_UsesExpectedPartsAndSanitizesInvalidCharacters()
    {
        var context = TestFactory.Context(checkNumber: "CHK:001");
        var timestamp = new DateTimeOffset(2026, 9, 15, 3, 4, 5, TimeSpan.Zero);

        var result = new TicketFileNameGenerator().Generate(context, timestamp);

        Assert.StartsWith("VID-TEST-123456-CHK-001-20260915030405-", result);
        Assert.EndsWith(".png", result);
        Assert.DoesNotContain(':', result);
    }

    [Theory]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("image/webp", ".webp")]
    public void Generate_UsesExtensionMatchingImageType(string mimeType, string extension)
    {
        var result = new TicketFileNameGenerator().Generate(
            TestFactory.Context(), DateTimeOffset.UtcNow, mimeType);

        Assert.EndsWith(extension, result);
    }
}
