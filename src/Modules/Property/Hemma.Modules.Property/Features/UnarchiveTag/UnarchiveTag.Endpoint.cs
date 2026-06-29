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

namespace Hemma.Modules.Property.Features.UnarchiveTag;

internal static class UnarchiveTagEndpoint
{
    private const string tagsPrefix = $"{PropertyRoutes.Prefix}/tags";

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost($"{tagsPrefix}/{{tagId:guid}}/unarchive",
                    async (Guid tagId, Guid householdId, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
                    {
                        var forbidden = await PropertyEndpointAuthorization.AuthorizeHouseholdAsync(householdId, PropertyPermissions.Write, authorization, currentUser, ct);
                        if (forbidden is not null) { return forbidden; }

                        var result = await bus.InvokeAsync<ErrorOr.ErrorOr<PropertyTagResponse>>(new UnarchiveTagCommand(tagId, householdId), ct);
                        return result.ToProblemDetailsOr(Results.Ok);
                    })
                    .WithName("UnarchivePropertyTag")
                    .WithTags(PropertyRoutes.GroupTag)
                    .Produces<PropertyTagResponse>()
                    .RequireAuthorization();
    }
}
