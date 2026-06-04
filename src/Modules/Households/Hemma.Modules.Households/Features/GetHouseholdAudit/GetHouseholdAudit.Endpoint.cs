using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Hemma.Modules.Audit.Contracts.Queries;
using Hemma.Modules.Households.Authorization;
using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Wolverine;

namespace Hemma.Modules.Households.Features.GetHouseholdAudit;

internal static class GetHouseholdAuditEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(HouseholdsRoutes.Audit,
            async (
                string householdRef,
                IHouseholdRefResolver resolver,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                int? page,
                int? pageSize,
                CancellationToken ct) =>
            {
                var household = await resolver.ResolveAsync(householdRef, ct);
                if (household.IsError)
                {
                    return household.ToProblemDetailsOr(_ => Results.Empty);
                }

                var access = await authorization.AuthorizeAsync(
                    currentUser,
                    household.Value,
                    HouseholdsPermissions.AuditRead,
                    ScopedAuthorizationOptions.WithPlatformOverride,
                    ct);
                if (!access.Succeeded)
                {
                    return Results.Forbid();
                }

                var audit = await bus.InvokeAsync<ErrorOr.ErrorOr<ListHouseholdAuditEntriesResponse>>(
                    new ListHouseholdAuditEntriesQuery(household.Value.Id.Value, page ?? 1, pageSize ?? 20),
                    ct);

                return audit.ToProblemDetailsOr(response => Results.Ok(new GetHouseholdAuditResponse(
                        household.Value.Id.Value,
                        access.AccessMode.ToString(),
                        response.Entries,
                        response.Total,
                        response.Page,
                        response.PageSize)));
            })
        .WithName("GetHouseholdAudit")
        .WithSummary("Get household audit entries.")
        .Produces<GetHouseholdAuditResponse>()
        .RequireAuthorization();
}
