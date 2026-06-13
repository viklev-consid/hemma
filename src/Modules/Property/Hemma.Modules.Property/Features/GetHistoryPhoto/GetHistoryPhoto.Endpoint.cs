using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Modules.Property.Contracts.Authorization;
using Hemma.Modules.Property.Features.Shared;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Property.Features.GetHistoryPhoto;

internal static class GetHistoryPhotoEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet($"{PropertyRoutes.Prefix}/history/{{historyEntryId:guid}}/photos/{{blobKey}}/content",
            async (Guid historyEntryId, string blobKey, Guid householdId, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                var forbidden = await PropertyEndpointAuthorization.AuthorizeHouseholdAsync(householdId, PropertyPermissions.Read, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<HistoryPhotoContentResponse>>(new GetHistoryPhotoQuery(historyEntryId, blobKey, householdId), ct);
                return result.Match<IResult>(photo => Results.File(photo.Content, photo.ContentType, photo.FileName), errors => Results.Problem(statusCode: StatusCodes.Status404NotFound));
            })
            .WithName("GetPropertyHistoryPhotoContent")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces(StatusCodes.Status200OK)
            .RequireAuthorization();
}
