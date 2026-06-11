using FluentValidation;
using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Modules.Property.Contracts.Authorization;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Property.Features.Logbook;

internal static class LogbookEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost($"{PropertyRoutes.Prefix}/history",
            async (
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
                    new CreateHistoryEntryCommand(
                        request.HouseholdId,
                        request.Date,
                        request.Title,
                        request.Area,
                        request.Cost,
                        request.Type,
                        request.SourceProjectId,
                        request.SourceMaintenanceOccurrenceId,
                        request.PhotoRefs),
                    ct);

                return result.ToProblemDetailsOr(response => Results.Created($"{PropertyRoutes.Prefix}/history/{response.HistoryEntryId}", response));
            })
            .WithName("CreatePropertyHistoryEntry")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<HistoryEntryResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization();

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
                        request.Area,
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

        app.MapGet($"{PropertyRoutes.Prefix}/history",
            async (
                Guid householdId,
                int? year,
                string? area,
                string? type,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var forbidden = await AuthorizeAsync(householdId, PropertyPermissions.Read, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ListHistoryResponse>>(
                    new ListHistoryQuery(householdId, year, area, type),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("ListPropertyHistory")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<ListHistoryResponse>()
            .RequireAuthorization();

        app.MapGet($"{PropertyRoutes.Prefix}/history/{{historyEntryId:guid}}/photos/{{blobKey}}/content",
            async (
                Guid historyEntryId,
                string blobKey,
                Guid householdId,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var forbidden = await AuthorizeAsync(householdId, PropertyPermissions.Read, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<HistoryPhotoContentResponse>>(
                    new GetHistoryPhotoQuery(historyEntryId, blobKey, householdId),
                    ct);
                return result.Match<IResult>(
                    content => Results.File(content.Content, content.ContentType, content.FileName),
                    Problems.FromErrors);
            })
            .WithName("GetPropertyHistoryPhotoContent")
            .WithTags(PropertyRoutes.GroupTag)
            .RequireAuthorization();

        app.MapDelete($"{PropertyRoutes.Prefix}/history/{{historyEntryId:guid}}",
            async (
                Guid historyEntryId,
                Guid householdId,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var forbidden = await AuthorizeAsync(householdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ErrorOr.Deleted>>(
                    new DeleteHistoryEntryCommand(historyEntryId, householdId),
                    ct);
                return result.ToProblemDetailsOr(_ => Results.NoContent());
            })
            .WithName("DeletePropertyHistoryEntry")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces(StatusCodes.Status204NoContent)
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
        Projects.PropertyEndpointAuthorization.AuthorizeHouseholdAsync(householdId, permission, authorization, currentUser, ct);
}
