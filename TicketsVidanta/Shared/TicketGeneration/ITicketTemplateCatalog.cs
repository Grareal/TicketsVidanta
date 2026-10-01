namespace TicketsVidanta.Shared.TicketGeneration;

public interface ITicketTemplateCatalog
{
    TicketTemplate Find(string sourceSystem, string resort, string? pointOfSale);
}

public sealed class DefaultTicketTemplateCatalog : ITicketTemplateCatalog
{
    public static TicketTemplate BuiltIn { get; } = new(
        Guid.Empty,
        "Formato termico predeterminado",
        null,
        null,
        null,
        true,
        new TicketLayout
        {
            Title = "VIDANTA",
            HeaderText = "Comprobante de consumo",
            LegalTextEs = "Al firmar este recibo acepto y reconozco que los consumos y cargos aqui senalados son correctos.",
            LegalTextEn = "By signing this voucher I accept and acknowledge that the listed charges are correct."
        },
        DateTimeOffset.MinValue);

    public TicketTemplate Find(string sourceSystem, string resort, string? pointOfSale) => BuiltIn;
}

public static class TicketTemplateSelector
{
    public static TicketTemplate Select(
        IEnumerable<TicketTemplate> templates,
        string sourceSystem,
        string resort,
        string? pointOfSale) => templates
        .Where(x => x.IsEnabled && Matches(x.SourceSystem, sourceSystem) && Matches(x.Resort, resort) && Matches(x.PointOfSale, pointOfSale))
        .OrderByDescending(Specificity)
        .ThenByDescending(x => x.UpdatedAtUtc)
        .FirstOrDefault() ?? DefaultTicketTemplateCatalog.BuiltIn;

    private static bool Matches(string? configured, string? actual) =>
        string.IsNullOrWhiteSpace(configured) || string.Equals(configured.Trim(), actual?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static int Specificity(TicketTemplate template) =>
        (string.IsNullOrWhiteSpace(template.SourceSystem) ? 0 : 4) +
        (string.IsNullOrWhiteSpace(template.Resort) ? 0 : 2) +
        (string.IsNullOrWhiteSpace(template.PointOfSale) ? 0 : 1);
}
