namespace TicketsVidanta.Shared.TicketGeneration;

public sealed record TicketTemplate(
    Guid Id,
    string Name,
    string? SourceSystem,
    string? Resort,
    string? PointOfSale,
    bool IsEnabled,
    TicketLayout Layout,
    DateTimeOffset UpdatedAtUtc);

public sealed record SaveTicketTemplateRequest(
    Guid? Id,
    string? Name,
    string? SourceSystem,
    string? Resort,
    string? PointOfSale,
    bool IsEnabled,
    TicketLayout? Layout);

public sealed class TicketLayout
{
    public int PaperWidth { get; init; } = 420;
    public int Padding { get; init; } = 22;
    public string FontFamily { get; init; } = "Courier New, monospace";
    public int BaseFontSize { get; init; } = 14;
    public int HeaderFontSize { get; init; } = 22;
    public int LineHeight { get; init; } = 20;
    public string TextColor { get; init; } = "#111111";
    public string BackgroundColor { get; init; } = "#ffffff";
    public string SeparatorColor { get; init; } = "#777777";

    public string Title { get; init; } = "VIDANTA";
    public string? LegalName { get; init; }
    public string? TaxId { get; init; }
    public string? TaxRegime { get; init; }
    public List<string> AddressLines { get; init; } = [];
    public string? HeaderText { get; init; }
    public string? FooterText { get; init; }
    public string? TipLegend { get; init; } = "Propina no incluida / Tip not included";
    public string? TaxLegend { get; init; } = "Impuestos incluidos / Taxes included";
    public string? LegalTextEs { get; init; }
    public string? LegalTextEn { get; init; }

    public string QuantityLabel { get; init; } = "Cant";
    public string DescriptionLabel { get; init; } = "Descripcion";
    public string AmountLabel { get; init; } = "Importe";
    public string CheckLabel { get; init; } = "Folio";
    public string DateLabel { get; init; } = "Fecha";
    public string RoomLabel { get; init; } = "Habitacion/Room";
    public string GuestLabel { get; init; } = "Nombre/Name";
    public string SignatureLabel { get; init; } = "Firma/Signature";
    public string TipLabel { get; init; } = "Propina/Tip";
    public string TotalLabel { get; init; } = "TOTAL";

    public bool ShowGuest { get; init; } = true;
    public bool ShowRoom { get; init; } = true;
    public bool ShowPointOfSale { get; init; } = true;
    public bool ShowServer { get; init; } = true;
    public bool ShowTable { get; init; } = true;
    public bool ShowGuestCount { get; init; } = true;
    public bool ShowTurn { get; init; } = true;
    public bool ShowSubtotal { get; init; } = true;
    public bool ShowTax { get; init; } = true;
    public bool ShowTip { get; init; } = true;
    public bool ShowTipLine { get; init; } = true;
    public bool ShowSignatureLine { get; init; } = true;
    public bool ShowAmountInWords { get; init; } = true;
    public string DateFormat { get; init; } = "dd/MM/yyyy";
}
