using Hemma.Shared.Infrastructure.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Property;

public sealed class PropertyModuleInstaller : IModuleInstaller
{
    public string Name => "Property";

    public void Install(WebApplicationBuilder builder)
    {
        builder.Services.AddPropertyModule(builder.Configuration, builder.Environment);
    }

    public void ConfigureMessaging(WolverineOptions options)
    {
        options.AddPropertyHandlers();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPropertyEndpoints();
    }
}
