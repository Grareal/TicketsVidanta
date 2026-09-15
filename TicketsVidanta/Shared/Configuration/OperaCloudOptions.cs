namespace TicketsVidanta.Shared.Configuration;

public sealed class OperaCloudOptions
{
    public const string SectionName = "OperaCloud";
    public string BaseUrl { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public string Scope { get; init; } = string.Empty;
    public bool UseMock { get; init; } = true;
    // Nunca agregar ClientSecret a archivos versionados. Usar variables de entorno,
    // Azure Key Vault u otro proveedor corporativo de secretos cuando se defina.
}
