namespace TicketsVidanta.Shared.Configuration;

public sealed class OperaCloudOptions
{
    public const string SectionName = "OperaCloud";
    public bool UseMock { get; init; } = true;
    public bool EnableUpload { get; init; }
    public string GatewayUrl { get; init; } = string.Empty;
    public string TokenPath { get; init; } = "/oauth/v1/tokens";
    public string GrantType { get; init; } = "client_credentials";
    public string AppKey { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string EnterpriseId { get; init; } = string.Empty;
    public string Scope { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string DefaultResort { get; init; } = "VINV";
    public Dictionary<string, string> HotelIdsByResort { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    // Compatibilidad con la configuracion anterior. Solo se usa para DefaultResort.
    public string HotelId { get; init; } = string.Empty;
    public string ExternalSystemCode { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 30;
}
