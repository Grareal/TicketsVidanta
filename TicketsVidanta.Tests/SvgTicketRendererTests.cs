using System.Text;
using TicketsVidanta.Shared.Models;
using TicketsVidanta.Shared.TicketGeneration;

namespace TicketsVidanta.Tests;

public sealed class SvgTicketRendererTests
{
    [Fact]
    public async Task RenderAsync_UsesResolvedReceiptDataAndEscapesText()
    {
        var detail = new CheckDetail(
            [new CheckItem("Masaje & facial", 1, 1200m)],
            1200m,
            "MXN",
            new CheckReceiptDetails(GuestName: "Ana <Test>", PointOfSale: "SPA", CheckNumber: "77"));

        var result = await new SvgTicketRenderer().RenderAsync(
            TestFactory.Context(sourceSystem: "INSSIST_SPA"), detail, CancellationToken.None);
        var svg = Encoding.UTF8.GetString(result.Content.Span);

        Assert.Equal("image/svg+xml", result.MimeType);
        Assert.Contains("Masaje &amp; facial", svg);
        Assert.Contains("Ana &lt;Test&gt;", svg);
        Assert.Contains("TOTAL", svg);
    }
}
