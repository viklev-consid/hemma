using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Modules.Property.Contracts.Authorization;
using Hemma.Modules.Property.Features.Shared;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Property.Features.ClearOccurrenceSnooze;

internal static class ClearOccurrenceSnoozeEndpoint
{
    private const string occurrencesPrefix = $"{PropertyRoutes.Prefix}/maintenance/occurrences";

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete($"{occurrencesPrefix}/{{occurrenceId:guid}}/snooze",
                    async (
                        Guid occurrenceId,
                        Guid householdId,
                        IScopedAuthorizationService<HouseholdScope> authorization,
                        ICurrentUser currentUser,
                        IMessageBus bus,
                        CancellationToken ct) =>
                    {
                        var forbidden = await PropertyEndpointAuthorization.AuthorizeHouseholdAsync(
                            householdId,
                            PropertyPermissions.Write,
                            authorization,
                            currentUser,
                            ct);
                        if (forbidden is not null) { return forbidden; }

                        var result = await bus.InvokeAsync<ErrorOr.ErrorOr<MaintenanceOccurrenceResponse>>(
                            new ClearOccurrenceSnoozeCommand(occurrenceId, householdId),
                            ct);
                        return result.ToProblemDetailsOr(Results.Ok);
                    })
                    .WithName("ClearPropertyMaintenanceOccurrenceSnooze")
                    .WithTags(PropertyRoutes.GroupTag)
                    .Produces<MaintenanceOccurrenceResponse>()
                    .RequireAuthorization();
    }
}
