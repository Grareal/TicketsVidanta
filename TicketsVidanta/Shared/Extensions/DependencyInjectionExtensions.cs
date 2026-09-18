using TicketsVidanta.Features.Tickets.ProcesarCheque;
using TicketsVidanta.Features.VisualTest;
using TicketsVidanta.Shared.Auditing;
using TicketsVidanta.Shared.Configuration;
using TicketsVidanta.Shared.Database;
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
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<OperaCloudOptions>()
            .Bind(configuration.GetSection(OperaCloudOptions.SectionName))
            .Validate(IsValidOperaConfiguration,
                "La configuración OHIP está incompleta para el modo real.")
            .ValidateOnStart();
        services.AddOptions<TicketGenerationOptions>().Bind(configuration.GetSection(TicketGenerationOptions.SectionName));
        services.AddOptions<CommerceDatabaseOptions>().Bind(configuration.GetSection(CommerceDatabaseOptions.SectionName));

        var useSqlPersistence = configuration.GetValue<bool>($"{DatabaseOptions.SectionName}:UseSqlPersistence");
        if (useSqlPersistence)
        {
            services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
            services.AddSingleton<IProcessingRegistry, SqlProcessingRegistry>();
            services.AddSingleton<IAuditService, SqlAuditService>();
            services.AddSingleton<IMasterTransactionRepository, SqlMasterTransactionRepository>();
            services.AddHealthChecks().AddCheck<SqlServerHealthCheck>("sql-server", tags: ["ready"]);
        }
        else
        {
            services.AddSingleton<IProcessingRegistry, InMemoryProcessingRegistry>();
            services.AddSingleton<IAuditService, InMemoryAuditService>();
            services.AddSingleton<IMasterTransactionRepository, MockMasterTransactionRepository>();
        }
        services.AddSingleton<ICommerceConnectionCatalog, OptionsCommerceConnectionCatalog>();
        services.AddSingleton<ITicketFileNameGenerator, TicketFileNameGenerator>();
        services.AddTicketResolvers(environment, useSqlPersistence);
        services.AddOperaCloud(configuration);
        services.AddTicketGeneration(configuration);

        services.AddScoped<Validator>();
        services.AddScoped<ITicketProcessor, ProcessHandler>();
        services.AddScoped<StatusHandler>();
        services.AddHostedService<TicketProcessingBackgroundService>();
        return services;
    }

    public static IServiceCollection AddTicketResolvers(
        this IServiceCollection services,
        IHostEnvironment environment,
        bool useSqlPersistence)
    {
        if (environment.IsDevelopment())
            services.AddSingleton<ICheckResolver, MockCheckResolver>();
        if (useSqlPersistence)
            services.AddSingleton<ICheckResolver, SqlCheckResolver>();
        services.AddSingleton<ICheckResolverSelector, CheckResolverSelector>();
        return services;
    }

    public static IServiceCollection AddOperaCloud(this IServiceCollection services, IConfiguration configuration)
    {
        var useMock = configuration.GetValue<bool>($"{OperaCloudOptions.SectionName}:UseMock");
        if (useMock)
            services.AddSingleton<IOperaCloudClient, MockOperaCloudClient>();
        else
        {
            var timeout = configuration.GetValue<int?>($"{OperaCloudOptions.SectionName}:TimeoutSeconds") ?? 30;
            services.AddHttpClient("OperaCloudAuth", client => client.Timeout = TimeSpan.FromSeconds(timeout));
            services.AddSingleton<IOperaCloudTokenProvider, OperaCloudTokenProvider>();
            services.AddHttpClient<IOperaCloudClient, OperaCloudClient>(client =>
                client.Timeout = TimeSpan.FromSeconds(timeout));
        }
        return services;
    }

    public static IServiceCollection AddTicketGeneration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IUploadedTicketImageStore, FileSystemUploadedTicketImageStore>();
        if (configuration.GetValue<bool>($"{TicketGenerationOptions.SectionName}:UseMock"))
        {
            services.AddSingleton<MockTicketRenderer>();
            services.AddSingleton<ITicketRenderer, UploadedTicketImageRenderer>();
        }
        else
            services.AddSingleton<ITicketRenderer, PendingTicketRenderer>();
        return services;
    }

    private static bool IsValidOperaConfiguration(OperaCloudOptions options)
    {
        if (options.UseMock) return true;
        var common = !string.IsNullOrWhiteSpace(options.GatewayUrl)
            && !string.IsNullOrWhiteSpace(options.AppKey)
            && !string.IsNullOrWhiteSpace(options.ClientId)
            && !string.IsNullOrWhiteSpace(options.ClientSecret)
            && !string.IsNullOrWhiteSpace(options.HotelId)
            && options.TimeoutSeconds is >= 1 and <= 300;
        if (!common) return false;

        return string.Equals(options.GrantType, "password", StringComparison.OrdinalIgnoreCase)
            ? !string.IsNullOrWhiteSpace(options.Username) && !string.IsNullOrWhiteSpace(options.Password)
            : string.Equals(options.GrantType, "client_credentials", StringComparison.OrdinalIgnoreCase)
              && !string.IsNullOrWhiteSpace(options.EnterpriseId)
              && !string.IsNullOrWhiteSpace(options.Scope);
    }
}
