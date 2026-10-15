using Microsoft.Data.SqlClient;
using TicketsVidanta.Features.Tickets.ProcesarCheque;
using TicketsVidanta.Shared.Database;
using TicketsVidanta.Shared.Models;

namespace TicketsVidanta.Features.ControlledExecution;

internal sealed class ControlledExecutionService(
    ISqlConnectionFactory connectionFactory,
    ITicketProcessor processor,
    ILogger<ControlledExecutionService> logger) : IControlledExecutionService
{
    public async Task<ControlledBatchResult> ExecuteAsync(ControlledBatchRequest request, CancellationToken cancellationToken)
    {
        var candidates = new List<Request>();

        // Si se pide un ticket puntual con reservación directa
        if (!string.IsNullOrWhiteSpace(request.CheckNumber) && !string.IsNullOrWhiteSpace(request.ReservationId))
        {
            candidates.Add(new Request(
                request.Resort ?? "VINV",
                request.ReservationId.Trim(),
                request.CheckNumber.Trim(),
                null,
                null,
                request.SourceSystem,
                Mode: request.Mode));
        }
        else
        {
            // Consultar candidatos desde TicketsVidanta local
            candidates = await QueryCandidatesFromLocalDbAsync(request, cancellationToken);
        }

        var results = new List<Response>();
        var successCount = 0;
        var failedCount = 0;

        foreach (var item in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var response = await processor.ProcessAsync(item with { Mode = request.Mode }, cancellationToken);
                results.Add(response);
                if (response.Succeeded || response.AlreadyProcessed)
                    successCount++;
                else
                    failedCount++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falla en ejecución controlada para Check {CheckNumber}", item.CheckNumber);
                results.Add(new Response(false, Guid.NewGuid(), ProcessingStatus.Failed, ex.Message));
                failedCount++;
            }
        }

        return new ControlledBatchResult(candidates.Count, successCount, failedCount, results);
    }

    public async Task<ReprocessResult> ReprocessAsync(ReprocessRequest request, CancellationToken cancellationToken)
    {
        var predicates = new List<string> { "Status IN ('Completed', 'Failed', 'Processing')" };
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand { Connection = connection };

        if (!string.IsNullOrWhiteSpace(request.CheckNumber))
        {
            predicates.Add("CheckNumber = @CheckNumber");
            command.Parameters.AddWithValue("@CheckNumber", request.CheckNumber.Trim());
        }
        if (!string.IsNullOrWhiteSpace(request.ReservationId))
        {
            predicates.Add("ReservationId = @ReservationId");
            command.Parameters.AddWithValue("@ReservationId", request.ReservationId.Trim());
        }
        if (!string.IsNullOrWhiteSpace(request.Resort))
        {
            predicates.Add("Resort = @Resort");
            command.Parameters.AddWithValue("@Resort", request.Resort.Trim());
        }
        if (!string.IsNullOrWhiteSpace(request.SourceSystem))
        {
            predicates.Add("SourceSystem = @SourceSystem");
            command.Parameters.AddWithValue("@SourceSystem", request.SourceSystem.Trim());
        }

        command.CommandText = $"""
            UPDATE dbo.MasterTransactions
               SET Status = 'Pending',
                   AttemptCount = 0,
                   LeaseUntilUtc = NULL,
                   LastError = 'Reprocesamiento solicitado el ' + CONVERT(nvarchar(30), SYSUTCDATETIME(), 120),
                   UpdatedAtUtc = SYSUTCDATETIME()
             WHERE {string.Join(" AND ", predicates)};
            """;

        var affected = await command.ExecuteNonQueryAsync(cancellationToken);

        // También reiniciar el estado en ProcessingRecords si se reprocesa por CheckNumber puntual
        if (!string.IsNullOrWhiteSpace(request.CheckNumber))
        {
            const string deleteProcSql = "DELETE FROM dbo.ProcessingRecords WHERE CheckNumber = @CheckNumber;";
            await using var delCmd = new SqlCommand(deleteProcSql, connection);
            delCmd.Parameters.AddWithValue("@CheckNumber", request.CheckNumber.Trim());
            await delCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        return new ReprocessResult(affected, $"Se re-encolaron exitosamente {affected} transacción(es) para reprocesamiento.");
    }

    private async Task<List<Request>> QueryCandidatesFromLocalDbAsync(ControlledBatchRequest request, CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(request.Top ?? 10, 1, 1000);
        var filters = new List<string>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand { Connection = connection };

        if (!string.IsNullOrWhiteSpace(request.CheckNumber))
        {
            filters.Add("CheckNumber = @CheckNumber");
            command.Parameters.AddWithValue("@CheckNumber", request.CheckNumber.Trim());
        }
        if (!string.IsNullOrWhiteSpace(request.Resort))
        {
            filters.Add("Resort = @Resort");
            command.Parameters.AddWithValue("@Resort", request.Resort.Trim());
        }
        if (!string.IsNullOrWhiteSpace(request.SourceSystem))
        {
            filters.Add("SourceSystem = @SourceSystem");
            command.Parameters.AddWithValue("@SourceSystem", request.SourceSystem.Trim());
        }

        var filterSql = filters.Count > 0 ? "WHERE " + string.Join(" AND ", filters) : "";
        command.CommandText = $"""
            SELECT TOP ({limit}) Resort, ReservationId, CheckNumber, Room, Reference, SourceSystem, TcGroup, TrxCode
            FROM dbo.MasterTransactions
            {filterSql}
            ORDER BY Id DESC;
            """;

        var candidates = new List<Request>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            candidates.Add(new Request(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetString(7),
                Mode: request.Mode));
        }

        // Si no hay transacciones en MasterTransactions pero se buscó por CheckNumber, buscar en stg.hotche
        if (candidates.Count == 0 && !string.IsNullOrWhiteSpace(request.CheckNumber))
        {
            reader.Close();
            const string stgSql = """
                SELECT TOP (1) Resort, cargar_a, folio, num_hab
                FROM stg.hotche
                WHERE folio = @Folio;
                """;
            await using var stgCmd = new SqlCommand(stgSql, connection);
            stgCmd.Parameters.AddWithValue("@Folio", request.CheckNumber.Trim());
            await using var stgReader = await stgCmd.ExecuteReaderAsync(cancellationToken);
            if (await stgReader.ReadAsync(cancellationToken))
            {
                candidates.Add(new Request(
                    stgReader.GetString(0),
                    stgReader.GetString(1),
                    stgReader.GetString(2),
                    stgReader.IsDBNull(3) ? null : stgReader.GetString(3),
                    null,
                    request.SourceSystem ?? "INSSIST_AYB",
                    Mode: request.Mode));
            }
        }

        return candidates;
    }
}

