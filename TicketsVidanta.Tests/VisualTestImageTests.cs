using Microsoft.Extensions.Logging.Abstractions;
using TicketsVidanta.Features.Tickets.ProcesarCheque;
using TicketsVidanta.Features.VisualTest;
using TicketsVidanta.Shared.Models;
using TicketsVidanta.Shared.TicketGeneration;

namespace TicketsVidanta.Tests;

public sealed class VisualTestImageTests
{
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M/wHwAF/gL+Xw3LAAAAAElFTkSuQmCC");

    [Fact]
    public async Task Store_AssociatesImageWithCheckNumber_IgnoringCaseAndWhitespace()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var store = new FileSystemUploadedTicketImageStore(directory);
            await using var stream = new MemoryStream(Png);

            await store.SaveAsync("  CHK-001 ", stream, "image/png", CancellationToken.None);
            var result = await store.FindAsync("chk-001", CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("image/png", result.ContentType);
            Assert.Equal(Png, result.Content.ToArray());
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task Renderer_UsesAssociatedImageForMatchingCheck()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var store = new FileSystemUploadedTicketImageStore(directory);
            await using var stream = new MemoryStream(Png);
            await store.SaveAsync("CHK-002", stream, "image/png", CancellationToken.None);
            var renderer = new UploadedTicketImageRenderer(
                store, new MockTicketRenderer(), NullLogger<UploadedTicketImageRenderer>.Instance);
            var context = new CheckProcessingContext
            {
                Resort = "TEST",
                ReservationId = "123",
                CheckNumber = "CHK-002",
                SourceSystem = "MOCK",
                CorrelationId = Guid.NewGuid()
            };

            var result = await renderer.RenderAsync(
                context, new CheckDetail([], null, null), CancellationToken.None);

            Assert.Equal("image/png", result.MimeType);
            Assert.Equal(Png, result.Content.ToArray());
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task Store_RejectsFileWhenContentDoesNotMatchImageType()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var store = new FileSystemUploadedTicketImageStore(directory);
            await using var stream = new MemoryStream("not an image"u8.ToArray());

            await Assert.ThrowsAsync<InvalidDataException>(() =>
                store.SaveAsync("CHK-003", stream, "image/png", CancellationToken.None));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "TicketsVidanta.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
