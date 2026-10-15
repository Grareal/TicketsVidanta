using System.Globalization;

namespace TicketsVidanta.Shared.Database;

/// <summary>
/// Parser polimórfico y resiliente para columnas de fecha procedentes de sistemas heterogéneos (Inssist, Simphony, etc.)
/// Soporta nativamente:
/// - DATE, DATETIME, DATETIME2 (objetos DateTime / DateOnly / DateTimeOffset)
/// - VARCHAR(8) y CHAR(8) formato YYYYMMDD (ej. '20261015')
/// - INT y BIGINT formato YYYYMMDD (ej. 20261015)
/// - Formatos delimitados estándar ISO y regionales (YYYY-MM-DD, YYYY/MM/DD, DD/MM/YYYY, etc.)
/// </summary>
public static class FlexibleDateParser
{
    private static readonly string[] ExactDateFormats =
    [
        "yyyyMMdd",
        "yyyy-MM-dd",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd HH:mm:ss.fff",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss.fff",
        "yyyy/MM/dd",
        "yyyy/MM/dd HH:mm:ss",
        "dd/MM/yyyy",
        "dd/MM/yyyy HH:mm:ss",
        "MM/dd/yyyy",
        "yyyyMMdd HHmmss"
    ];

    public static DateTime Parse(object? rawValue, DateTime fallback = default) =>
        ParseNullable(rawValue) ?? fallback;

    public static DateTime? ParseNullable(object? rawValue)
    {
        if (rawValue is null or DBNull) return null;

        switch (rawValue)
        {
            case DateTime dt:
                return dt;

            case DateOnly dOnly:
                return dOnly.ToDateTime(TimeOnly.MinValue);

            case DateTimeOffset dto:
                return dto.DateTime;

            case int intVal:
                return ParseIntegerYmd(intVal);

            case long longVal:
                return ParseIntegerYmd(longVal);

            case short shortVal:
                return ParseIntegerYmd(shortVal);

            case string s:
            {
                var trimmed = s.Trim();
                if (string.IsNullOrEmpty(trimmed)) return null;

                // 1. Intentar formatos exactos conocidos de Inssist y SQL
                if (DateTime.TryParseExact(trimmed, ExactDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exactDt))
                    return exactDt;

                // 2. Si tiene longitud 8 y son dígitos (ej. "20261015")
                if (trimmed.Length == 8 && int.TryParse(trimmed, NumberStyles.None, CultureInfo.InvariantCulture, out var ymdInt))
                {
                    var parsed = ParseIntegerYmd(ymdInt);
                    if (parsed.HasValue) return parsed.Value;
                }

                // 3. Parser genérico con InvariantCulture
                if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var genericDt))
                    return genericDt;

                return null;
            }

            default:
            {
                var str = Convert.ToString(rawValue, CultureInfo.InvariantCulture)?.Trim();
                if (string.IsNullOrEmpty(str)) return null;

                if (DateTime.TryParseExact(str, ExactDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fallbackExact))
                    return fallbackExact;

                if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fallbackGeneric))
                    return fallbackGeneric;

                return null;
            }
        }
    }

    private static DateTime? ParseIntegerYmd(long value)
    {
        // Rango válido YYYYMMDD: entre 19000101 y 21001231
        if (value < 19000101 || value > 21001231)
            return null;

        var y = (int)(value / 10000);
        var m = (int)((value % 10000) / 100);
        var d = (int)(value % 100);

        if (m < 1 || m > 12 || d < 1 || d > 31)
            return null;

        try
        {
            return new DateTime(y, m, d);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }
}

