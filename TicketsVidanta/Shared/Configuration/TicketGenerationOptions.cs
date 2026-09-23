namespace TicketsVidanta.Shared.Configuration;

public sealed class TicketGenerationOptions
{
    public const string SectionName = "TicketGeneration";
    public bool UseMock { get; init; } = true;
    public string MimeType { get; init; } = "image/svg+xml";
    public string OutputDirectory { get; init; } = "App_Data/generated-tickets";
}
