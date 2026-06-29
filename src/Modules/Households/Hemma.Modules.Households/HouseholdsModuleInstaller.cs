using Hemma.Shared.Infrastructure.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Households;

public sealed class HouseholdsModuleInstaller : IModuleInstaller
{
    public string Name => "Households";

    public void Install(WebApplicationBuilder builder)
    {
        builder.Services.AddHouseholdsModule(builder.Configuration, builder.Environment);
    }

    public void ConfigureMessaging(WolverineOptions options)
    {
        options.AddHouseholdsHandlers();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHouseholdsEndpoints();
    }
}
