using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Hemma.Shared.Infrastructure.Modules;
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

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapEconomyEndpoints();
    }
}
