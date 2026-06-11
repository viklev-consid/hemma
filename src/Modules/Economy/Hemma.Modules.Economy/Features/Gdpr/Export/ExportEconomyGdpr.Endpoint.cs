using Hemma.Modules.Economy.Features.Contracts;
using Hemma.Modules.Economy.Gdpr;
using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Kernel.Gdpr;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Hemma.Modules.Economy.Features.Gdpr.Export;

internal static class ExportEconomyGdprEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet($"{EconomyRoutes.Prefix}/gdpr/export",
            async (
                Guid householdId,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                [FromServices] EconomyPersonalDataExporter exporter,
                IClock clock,
                CancellationToken ct) =>
            {
                var forbidden = await EconomyEndpointAuthorization.AuthorizeHouseholdAsync(
                    householdId,
                    HouseholdsPermissions.HouseholdsRead,
                    authorization,
                    currentUser,
                    ct);
                if (forbidden is not null)
                {
                    return forbidden;
                }

                if (!Guid.TryParse(currentUser.Id, out var userId))
                {
                    return Results.Forbid();
                }

                var export = await exporter.ExportAsync(new UserRef(userId), householdId, ct);
                var data = export.Data;
                return Results.Ok(new ExportEconomyGdprResponse(householdId, clock.UtcNow, data));
            })
        .WithName("ExportEconomyGdpr")
        .WithSummary("Export the caller's economy personal data for GDPR access requests.")
        .Produces<ExportEconomyGdprResponse>()
        .RequireAuthorization();
}
