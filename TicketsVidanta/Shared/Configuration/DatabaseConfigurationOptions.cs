namespace TicketsVidanta.Shared.Configuration;

public sealed class DatabaseConfigurationOptions
{
    public const string SectionName = "DatabaseConfiguration";
    public bool EnableAdminUi { get; init; }
    public string KeyRingDirectory { get; init; } = "App_Data/data-protection-keys";
}
