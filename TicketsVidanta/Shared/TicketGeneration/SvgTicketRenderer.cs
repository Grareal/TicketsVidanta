using System.Globalization;
using System.Net;
using System.Text;
using TicketsVidanta.Features.Tickets.ProcesarCheque;
using TicketsVidanta.Shared.Models;

namespace TicketsVidanta.Shared.TicketGeneration;

public sealed class SvgTicketRenderer : ITicketRenderer
{
    private readonly ITicketTemplateCatalog _catalog;

    public SvgTicketRenderer() : this(new DefaultTicketTemplateCatalog()) { }

    public SvgTicketRenderer(ITicketTemplateCatalog catalog) => _catalog = catalog;

    public Task<GeneratedTicket> RenderAsync(
        CheckProcessingContext context,
        CheckDetail detail,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var template = _catalog.Find(context.SourceSystem, context.Resort, detail.Receipt?.PointOfSale);
        return Task.FromResult(Render(context, detail, template.Layout));
    }

    public static GeneratedTicket Render(CheckProcessingContext context, CheckDetail detail, TicketLayout layout)
    {
        TicketTemplateValidator.Validate(new SaveTicketTemplateRequest(
            null, "preview", null, null, null, true, layout));

        var receipt = detail.Receipt ?? new CheckReceiptDetails();
        var writer = new ThermalSvgWriter(layout);
        var title = !string.IsNullOrWhiteSpace(receipt.Header) ? receipt.Header! : layout.Title;

        writer.Center(title, layout.HeaderFontSize, bold: true);
        writer.OptionalCenter(layout.LegalName);
        writer.OptionalCenter(layout.TaxId is null ? null : $"RFC: {layout.TaxId}");
        writer.OptionalCenter(layout.TaxRegime);
        foreach (var line in layout.AddressLines) writer.OptionalCenter(line);
        writer.OptionalCenter(layout.HeaderText);
        writer.Gap(5);
        writer.Separator();

        writer.LeftRight(
            $"{layout.CheckLabel}: {receipt.CheckNumber ?? context.CheckNumber}",
            receipt.CopyNumber is null ? null : $"Copia: {receipt.CopyNumber}");
        var date = receipt.BusinessDate?.ToString(layout.DateFormat, CultureInfo.InvariantCulture) ?? "-";
        var time = receipt.Time?.ToString(@"hh\:mm") ?? "-";
        writer.LeftRight($"{layout.DateLabel}: {date}", $"Hora: {time}");
        if (layout.ShowPointOfSale) writer.OptionalLine(receipt.PointOfSale, "Punto de venta: ");
        writer.Metadata(receipt, layout);
        writer.Separator();

        writer.ColumnsHeader(layout.QuantityLabel, layout.DescriptionLabel, layout.AmountLabel);
        writer.Separator();
        foreach (var item in detail.Items) writer.Item(item);
        if (detail.Items.Count == 0) writer.Center("Sin partidas", layout.BaseFontSize);
        writer.Separator();

        writer.OptionalCenter(layout.TipLegend);
        writer.OptionalCenter(layout.TaxLegend);
        writer.Gap(5);
        if (layout.ShowSubtotal && receipt.Subtotal is not null)
            writer.Money("Subtotal", receipt.Subtotal.Value, detail.Currency);
        if (layout.ShowTax && receipt.Tax is not null)
            writer.Money("Impuestos", receipt.Tax.Value, detail.Currency);
        if (layout.ShowTip && receipt.Tip is not null)
            writer.Money("Propina", receipt.Tip.Value, detail.Currency);
        writer.Money(layout.TotalLabel, detail.Total ?? 0m, detail.Currency, bold: true, larger: true);
        if (layout.ShowAmountInWords)
            writer.Center($"*** {AmountInWords(detail.Total ?? 0m, detail.Currency)} ***", layout.BaseFontSize - 1);

        writer.Gap(10);
        if (layout.ShowTipLine) writer.SignatureLine(layout.TipLabel);
        if (layout.ShowRoom) writer.SignatureLine(layout.RoomLabel, receipt.Room ?? context.Room);
        if (layout.ShowGuest) writer.SignatureLine(layout.GuestLabel, receipt.GuestName);
        if (layout.ShowSignatureLine) writer.SignatureLine(layout.SignatureLabel);

        writer.OptionalCenter(receipt.Footer);
        writer.OptionalCenter(layout.FooterText);
        if (!string.IsNullOrWhiteSpace(layout.LegalTextEs) || !string.IsNullOrWhiteSpace(layout.LegalTextEn))
        {
            writer.Separator();
            writer.OptionalWrapped(layout.LegalTextEs);
            writer.Gap(4);
            writer.OptionalWrapped(layout.LegalTextEn);
        }

        return new GeneratedTicket(Encoding.UTF8.GetBytes(writer.Build()), "image/svg+xml");
    }

    private static string AmountInWords(decimal amount, string? currency)
    {
        var absolute = Math.Round(Math.Abs(amount), 2, MidpointRounding.AwayFromZero);
        var whole = decimal.ToInt64(decimal.Truncate(absolute));
        var cents = (int)((absolute - decimal.Truncate(absolute)) * 100m);
        var words = SpanishNumber(whole);
        var sign = amount < 0 ? "MENOS " : string.Empty;
        return $"{sign}{words} {cents:00}/100 {(string.IsNullOrWhiteSpace(currency) ? "M.N." : currency)}".ToUpperInvariant();
    }

    private static string SpanishNumber(long value)
    {
        if (value == 0) return "cero";
        if (value >= 1_000_000_000) return value.ToString(CultureInfo.InvariantCulture);
        var parts = new List<string>();
        if (value >= 1_000_000)
        {
            var millions = value / 1_000_000;
            parts.Add(millions == 1 ? "un millon" : $"{SpanishNumber(millions)} millones");
            value %= 1_000_000;
        }
        if (value >= 1000)
        {
            var thousands = value / 1000;
            parts.Add(thousands == 1 ? "mil" : $"{UnderThousand((int)thousands)} mil");
            value %= 1000;
        }
        if (value > 0) parts.Add(UnderThousand((int)value));
        return string.Join(' ', parts);
    }

    private static string UnderThousand(int value)
    {
        string[] units = ["", "uno", "dos", "tres", "cuatro", "cinco", "seis", "siete", "ocho", "nueve"];
        string[] special = ["diez", "once", "doce", "trece", "catorce", "quince", "dieciseis", "diecisiete", "dieciocho", "diecinueve", "veinte", "veintiuno", "veintidos", "veintitres", "veinticuatro", "veinticinco", "veintiseis", "veintisiete", "veintiocho", "veintinueve"];
        string[] tens = ["", "", "veinte", "treinta", "cuarenta", "cincuenta", "sesenta", "setenta", "ochenta", "noventa"];
        string[] hundreds = ["", "ciento", "doscientos", "trescientos", "cuatrocientos", "quinientos", "seiscientos", "setecientos", "ochocientos", "novecientos"];
        if (value == 100) return "cien";
        var result = new List<string>();
        if (value >= 100) { result.Add(hundreds[value / 100]); value %= 100; }
        if (value is >= 10 and <= 29) { result.Add(special[value - 10]); return string.Join(' ', result); }
        if (value >= 30)
        {
            var ten = tens[value / 10];
            value %= 10;
            result.Add(value == 0 ? ten : $"{ten} y {units[value]}");
            return string.Join(' ', result);
        }
        if (value > 0) result.Add(units[value]);
        return string.Join(' ', result);
    }

    private sealed class ThermalSvgWriter
    {
        private readonly TicketLayout _layout;
        private readonly StringBuilder _content = new();
        private int _y;

        public ThermalSvgWriter(TicketLayout layout)
        {
            _layout = layout;
            _y = layout.Padding + layout.HeaderFontSize;
        }

        public void Center(string value, int size, bool bold = false)
        {
            foreach (var line in Wrap(value, MaxChars(size)))
            {
                Text(_layout.PaperWidth / 2, _y, line, size, "middle", bold);
                _y += Math.Max(_layout.LineHeight, size + 4);
            }
        }

        public void OptionalCenter(string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) Center(value, _layout.BaseFontSize);
        }

        public void OptionalWrapped(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            foreach (var paragraph in value.Replace("\r", string.Empty).Split('\n'))
                foreach (var line in Wrap(paragraph, MaxChars(_layout.BaseFontSize)))
                {
                    Text(_layout.Padding, _y, line, _layout.BaseFontSize);
                    _y += _layout.LineHeight;
                }
        }

        public void OptionalLine(string? value, string prefix)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            Text(_layout.Padding, _y, prefix + value.Trim(), _layout.BaseFontSize);
            _y += _layout.LineHeight;
        }

        public void LeftRight(string left, string? right)
        {
            Text(_layout.Padding, _y, left, _layout.BaseFontSize);
            if (!string.IsNullOrWhiteSpace(right)) Text(_layout.PaperWidth - _layout.Padding, _y, right, _layout.BaseFontSize, "end");
            _y += _layout.LineHeight;
        }

        public void Metadata(CheckReceiptDetails receipt, TicketLayout layout)
        {
            if (layout.ShowServer && !string.IsNullOrWhiteSpace(receipt.Server)) OptionalLine(receipt.Server, "Mesero: ");
            if (layout.ShowTable && !string.IsNullOrWhiteSpace(receipt.Table)) OptionalLine(receipt.Table, "Mesa: ");
            if (layout.ShowGuestCount && !string.IsNullOrWhiteSpace(receipt.GuestCount)) OptionalLine(receipt.GuestCount, "Personas: ");
            if (layout.ShowTurn && !string.IsNullOrWhiteSpace(receipt.Turn)) OptionalLine(receipt.Turn, "Turno: ");
        }

        public void ColumnsHeader(string quantity, string description, string amount)
        {
            Text(_layout.Padding, _y, quantity, _layout.BaseFontSize, bold: true);
            Text(_layout.Padding + 62, _y, description, _layout.BaseFontSize, bold: true);
            Text(_layout.PaperWidth - _layout.Padding, _y, amount, _layout.BaseFontSize, "end", true);
            _y += _layout.LineHeight;
        }

        public void Item(CheckItem item)
        {
            var descriptionWidth = _layout.PaperWidth - _layout.Padding * 2 - 145;
            var chars = Math.Max(8, (int)(descriptionWidth / (_layout.BaseFontSize * 0.62)));
            var lines = Wrap(item.Description, chars).ToArray();
            Text(_layout.Padding, _y, item.Quantity.ToString("0.##", CultureInfo.InvariantCulture), _layout.BaseFontSize);
            Text(_layout.PaperWidth - _layout.Padding, _y, item.Amount.ToString("N2", CultureInfo.InvariantCulture), _layout.BaseFontSize, "end");
            foreach (var line in lines)
            {
                Text(_layout.Padding + 62, _y, line, _layout.BaseFontSize);
                _y += _layout.LineHeight;
            }
        }

        public void Money(string label, decimal amount, string? currency, bool bold = false, bool larger = false)
        {
            var size = larger ? _layout.BaseFontSize + 3 : _layout.BaseFontSize;
            var text = $"{label}: {amount:N2}{(string.IsNullOrWhiteSpace(currency) ? string.Empty : " " + currency)}";
            Text(_layout.PaperWidth - _layout.Padding, _y, text, size, "end", bold);
            _y += Math.Max(_layout.LineHeight, size + 4);
        }

        public void SignatureLine(string label, string? value = null)
        {
            var shown = string.IsNullOrWhiteSpace(value) ? string.Empty : " " + value.Trim();
            Text(_layout.Padding, _y, $"{label}:{shown}", _layout.BaseFontSize);
            var start = Math.Min(_layout.PaperWidth - _layout.Padding - 40, _layout.Padding + Math.Max(115, label.Length * 8));
            _content.Append($"<line x1=\"{start}\" y1=\"{_y + 2}\" x2=\"{_layout.PaperWidth - _layout.Padding}\" y2=\"{_y + 2}\" stroke=\"{Encode(_layout.SeparatorColor)}\"/>");
            _y += _layout.LineHeight + 6;
        }

        public void Separator()
        {
            _y += 5;
            _content.Append($"<line x1=\"{_layout.Padding}\" y1=\"{_y}\" x2=\"{_layout.PaperWidth - _layout.Padding}\" y2=\"{_y}\" stroke=\"{Encode(_layout.SeparatorColor)}\" stroke-dasharray=\"4 3\"/>");
            _y += 12;
        }

        public void Gap(int pixels) => _y += pixels;

        public string Build()
        {
            var height = Math.Max(300, _y + _layout.Padding);
            return $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{_layout.PaperWidth}\" height=\"{height}\" viewBox=\"0 0 {_layout.PaperWidth} {height}\"><rect width=\"100%\" height=\"100%\" fill=\"{Encode(_layout.BackgroundColor)}\"/><g font-family=\"{Encode(_layout.FontFamily)}\" fill=\"{Encode(_layout.TextColor)}\">{_content}</g></svg>";
        }

        private void Text(int x, int y, string value, int size, string anchor = "start", bool bold = false) =>
            _content.Append($"<text x=\"{x}\" y=\"{y}\" font-size=\"{size}\" text-anchor=\"{anchor}\"{(bold ? " font-weight=\"bold\"" : string.Empty)}>{Encode(value)}</text>");

        private int MaxChars(int size) => Math.Max(10, (int)((_layout.PaperWidth - _layout.Padding * 2) / (size * 0.62)));

        private static IEnumerable<string> Wrap(string value, int maximum)
        {
            foreach (var raw in value.Replace("\r", string.Empty).Split('\n'))
            {
                var remaining = raw.Trim();
                if (remaining.Length == 0) { yield return string.Empty; continue; }
                while (remaining.Length > maximum)
                {
                    var split = remaining.LastIndexOf(' ', maximum);
                    if (split < maximum / 3) split = maximum;
                    yield return remaining[..split].Trim();
                    remaining = remaining[split..].TrimStart();
                }
                if (remaining.Length > 0) yield return remaining;
            }
        }

        private static string Encode(string value) => WebUtility.HtmlEncode(value);
    }
}
