using System.Text.RegularExpressions;

namespace TicketsVidanta.Features.DatabaseConfiguration;

/// <summary>
/// Applies a small contract to administrator-defined ticket queries. The configured
/// connection must still use a database login restricted to SELECT permissions.
/// </summary>
public static partial class ConfiguredQueryValidator
{
    public static string ValidateAndNormalize(string sql)
    {
        var normalized = sql.Trim();
        if (normalized.EndsWith(';')) normalized = normalized[..^1].TrimEnd();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new ArgumentException("La consulta personalizada no puede estar vacia.");
        if (!StartsWithSelectRegex().IsMatch(normalized))
            throw new ArgumentException("La consulta personalizada debe iniciar con SELECT o WITH.");
        if (normalized.Contains(';') || normalized.Contains("--", StringComparison.Ordinal) ||
            normalized.Contains("/*", StringComparison.Ordinal) || normalized.Contains("*/", StringComparison.Ordinal))
            throw new ArgumentException("La consulta debe contener una sola sentencia y no admite comentarios SQL.");
        if (ForbiddenKeywordRegex().IsMatch(normalized))
            throw new ArgumentException("La consulta contiene una operacion no permitida. Solo se admite lectura SELECT.");
        return normalized;
    }

    [GeneratedRegex(@"^\s*(SELECT\b|WITH\b)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex StartsWithSelectRegex();

    [GeneratedRegex(@"\b(INSERT|UPDATE|DELETE|MERGE|DROP|ALTER|CREATE|TRUNCATE|EXEC(?:UTE)?|GRANT|REVOKE|DENY|BACKUP|RESTORE|DBCC|INTO)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ForbiddenKeywordRegex();
}
