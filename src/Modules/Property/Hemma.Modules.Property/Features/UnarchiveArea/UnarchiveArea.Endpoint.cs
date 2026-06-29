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

namespace Hemma.Modules.Property.Features.UnarchiveArea;

internal static class UnarchiveAreaEndpoint
{
    private const string areasPrefix = $"{PropertyRoutes.Prefix}/areas";

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost($"{areasPrefix}/{{areaId:guid}}/unarchive",
                    async (Guid areaId, Guid householdId, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
                    {
                        var forbidden = await PropertyEndpointAuthorization.AuthorizeHouseholdAsync(householdId, PropertyPermissions.Write, authorization, currentUser, ct);
                        if (forbidden is not null) { return forbidden; }

                        var result = await bus.InvokeAsync<ErrorOr.ErrorOr<PropertyAreaResponse>>(new UnarchiveAreaCommand(areaId, householdId), ct);
                        return result.ToProblemDetailsOr(Results.Ok);
                    })
                    .WithName("UnarchivePropertyArea")
                    .WithTags(PropertyRoutes.GroupTag)
                    .Produces<PropertyAreaResponse>()
                    .RequireAuthorization();
    }
}
