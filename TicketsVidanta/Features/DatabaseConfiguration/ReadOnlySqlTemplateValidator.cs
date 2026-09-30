using System.Text.RegularExpressions;

namespace TicketsVidanta.Features.DatabaseConfiguration;

public static partial class ReadOnlySqlTemplateValidator
{
    private static readonly string[] ForbiddenKeywords =
    [
        "INSERT", "UPDATE", "DELETE", "MERGE", "DROP", "ALTER", "CREATE", "TRUNCATE",
        "EXEC", "EXECUTE", "GRANT", "REVOKE", "DENY", "BACKUP", "RESTORE", "DBCC", "INTO"
    ];

    public static string Validate(string sql)
    {
        var value = sql.Trim();
        if (value.Length is < 10 or > 30_000)
            throw new ArgumentException("La consulta SQL debe tener entre 10 y 30,000 caracteres.");
        if (value.Contains(';') || value.Contains("--", StringComparison.Ordinal) ||
            value.Contains("/*", StringComparison.Ordinal) || value.Contains("*/", StringComparison.Ordinal))
            throw new ArgumentException("La consulta no admite punto y coma ni comentarios SQL.");
        if (!SelectOrCteRegex().IsMatch(value))
            throw new ArgumentException("La consulta debe iniciar con SELECT o WITH.");
        foreach (var keyword in ForbiddenKeywords)
            if (Regex.IsMatch(value, $@"\b{keyword}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                throw new ArgumentException($"La instrucción {keyword} no está permitida en una consulta de solo lectura.");
        if (!ParameterRegex().IsMatch(value))
            throw new ArgumentException("La consulta debe filtrar por @ReservationId, @CheckNumber o @Resort.");
        if (!MaxRowsRegex().IsMatch(value))
            throw new ArgumentException("La consulta debe limitar resultados con TOP (@MaxRows).");
        return value;
    }

    [GeneratedRegex(@"^\s*(SELECT|WITH)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SelectOrCteRegex();
    [GeneratedRegex(@"@(ReservationId|CheckNumber|Resort)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ParameterRegex();
    [GeneratedRegex(@"\bTOP\s*\(\s*@MaxRows\s*\)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MaxRowsRegex();
}
