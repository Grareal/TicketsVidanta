using System.Text.Json.Serialization;
using TicketsVidanta.Features.Tickets.ObtenerEstado;
using TicketsVidanta.Features.Tickets.ProcesarCheque;
using TicketsVidanta.Features.VisualTest;
using TicketsVidanta.Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));


Console.WriteLine("CONFIG TEST");

Console.WriteLine(builder.Configuration["OperaCloud:GatewayUrl"]);

Console.WriteLine(builder.Configuration["OperaCloud:ClientId"]);

Console.WriteLine(builder.Configuration["OperaCloud:HotelId"]);    
builder.Services.AddTicketProcessing(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.UseStaticFiles();
    app.MapOpenApi();
    app.MapVisualTestEndpoints();
}
app.MapHealthChecks("/health");
// TODO [DISCOVERY]:
// Agregar readiness checks para tabla maestra, bases de comercios, Opera Cloud y
// secret provider cuando existan contratos y conectividad no productiva confirmados.
app.MapProcesarChequeEndpoint();
app.MapObtenerEstadoEndpoint();

app.Run();

public partial class Program;
