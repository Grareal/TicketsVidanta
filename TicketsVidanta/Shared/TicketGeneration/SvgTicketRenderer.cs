using System.Globalization;
using System.Net;
using System.Text;
using TicketsVidanta.Features.Tickets.ProcesarCheque;
using TicketsVidanta.Shared.Models;

namespace TicketsVidanta.Shared.TicketGeneration;

public sealed class SvgTicketRenderer : ITicketRenderer
{
    public Task<GeneratedTicket> RenderAsync(
        CheckProcessingContext context,
        CheckDetail detail,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const int width = 760;
        var summaryLines = 1 + (detail.Receipt?.Subtotal is null ? 0 : 1)
            + (detail.Receipt?.Tip is null ? 0 : 1)
            + (detail.Receipt?.Tax is null ? 0 : 1);
        var height = 360 + detail.Items.Count * 42 + summaryLines * 30 + 70;
        var receipt = detail.Receipt;
        var culture = CultureInfo.GetCultureInfo("es-MX");
        var builder = new StringBuilder();
        builder.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\">")
            .Append("<rect width=\"100%\" height=\"100%\" fill=\"white\"/><g font-family=\"Arial,sans-serif\" fill=\"#111\">")
            .Append(Text(380, 48, receipt?.Header ?? "VIDANTA", 28, "middle", true))
            .Append(Text(380, 82, receipt?.PointOfSale ?? context.SourceSystem, 20, "middle"))
            .Append(Text(40, 128, $"Cheque: {receipt?.CheckNumber ?? context.CheckNumber}", 18))
            .Append(Text(420, 128, $"Habitación: {receipt?.Room ?? context.Room ?? "-"}", 18));
        if (!string.IsNullOrWhiteSpace(receipt?.GuestName)) builder.Append(Text(40, 162, $"Huésped: {receipt.GuestName}", 18));
        var date = receipt?.BusinessDate?.ToString("dd/MM/yyyy", culture) ?? "-";
        var time = receipt?.Time?.ToString(@"hh\:mm") ?? "-";
        builder.Append(Text(40, 196, $"Fecha: {date}  Hora: {time}", 18))
            .Append("<line x1=\"40\" y1=\"220\" x2=\"720\" y2=\"220\" stroke=\"#333\"/>")
            .Append(Text(40, 252, "Descripción", 17, bold: true))
            .Append(Text(520, 252, "Cant.", 17, "end", true))
            .Append(Text(710, 252, "Importe", 17, "end", true));

        var y = 292;
        foreach (var item in detail.Items)
        {
            builder.Append(Text(40, y, item.Description, 17))
                .Append(Text(520, y, item.Quantity.ToString("0.##", culture), 17, "end"))
                .Append(Text(710, y, item.Amount.ToString("C", culture), 17, "end"));
            y += 42;
        }

        builder.Append($"<line x1=\"400\" y1=\"{y}\" x2=\"720\" y2=\"{y}\" stroke=\"#333\"/>");
        y += 32;
        if (receipt?.Subtotal is not null) { builder.Append(Text(710, y, $"Subtotal: {receipt.Subtotal.Value.ToString("C", culture)}", 17, "end")); y += 30; }
        if (receipt?.Tip is not null) { builder.Append(Text(710, y, $"Propina: {receipt.Tip.Value.ToString("C", culture)}", 17, "end")); y += 30; }
        if (receipt?.Tax is not null) { builder.Append(Text(710, y, $"IVA: {receipt.Tax.Value.ToString("C", culture)}", 17, "end")); y += 30; }
        builder.Append(Text(710, y, $"TOTAL: {(detail.Total ?? 0m).ToString("C", culture)} {detail.Currency}", 21, "end", true));
        if (!string.IsNullOrWhiteSpace(receipt?.Footer)) builder.Append(Text(380, height - 28, receipt.Footer, 15, "middle"));
        builder.Append("</g></svg>");
        return Task.FromResult(new GeneratedTicket(Encoding.UTF8.GetBytes(builder.ToString()), "image/svg+xml"));
    }

    private static string Text(int x, int y, string value, int size, string anchor = "start", bool bold = false) =>
        $"<text x=\"{x}\" y=\"{y}\" font-size=\"{size}\" text-anchor=\"{anchor}\"{(bold ? " font-weight=\"bold\"" : string.Empty)}>{WebUtility.HtmlEncode(value)}</text>";
}
