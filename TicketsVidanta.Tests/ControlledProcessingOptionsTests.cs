using TicketsVidanta.Shared.Configuration;

namespace TicketsVidanta.Tests;

public sealed class ControlledProcessingOptionsTests
{
    [Fact]
    public void ToSelection_RequiresAtLeastOneSelector()
    {
        var options = new ControlledProcessingOptions();

        Assert.Throws<InvalidOperationException>(() => options.ToSelection());
    }

    [Fact]
    public void ToSelection_PreservesExactDocumentAndMaximum()
    {
        var options = new ControlledProcessingOptions
        {
            MaxDocuments = 1,
            Documents =
            [
                new() { Resort = " VINV ", ReservationId = " 123456 ", CheckNumber = " CHK-001 " }
            ],
            SourceSystems = ["INSSIST_SPA", "inssist_spa"]
        };

        var selection = options.ToSelection();

        Assert.Equal(1, selection.MaxDocuments);
        Assert.Equal(new MasterTransactionDocument("VINV", "123456", "CHK-001"), selection.Documents.Single());
        Assert.Equal(["INSSIST_SPA"], selection.SourceSystems);
    }
}
