using System.Text;
using System.Xml.Linq;
using TicketsVidanta.Shared.Models;
using TicketsVidanta.Shared.TicketGeneration;

namespace TicketsVidanta.Tests;

public sealed class TicketTemplateTests
{
    [Fact]
    public void Selector_PrefersSourceResortAndPointOfSale()
    {
        var wildcard = Template("General", null, null, null);
        var hotel = Template("Hotel", "INSSIST", "ACA", null);
        var exact = Template("Exacta", "INSSIST", "ACA", "P5H");

        var selected = TicketTemplateSelector.Select([wildcard, hotel, exact], "inssist", "aca", "p5h");

        Assert.Equal("Exacta", selected.Name);
    }

    [Fact]
    public async Task Renderer_UsesConfigurableThermalSectionsAndEscapesValues()
    {
        var layout = new TicketLayout
        {
            Title = "YULL & GO",
            LegalName = "OPERADORA <HOTELERA>",
            ShowSignatureLine = false,
            ShowAmountInWords = true,
            LegalTextEs = "Texto legal configurable"
        };
        var renderer = new SvgTicketRenderer(new FixedCatalog(Template("Prueba", null, null, null, layout)));
        var detail = new CheckDetail(
            [new CheckItem("JUEGO 2X1", 1, 659.40m)], 659.40m, "MXN",
            new CheckReceiptDetails(PointOfSale: "P5H", CheckNumber: "100824", Server: "01", Table: "R1"));

        var result = await renderer.RenderAsync(TestFactory.Context(sourceSystem: "INSSIST"), detail, CancellationToken.None);
        var svg = Encoding.UTF8.GetString(result.Content.Span);
        var document = XDocument.Parse(svg);

        Assert.Equal("svg", document.Root!.Name.LocalName);
        Assert.Contains("YULL &amp; GO", svg);
        Assert.Contains("OPERADORA &lt;HOTELERA&gt;", svg);
        Assert.Contains("SEISCIENTOS CINCUENTA Y NUEVE 40/100 MXN", svg);
        Assert.Contains("Texto legal configurable", svg);
        Assert.DoesNotContain("Firma/Signature:", svg);
    }

    [Fact]
    public void Validator_RejectsUnsafeColorsAndInvalidDimensions()
    {
        var badColor = new SaveTicketTemplateRequest(null, "x", null, null, null, true,
            new TicketLayout { TextColor = "red" });
        var badWidth = new SaveTicketTemplateRequest(null, "x", null, null, null, true,
            new TicketLayout { PaperWidth = 100 });

        Assert.Throws<ArgumentException>(() => TicketTemplateValidator.Validate(badColor));
        Assert.Throws<ArgumentException>(() => TicketTemplateValidator.Validate(badWidth));
    }

    private static TicketTemplate Template(
        string name, string? source, string? resort, string? pos, TicketLayout? layout = null) =>
        new(Guid.NewGuid(), name, source, resort, pos, true, layout ?? new TicketLayout(), DateTimeOffset.UtcNow);

    private sealed class FixedCatalog(TicketTemplate template) : ITicketTemplateCatalog
    {
        public TicketTemplate Find(string sourceSystem, string resort, string? pointOfSale) => template;
    }
}
