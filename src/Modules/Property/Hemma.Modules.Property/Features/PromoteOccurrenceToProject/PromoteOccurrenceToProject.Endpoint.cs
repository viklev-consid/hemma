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

namespace Hemma.Modules.Property.Features.PromoteOccurrenceToProject;

internal static class PromoteOccurrenceToProjectEndpoint
{
    private const string occurrencesPrefix = $"{PropertyRoutes.Prefix}/maintenance/occurrences";

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost($"{occurrencesPrefix}/{{occurrenceId:guid}}/promote",
                    async (
                        Guid occurrenceId,
                        PromoteOccurrenceRequest request,
                        IValidator<PromoteOccurrenceRequest> validator,
                        IScopedAuthorizationService<HouseholdScope> authorization,
                        ICurrentUser currentUser,
                        IMessageBus bus,
                        CancellationToken ct) =>
                    {
                        var invalid = await ValidateAsync(request, validator, ct);
                        if (invalid is not null) { return invalid; }

                        var forbidden = await AuthorizeAsync(request.HouseholdId, PropertyPermissions.Write, authorization, currentUser, ct);
                        if (forbidden is not null) { return forbidden; }

                        var result = await bus.InvokeAsync<ErrorOr.ErrorOr<PromoteOccurrenceResponse>>(
                            new PromoteOccurrenceToProjectCommand(
                                occurrenceId,
                                request.HouseholdId,
                                request.Name,
                                request.Description,
                                request.Status,
                                request.AreaId,
                                request.Priority,
                                request.TargetStartDate,
                                request.TargetEndDate,
                                request.BudgetEstimate,
                                request.Notes),
                            ct);
                        return result.ToProblemDetailsOr(response => Results.Created($"{PropertyRoutes.Prefix}/projects/{response.Project.ProjectId}", response));
                    })
                    .WithName("PromotePropertyMaintenanceOccurrence")
                    .WithTags(PropertyRoutes.GroupTag)
                    .Produces<PromoteOccurrenceResponse>(StatusCodes.Status201Created)
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
