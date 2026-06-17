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

namespace Hemma.Modules.Property.Features.ArchiveTag;

internal static class ArchiveTagEndpoint
{
    private const string tagsPrefix = $"{PropertyRoutes.Prefix}/tags";

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost($"{tagsPrefix}/{{tagId:guid}}/archive",
                    async (Guid tagId, Guid householdId, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
                    {
                        var forbidden = await AuthorizeAsync(householdId, PropertyPermissions.Write, authorization, currentUser, ct);
                        if (forbidden is not null) { return forbidden; }

                        var result = await bus.InvokeAsync<ErrorOr.ErrorOr<PropertyTagResponse>>(new ArchiveTagCommand(tagId, householdId), ct);
                        return result.ToProblemDetailsOr(Results.Ok);
                    })
                    .WithName("ArchivePropertyTag")
                    .WithTags(PropertyRoutes.GroupTag)
                    .Produces<PropertyTagResponse>()
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
