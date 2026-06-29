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

namespace Hemma.Modules.Property.Features.GetProjectBudget;

internal static class GetProjectBudgetEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet($"{PropertyRoutes.Prefix}/projects/{{projectId:guid}}/budget",
            async (Guid projectId, Guid householdId, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                var forbidden = await PropertyEndpointAuthorization.AuthorizeHouseholdAsync(householdId, PropertyPermissions.Read, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<GetProjectBudgetResponse>>(new GetProjectBudgetQuery(projectId, householdId), ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("GetPropertyProjectBudget")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<GetProjectBudgetResponse>()
            .RequireAuthorization();
}
