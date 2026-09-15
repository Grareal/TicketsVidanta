using TicketsVidanta.Features.Tickets.ProcesarCheque;
using TicketsVidanta.Shared.Auditing;
using TicketsVidanta.Shared.Configuration;
using TicketsVidanta.Shared.Database.Commerce;
using TicketsVidanta.Shared.Database.Master;
using TicketsVidanta.Shared.Naming;
using TicketsVidanta.Shared.OperaCloud;
using TicketsVidanta.Shared.Processing;
using TicketsVidanta.Shared.Resolvers;
using TicketsVidanta.Shared.TicketGeneration;
using StatusHandler = TicketsVidanta.Features.Tickets.ObtenerEstado.Handler;
using ProcessHandler = TicketsVidanta.Features.Tickets.ProcesarCheque.Handler;

namespace TicketsVidanta.Shared.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddTicketProcessing(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddOptions<ProcessingOptions>()
            .Bind(configuration.GetSection(ProcessingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<OperaCloudOptions>().Bind(configuration.GetSection(OperaCloudOptions.SectionName));
        services.AddOptions<TicketGenerationOptions>().Bind(configuration.GetSection(TicketGenerationOptions.SectionName));
        services.AddOptions<CommerceDatabaseOptions>().Bind(configuration.GetSection(CommerceDatabaseOptions.SectionName));

        services.AddSingleton<IProcessingRegistry, InMemoryProcessingRegistry>();
        services.AddSingleton<IAuditService, InMemoryAuditService>();
        if (environment.IsDevelopment())
            services.AddSingleton<IMasterTransactionRepository, MockMasterTransactionRepository>();
        else
            services.AddSingleton<IMasterTransactionRepository, SqlMasterTransactionRepository>();
        services.AddSingleton<ICommerceConnectionCatalog, OptionsCommerceConnectionCatalog>();
        services.AddSingleton<ITicketFileNameGenerator, TicketFileNameGenerator>();
        services.AddTicketResolvers(environment);
        services.AddOperaCloud(environment);
        services.AddTicketGeneration(environment);

        services.AddScoped<Validator>();
        services.AddScoped<ITicketProcessor, ProcessHandler>();
        services.AddScoped<StatusHandler>();
        services.AddHostedService<TicketProcessingBackgroundService>();
        return services;
    }

    public static IServiceCollection AddTicketResolvers(this IServiceCollection services, IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
            services.AddSingleton<ICheckResolver, MockCheckResolver>();
        services.AddSingleton<ICheckResolverSelector, CheckResolverSelector>();
        return services;
    }

    public static IServiceCollection AddOperaCloud(this IServiceCollection services, IHostEnvironment environment)
    {
        services.AddHttpClient<OperaCloudClient>();
        if (environment.IsDevelopment())
            services.AddSingleton<IOperaCloudClient, MockOperaCloudClient>();
        else
            services.AddScoped<IOperaCloudClient>(provider => provider.GetRequiredService<OperaCloudClient>());
        return services;
    }

    public static IServiceCollection AddTicketGeneration(this IServiceCollection services, IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
            services.AddSingleton<ITicketRenderer, MockTicketRenderer>();
        else
            services.AddSingleton<ITicketRenderer, PendingTicketRenderer>();
        return services;
    }
}
