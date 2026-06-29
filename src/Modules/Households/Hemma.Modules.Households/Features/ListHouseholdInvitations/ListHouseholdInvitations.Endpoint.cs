using Hemma.Modules.Households.Authorization;
using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Households.Features.ListHouseholdInvitations;

internal static class ListHouseholdInvitationsEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(HouseholdsRoutes.Invitations,
            async (
                string householdRef,
                IHouseholdRefResolver resolver,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct,
                int page = 1,
                int pageSize = 20) =>
            {
                var household = await resolver.ResolveAsync(householdRef, ct);
                if (household.IsError)
                {
                    return household.ToProblemDetailsOr(_ => Results.Empty);
                }

                var access = await authorization.AuthorizeAsync(currentUser, household.Value, HouseholdsPermissions.InvitationsManage, ScopedAuthorizationOptions.WithPlatformOverride, ct);
                if (!access.Succeeded)
                {
                    return Results.Forbid();
                }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ListHouseholdInvitationsResponse>>(
                    new ListHouseholdInvitationsQuery(household.Value.Id, page, pageSize),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("ListHouseholdInvitations")
        .WithSummary("List household invitations.")
        .Produces<ListHouseholdInvitationsResponse>()
        .RequireAuthorization();
}
