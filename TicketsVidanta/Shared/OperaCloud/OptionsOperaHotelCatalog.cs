using Microsoft.Extensions.Options;
using TicketsVidanta.Shared.Configuration;

namespace TicketsVidanta.Shared.OperaCloud;

public sealed class OptionsOperaHotelCatalog(IOptions<OperaCloudOptions> options) : IOperaHotelCatalog
{
    public string ResolveHotelId(string? resort)
    {
        var value = options.Value;
        var resortCode = string.IsNullOrWhiteSpace(resort)
            ? value.DefaultResort.Trim()
            : resort.Trim();

        var match = value.HotelIdsByResort.FirstOrDefault(entry =>
            string.Equals(entry.Key.Trim(), resortCode, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(match.Value)) return match.Value.Trim();

        if (string.Equals(resortCode, value.DefaultResort.Trim(), StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(value.HotelId))
            return value.HotelId.Trim();

        throw new InvalidOperationException(
            $"No existe HotelId de Opera configurado para el resort '{resortCode}'.");
    }
}
