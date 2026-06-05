using Hemma.Shared.Infrastructure.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using TickerQ.Utilities;
using TickerQ.Utilities.Entities;
using Wolverine;

namespace Hemma.Modules.Economy;

public sealed class EconomyModuleInstaller : IModuleInstaller
{
    public string Name => "Economy";

    public void Install(WebApplicationBuilder builder)
    {
        builder.Services.AddEconomyModule(builder.Configuration, builder.Environment);
    }

    public void ConfigureMessaging(WolverineOptions options)
    {
        options.AddEconomyHandlers();
    }

    public void ConfigureJobs(TickerOptionsBuilder<TimeTickerEntity, CronTickerEntity> options)
    {
        options.AddEconomyJobs();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapEconomyEndpoints();
    }
}
