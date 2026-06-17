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

namespace Hemma.Modules.Property.Features.DeleteHistoryEntry;

internal static class DeleteHistoryEntryEndpoint
{

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete($"{PropertyRoutes.Prefix}/history/{{historyEntryId:guid}}",
                    async (
                        Guid historyEntryId,
                        Guid householdId,
                        IScopedAuthorizationService<HouseholdScope> authorization,
                        ICurrentUser currentUser,
                        IMessageBus bus,
                        CancellationToken ct) =>
                    {
                        var forbidden = await AuthorizeAsync(householdId, PropertyPermissions.Write, authorization, currentUser, ct);
                        if (forbidden is not null) { return forbidden; }

                        var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ErrorOr.Deleted>>(
                            new DeleteHistoryEntryCommand(historyEntryId, householdId),
                            ct);
                        return result.ToProblemDetailsOr(_ => Results.NoContent());
                    })
                    .WithName("DeletePropertyHistoryEntry")
                    .WithTags(PropertyRoutes.GroupTag)
                    .Produces(StatusCodes.Status204NoContent)
                    .RequireAuthorization();
    }

    private static Task<IResult?> AuthorizeAsync(
        Guid householdId,
        string permission,
        IScopedAuthorizationService<HouseholdScope> authorization,
        ICurrentUser currentUser,
        CancellationToken ct) =>
        PropertyEndpointAuthorization.AuthorizeHouseholdAsync(householdId, permission, authorization, currentUser, ct);
}
