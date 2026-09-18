using System.Security.Cryptography;
using System.Text;

namespace TicketsVidanta.Features.VisualTest;

public sealed class FileSystemUploadedTicketImageStore : IUploadedTicketImageStore
{
    private const int MaximumImageSize = 10 * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, string> Extensions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/png"] = ".png",
            ["image/jpeg"] = ".jpg",
            ["image/webp"] = ".webp"
        };

    private readonly string _storagePath;

    public FileSystemUploadedTicketImageStore(IHostEnvironment environment)
        : this(Path.Combine(environment.ContentRootPath, "App_Data", "visual-test-images"))
    {
    }

    public FileSystemUploadedTicketImageStore(string storagePath)
    {
        _storagePath = storagePath;
    }

    public async Task SaveAsync(
        string checkNumber,
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        ValidateCheckNumber(checkNumber);
        if (!Extensions.TryGetValue(contentType, out var extension))
            throw new InvalidDataException("El archivo debe ser PNG, JPEG o WebP.");

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length is 0 or > MaximumImageSize)
            throw new InvalidDataException("La imagen debe pesar entre 1 byte y 10 MB.");

        var bytes = buffer.ToArray();
        if (!HasExpectedSignature(bytes, contentType.ToLowerInvariant()))
            throw new InvalidDataException("El contenido del archivo no coincide con el formato indicado.");

        Directory.CreateDirectory(_storagePath);
        var key = CreateKey(checkNumber);
        var destination = Path.Combine(_storagePath, key + extension);
        var temporary = Path.Combine(_storagePath, $"{key}.{Guid.NewGuid():N}.tmp");

        try
        {
            await File.WriteAllBytesAsync(temporary, bytes, cancellationToken);
            File.Move(temporary, destination, true);

            foreach (var otherExtension in Extensions.Values.Where(value => value != extension))
            {
                var previous = Path.Combine(_storagePath, key + otherExtension);
                if (File.Exists(previous)) File.Delete(previous);
            }
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public async Task<UploadedTicketImage?> FindAsync(string checkNumber, CancellationToken cancellationToken)
    {
        ValidateCheckNumber(checkNumber);
        var key = CreateKey(checkNumber);

        foreach (var format in Extensions)
        {
            var path = Path.Combine(_storagePath, key + format.Value);
            if (!File.Exists(path)) continue;

            var content = await File.ReadAllBytesAsync(path, cancellationToken);
            return new UploadedTicketImage(content, format.Key);
        }

        return null;
    }

    private static void ValidateCheckNumber(string checkNumber)
    {
        if (string.IsNullOrWhiteSpace(checkNumber) || checkNumber.Trim().Length > 80)
            throw new ArgumentException("El nombre o número de cheque es requerido y admite hasta 80 caracteres.", nameof(checkNumber));
    }

    private static string CreateKey(string checkNumber)
    {
        var normalized = checkNumber.Trim().ToUpperInvariant();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }

    private static bool HasExpectedSignature(byte[] content, string contentType) => contentType switch
    {
        "image/png" => content.Length >= 8 && content.AsSpan(0, 8).SequenceEqual(
            new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }),
        "image/jpeg" => content.Length >= 3 && content[0] == 0xff && content[1] == 0xd8 && content[2] == 0xff,
        "image/webp" => content.Length >= 12
            && Encoding.ASCII.GetString(content, 0, 4) == "RIFF"
            && Encoding.ASCII.GetString(content, 8, 4) == "WEBP",
        _ => false
    };
}
