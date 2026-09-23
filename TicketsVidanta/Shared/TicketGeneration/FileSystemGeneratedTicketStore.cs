using Microsoft.Extensions.Options;
using TicketsVidanta.Shared.Configuration;

namespace TicketsVidanta.Shared.TicketGeneration;

public sealed class FileSystemGeneratedTicketStore : IGeneratedTicketStore
{
    private readonly string _directory;

    public FileSystemGeneratedTicketStore(IHostEnvironment environment, IOptions<TicketGenerationOptions> options)
    {
        var configured = options.Value.OutputDirectory;
        _directory = Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(environment.ContentRootPath, configured);
    }

    public async Task SaveAsync(string fileName, GeneratedTicket ticket, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_directory);
        var safeName = Path.GetFileName(fileName);
        var destination = Path.GetFullPath(Path.Combine(_directory, safeName));
        var root = Path.GetFullPath(_directory) + Path.DirectorySeparatorChar;
        if (!destination.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("El nombre del ticket no es seguro.");
        await File.WriteAllBytesAsync(destination, ticket.Content.ToArray(), cancellationToken);
    }
}
