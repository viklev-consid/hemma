using FluentValidation;
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

namespace Hemma.Modules.Households.Features.ChangeHouseholdMemberRole;

internal static class ChangeHouseholdMemberRoleEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPut(HouseholdsRoutes.MemberRole,
            async (
                string householdRef,
                Guid userId,
                ChangeHouseholdMemberRoleRequest request,
                IValidator<ChangeHouseholdMemberRoleRequest> validator,
                IHouseholdRefResolver resolver,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                if (currentUser.Id is null || !Guid.TryParse(currentUser.Id, out var changedByUserId))
                {
                    return Results.Unauthorized();
                }

                var validation = await validator.ValidateAsync(request, ct);
                if (!validation.IsValid)
                {
                    return Results.ValidationProblem(validation.ToDictionary(), statusCode: StatusCodes.Status422UnprocessableEntity);
                }

                var household = await resolver.ResolveAsync(householdRef, ct);
                if (household.IsError)
                {
                    return household.ToProblemDetailsOr(_ => Results.Empty);
                }

                var access = await authorization.AuthorizeAsync(currentUser, household.Value, HouseholdsPermissions.MembersManage, ScopedAuthorizationOptions.WithPlatformOverride, ct);
                if (!access.Succeeded)
                {
                    return Results.Forbid();
                }
                if (access.AccessMode == ScopedAuthorizationAccessMode.PlatformOverride)
                {
                    return Results.Problem(title: "Forbidden", detail: HouseholdsErrors.PlatformOverrideMutationForbidden.Description, statusCode: StatusCodes.Status403Forbidden);
                }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ChangeHouseholdMemberRoleResponse>>(
                    new ChangeHouseholdMemberRoleCommand(household.Value.Id, userId, request.Role, changedByUserId),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("ChangeHouseholdMemberRole")
        .WithSummary("Change an household member role.")
        .Produces<ChangeHouseholdMemberRoleResponse>()
        .RequireAuthorization();
}
