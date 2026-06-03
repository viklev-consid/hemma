using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Exceptions;

namespace Hemma.Shared.Infrastructure.Logging;

public static class SerilogExtensions
{
    public static IHostBuilder UseHemmaSerilog(this IHostBuilder builder)
    {
        return builder.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithExceptionDetails()
                .Destructure.With<PersonalDataDestructuringPolicy>();
        });
    }

    public static LoggerConfiguration AddHemmaDefaults(
        this LoggerConfiguration loggerConfiguration)
    {
        return loggerConfiguration
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithExceptionDetails()
            .Destructure.With<PersonalDataDestructuringPolicy>();
    }
}
