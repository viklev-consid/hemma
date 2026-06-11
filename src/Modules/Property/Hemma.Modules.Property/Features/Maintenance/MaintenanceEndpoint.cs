using FluentValidation;
using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Modules.Property.Contracts.Authorization;
using Hemma.Modules.Property.Features.Projects;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Property.Features.Maintenance;

internal static class MaintenanceEndpoint
{
    private const string PlansPrefix = $"{PropertyRoutes.Prefix}/maintenance/plans";
    private const string OccurrencesPrefix = $"{PropertyRoutes.Prefix}/maintenance/occurrences";

    public static void Map(IEndpointRouteBuilder app)
    {
        MapPlans(app);
        MapOccurrences(app);
    }

    private static void MapPlans(IEndpointRouteBuilder app)
    {
        app.MapPost(PlansPrefix,
            async (
                MaintenancePlanRequest request,
                IValidator<MaintenancePlanRequest> validator,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var invalid = await ValidateAsync(request, validator, ct);
                if (invalid is not null) { return invalid; }

                var forbidden = await AuthorizeAsync(request.HouseholdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<GetMaintenancePlanResponse>>(
                    new CreateMaintenancePlanCommand(
                        request.HouseholdId,
                        request.Title,
                        request.Description,
                        request.Area,
                        request.RecurrenceUnit,
                        request.RecurrenceInterval,
                        request.AnchorDate,
                        request.LeadTimeDays),
                    ct);

                return result.ToProblemDetailsOr(response => Results.Created($"{PlansPrefix}/{response.Plan.PlanId}", response));
            })
            .WithName("CreatePropertyMaintenancePlan")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<GetMaintenancePlanResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization();

        app.MapGet(PlansPrefix,
            async (
                Guid householdId,
                bool? activeOnly,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var forbidden = await AuthorizeAsync(householdId, PropertyPermissions.Read, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ListMaintenancePlansResponse>>(new ListMaintenancePlansQuery(householdId, activeOnly), ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("ListPropertyMaintenancePlans")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<ListMaintenancePlansResponse>()
            .RequireAuthorization();

        app.MapGet($"{PlansPrefix}/{{planId:guid}}",
            async (
                Guid planId,
                Guid householdId,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var forbidden = await AuthorizeAsync(householdId, PropertyPermissions.Read, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<GetMaintenancePlanResponse>>(new GetMaintenancePlanQuery(planId, householdId), ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("GetPropertyMaintenancePlan")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<GetMaintenancePlanResponse>()
            .RequireAuthorization();

        app.MapPut($"{PlansPrefix}/{{planId:guid}}",
            async (
                Guid planId,
                MaintenancePlanRequest request,
                IValidator<MaintenancePlanRequest> validator,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var invalid = await ValidateAsync(request, validator, ct);
                if (invalid is not null) { return invalid; }

                var forbidden = await AuthorizeAsync(request.HouseholdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<MaintenancePlanResponse>>(
                    new UpdateMaintenancePlanCommand(
                        planId,
                        request.HouseholdId,
                        request.Title,
                        request.Description,
                        request.Area,
                        request.RecurrenceUnit,
                        request.RecurrenceInterval,
                        request.AnchorDate,
                        request.LeadTimeDays),
                    ct);

                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("UpdatePropertyMaintenancePlan")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<MaintenancePlanResponse>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization();

        app.MapPost($"{PlansPrefix}/{{planId:guid}}/deactivate",
            async (
                Guid planId,
                Guid householdId,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var forbidden = await AuthorizeAsync(householdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<MaintenancePlanResponse>>(new DeactivatePlanCommand(planId, householdId), ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("DeactivatePropertyMaintenancePlan")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<MaintenancePlanResponse>()
            .RequireAuthorization();

        app.MapDelete($"{PlansPrefix}/{{planId:guid}}",
            async (
                Guid planId,
                Guid householdId,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var forbidden = await AuthorizeAsync(householdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ErrorOr.Deleted>>(new DeletePlanCommand(planId, householdId), ct);
                return result.ToProblemDetailsOr(_ => Results.NoContent());
            })
            .WithName("DeletePropertyMaintenancePlan")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces(StatusCodes.Status204NoContent)
            .RequireAuthorization();
    }

    private static void MapOccurrences(IEndpointRouteBuilder app)
    {
        app.MapGet(OccurrencesPrefix,
            async (
                Guid householdId,
                int? horizonDays,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var forbidden = await AuthorizeAsync(householdId, PropertyPermissions.Read, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ListUpcomingOccurrencesResponse>>(
                    new ListUpcomingOccurrencesQuery(householdId, horizonDays ?? 30),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("ListPropertyUpcomingMaintenanceOccurrences")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<ListUpcomingOccurrencesResponse>()
            .RequireAuthorization();

        app.MapPost($"{OccurrencesPrefix}/{{occurrenceId:guid}}/complete",
            async (
                Guid occurrenceId,
                CompleteOccurrenceRequest request,
                IValidator<CompleteOccurrenceRequest> validator,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var invalid = await ValidateAsync(request, validator, ct);
                if (invalid is not null) { return invalid; }

                var forbidden = await AuthorizeAsync(request.HouseholdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<CompleteOccurrenceResponse>>(
                    new CompleteOccurrenceCommand(occurrenceId, request.HouseholdId, request.Notes),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("CompletePropertyMaintenanceOccurrence")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<CompleteOccurrenceResponse>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization();

        app.MapPost($"{OccurrencesPrefix}/{{occurrenceId:guid}}/skip",
            async (
                Guid occurrenceId,
                SkipOccurrenceRequest request,
                IValidator<SkipOccurrenceRequest> validator,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var invalid = await ValidateAsync(request, validator, ct);
                if (invalid is not null) { return invalid; }

                var forbidden = await AuthorizeAsync(request.HouseholdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<SkipOccurrenceResponse>>(
                    new SkipOccurrenceCommand(occurrenceId, request.HouseholdId, request.Notes),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("SkipPropertyMaintenanceOccurrence")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<SkipOccurrenceResponse>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization();

        app.MapPost($"{OccurrencesPrefix}/{{occurrenceId:guid}}/promote",
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
                        request.Area,
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
