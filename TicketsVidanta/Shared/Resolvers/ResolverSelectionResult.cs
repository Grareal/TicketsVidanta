namespace TicketsVidanta.Shared.Resolvers;

public sealed record ResolverSelectionResult(ICheckResolver? Resolver, string? Error)
{
    public bool IsSuccess => Resolver is not null;
    public static ResolverSelectionResult Success(ICheckResolver resolver) => new(resolver, null);
    public static ResolverSelectionResult NotFound(string error) => new(null, error);
}
