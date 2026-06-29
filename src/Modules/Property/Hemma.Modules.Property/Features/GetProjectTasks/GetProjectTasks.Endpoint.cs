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

namespace Hemma.Modules.Property.Features.GetProjectTasks;

internal static class GetProjectTasksEndpoint
{

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet($"{PropertyRoutes.Prefix}/projects/{{projectId:guid}}/tasks",
                    async (Guid projectId, Guid householdId, bool? isOverdue, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
                    {
                        var forbidden = await AuthorizeAsync(householdId, PropertyPermissions.Read, authorization, currentUser, ct);
                        if (forbidden is not null) { return forbidden; }

                        var result = await bus.InvokeAsync<ErrorOr.ErrorOr<GetProjectTasksResponse>>(new GetProjectTasksQuery(projectId, householdId, isOverdue), ct);
                        return result.ToProblemDetailsOr(Results.Ok);
                    })
                    .WithName("GetPropertyProjectTasks")
                    .WithTags(PropertyRoutes.GroupTag)
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
