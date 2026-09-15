namespace TicketsVidanta.Features.Tickets.ProcesarCheque;

public sealed class Validator
{
    public IReadOnlyDictionary<string, string[]> Validate(Request request)
    {
        var errors = new Dictionary<string, string[]>();
        AddRequired(errors, nameof(request.Resort), request.Resort);
        AddRequired(errors, nameof(request.ReservationId), request.ReservationId);
        AddRequired(errors, nameof(request.CheckNumber), request.CheckNumber);

        if (string.IsNullOrWhiteSpace(request.SourceSystem) && string.IsNullOrWhiteSpace(request.Reference))
            errors[nameof(request.SourceSystem)] = ["SourceSystem o Reference es requerido para seleccionar un resolver."];

        return errors;
    }

    private static void AddRequired(Dictionary<string, string[]> errors, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) errors[field] = ["El campo es requerido."];
    }
}
