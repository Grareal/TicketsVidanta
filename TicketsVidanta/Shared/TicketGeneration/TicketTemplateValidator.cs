using System.Text.RegularExpressions;

namespace TicketsVidanta.Shared.TicketGeneration;

public static partial class TicketTemplateValidator
{
    public static void Validate(SaveTicketTemplateRequest request)
    {
        Required(request.Name, "nombre", 120);
        if (request.Layout is null) throw new ArgumentException("La configuracion visual es obligatoria.");
        var layout = request.Layout;
        if (layout.PaperWidth is < 280 or > 1600) throw new ArgumentException("PaperWidth debe estar entre 280 y 1600.");
        if (layout.Padding is < 0 or > 160 || layout.Padding * 2 >= layout.PaperWidth)
            throw new ArgumentException("Padding no es valido para el ancho del papel.");
        if (layout.BaseFontSize is < 8 or > 40 || layout.HeaderFontSize is < 10 or > 72)
            throw new ArgumentException("Los tamanos de fuente estan fuera del rango permitido.");
        if (layout.LineHeight < layout.BaseFontSize || layout.LineHeight > 80)
            throw new ArgumentException("LineHeight debe ser mayor o igual a BaseFontSize y no superar 80.");
        ValidateColor(layout.TextColor, nameof(layout.TextColor));
        ValidateColor(layout.BackgroundColor, nameof(layout.BackgroundColor));
        ValidateColor(layout.SeparatorColor, nameof(layout.SeparatorColor));
        Required(layout.FontFamily, "FontFamily", 150);
        Required(layout.Title, "Title", 200);
        if (layout.AddressLines is null || layout.AddressLines.Count > 10 || layout.AddressLines.Any(x => x is null || x.Length > 300))
            throw new ArgumentException("AddressLines admite hasta 10 lineas de 300 caracteres.");
        foreach (var value in TextValues(layout).Where(x => x is not null))
            if (value!.Length > 4000) throw new ArgumentException("Un texto de plantilla supera 4000 caracteres.");
        Required(layout.DateFormat, "DateFormat", 100);
        try { _ = DateTime.UtcNow.ToString(layout.DateFormat); }
        catch (FormatException) { throw new ArgumentException("DateFormat no es un formato de fecha valido."); }
    }

    public static string MatchKey(string? sourceSystem, string? resort, string? pointOfSale) =>
        $"{Part(sourceSystem)}|{Part(resort)}|{Part(pointOfSale)}";

    private static IEnumerable<string?> TextValues(TicketLayout x) =>
    [
        x.LegalName, x.TaxId, x.TaxRegime, x.HeaderText, x.FooterText, x.TipLegend, x.TaxLegend,
        x.LegalTextEs, x.LegalTextEn, x.QuantityLabel, x.DescriptionLabel, x.AmountLabel, x.CheckLabel,
        x.DateLabel, x.RoomLabel, x.GuestLabel, x.SignatureLabel, x.TipLabel, x.TotalLabel
    ];

    private static void ValidateColor(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value) || !ColorRegex().IsMatch(value))
            throw new ArgumentException($"{name} debe usar formato #RRGGBB.");
    }

    private static string Required(string? value, string name, int maximum) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= maximum
            ? value.Trim()
            : throw new ArgumentException($"{name} es obligatorio y admite hasta {maximum} caracteres.");

    private static string Part(string? value) => string.IsNullOrWhiteSpace(value) ? "*" : value.Trim().ToUpperInvariant();

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex ColorRegex();
}
