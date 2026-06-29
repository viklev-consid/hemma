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

namespace Hemma.Modules.Property.Features.ListIssues;

internal static class ListIssuesEndpoint
{
    private const string issuesPrefix = $"{PropertyRoutes.Prefix}/issues";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(issuesPrefix,
            async (Guid householdId, string? status, Guid? areaId, string? severity, [Microsoft.AspNetCore.Mvc.FromQuery] Guid[]? tagIds, bool? isOverdue, Guid? linkedProjectId, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                var forbidden = await PropertyEndpointAuthorization.AuthorizeHouseholdAsync(householdId, PropertyPermissions.Read, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ListIssuesResponse>>(new ListIssuesQuery(householdId, status, areaId, severity, tagIds, isOverdue, linkedProjectId), ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("ListPropertyIssues")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<ListIssuesResponse>()
            .RequireAuthorization();
}
