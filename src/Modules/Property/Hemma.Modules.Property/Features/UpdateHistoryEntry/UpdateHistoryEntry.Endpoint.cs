using FluentValidation;
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

namespace Hemma.Modules.Property.Features.UpdateHistoryEntry;

internal static class UpdateHistoryEntryEndpoint
{

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut($"{PropertyRoutes.Prefix}/history/{{historyEntryId:guid}}",
                    async (
                        Guid historyEntryId,
                        HistoryEntryRequest request,
                        IValidator<HistoryEntryRequest> validator,
                        IScopedAuthorizationService<HouseholdScope> authorization,
                        ICurrentUser currentUser,
                        IMessageBus bus,
                        CancellationToken ct) =>
                    {
                        var invalid = await ValidateAsync(request, validator, ct);
                        if (invalid is not null) { return invalid; }

                        var forbidden = await AuthorizeAsync(request.HouseholdId, PropertyPermissions.Write, authorization, currentUser, ct);
                        if (forbidden is not null) { return forbidden; }

                        var result = await bus.InvokeAsync<ErrorOr.ErrorOr<HistoryEntryResponse>>(
                            new UpdateHistoryEntryCommand(
                                historyEntryId,
                                request.HouseholdId,
                                request.Date,
                                request.Title,
                                request.AreaId,
                                request.Cost,
                                request.Type,
                                request.SourceProjectId,
                                request.SourceMaintenanceOccurrenceId),
                            ct);

                        return result.ToProblemDetailsOr(Results.Ok);
                    })
                    .WithName("UpdatePropertyHistoryEntry")
                    .WithTags(PropertyRoutes.GroupTag)
                    .Produces<HistoryEntryResponse>()
                    .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
                    .RequireAuthorization();
    }

    private static async Task<IResult?> ValidateAsync<T>(T request, IValidator<T> validator, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        return validation.IsValid
            ? null
            : Results.ValidationProblem(validation.ToDictionary(), statusCode: StatusCodes.Status422UnprocessableEntity);
    }

    private static Task<IResult?> AuthorizeAsync(
        Guid householdId,
        string permission,
        IScopedAuthorizationService<HouseholdScope> authorization,
        ICurrentUser currentUser,
        CancellationToken ct) =>
        PropertyEndpointAuthorization.AuthorizeHouseholdAsync(householdId, permission, authorization, currentUser, ct);
}
