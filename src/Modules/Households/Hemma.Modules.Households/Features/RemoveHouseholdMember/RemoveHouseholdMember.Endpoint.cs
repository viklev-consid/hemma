using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Hemma.Modules.Households.Authorization;
using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Modules.Households.Errors;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Wolverine;

namespace Hemma.Modules.Households.Features.RemoveHouseholdMember;

internal static class RemoveHouseholdMemberEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(HouseholdsRoutes.MemberByUserId,
            async (
                string householdRef,
                Guid userId,
                IHouseholdRefResolver resolver,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                if (currentUser.Id is null || !Guid.TryParse(currentUser.Id, out var removedByUserId))
                {
                    return Results.Unauthorized();
                }

                var household = await resolver.ResolveAsync(householdRef, ct);
                if (household.IsError)
                {
                    return household.ToProblemDetailsOr(_ => Results.Empty);
                }

                var permission = GetRequiredPermission(userId, removedByUserId);
                var access = await authorization.AuthorizeAsync(currentUser, household.Value, permission, ScopedAuthorizationOptions.WithPlatformOverride, ct);
                if (!access.Succeeded)
                {
                    return Results.Forbid();
                }
                if (access.AccessMode == ScopedAuthorizationAccessMode.PlatformOverride)
                {
                    return Results.Problem(title: "Forbidden", detail: HouseholdsErrors.PlatformOverrideMutationForbidden.Description, statusCode: StatusCodes.Status403Forbidden);
                }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ErrorOr.Success>>(
                    new RemoveHouseholdMemberCommand(household.Value.Id, userId, removedByUserId),
                    ct);
                return result.ToProblemDetailsOr(_ => Results.NoContent());
            })
        .WithName("RemoveHouseholdMember")
        .WithSummary("Remove a member or leave an household.")
        .Produces(StatusCodes.Status204NoContent)
        .RequireAuthorization();

    private static string GetRequiredPermission(Guid targetUserId, Guid actorUserId) =>
        targetUserId == actorUserId
            ? HouseholdsPermissions.HouseholdsRead
            : HouseholdsPermissions.MembersManage;
}
