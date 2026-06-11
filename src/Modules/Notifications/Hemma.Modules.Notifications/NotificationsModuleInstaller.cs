using Hemma.Shared.Infrastructure.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using TickerQ.Utilities;
using TickerQ.Utilities.Entities;
using Wolverine;

namespace Hemma.Modules.Notifications;

public sealed class NotificationsModuleInstaller : IModuleInstaller
{
    public string Name => "Notifications";

    public void Install(WebApplicationBuilder builder)
    {
        builder.Services.AddNotificationsModule(builder.Configuration);
    }

    public void ConfigureMessaging(WolverineOptions options)
    {
        options.AddNotificationsHandlers();
    }

    public void ConfigureJobs(TickerOptionsBuilder<TimeTickerEntity, CronTickerEntity> options)
    {
        options.AddNotificationsJobs();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapNotificationsEndpoints();
    }
}
