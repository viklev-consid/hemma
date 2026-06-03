using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Hemma.Shared.Infrastructure.Modules;
using Wolverine;

namespace Hemma.Modules.Catalog;

public sealed class CatalogModuleInstaller : IModuleInstaller
{
    public string Name => "Catalog";

    public void Install(WebApplicationBuilder builder)
    {
        builder.Services.AddCatalogModule(builder.Configuration, builder.Environment);
    }

    public void ConfigureMessaging(WolverineOptions options)
    {
        options.AddCatalogHandlers();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapCatalogEndpoints();
    }
}
