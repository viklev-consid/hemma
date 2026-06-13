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

namespace Hemma.Modules.Property.Features.Issues;

internal static class IssueEndpoint
{
    private const string issuesPrefix = $"{PropertyRoutes.Prefix}/issues";

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(issuesPrefix,
            async (IssueRequest request, IValidator<IssueRequest> validator, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                var invalid = await ValidateAsync(request, validator, ct);
                if (invalid is not null) { return invalid; }

                var forbidden = await AuthorizeAsync(request.HouseholdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<IssueResponse>>(
                    new ReportIssueCommand(request.HouseholdId, request.Title, request.Description, request.AreaId, request.Severity, request.DueDate, request.Notes),
                    ct);
                return result.ToProblemDetailsOr(response => Results.Created($"{issuesPrefix}/{response.IssueId}", response));
            })
            .WithName("ReportPropertyIssue")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<IssueResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization();

        app.MapGet(issuesPrefix,
            async (
                Guid householdId,
                string? status,
                Guid? areaId,
                string? severity,
                [Microsoft.AspNetCore.Mvc.FromQuery] Guid[]? tagIds,
                bool? isOverdue,
                Guid? linkedProjectId,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var forbidden = await AuthorizeAsync(householdId, PropertyPermissions.Read, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ListIssuesResponse>>(
                    new ListIssuesQuery(householdId, status, areaId, severity, tagIds, isOverdue, linkedProjectId),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("ListPropertyIssues")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<ListIssuesResponse>()
            .RequireAuthorization();

        app.MapGet($"{issuesPrefix}/{{issueId:guid}}",
            async (Guid issueId, Guid householdId, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                var forbidden = await AuthorizeAsync(householdId, PropertyPermissions.Read, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<IssueResponse>>(new GetIssueQuery(issueId, householdId), ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("GetPropertyIssue")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<IssueResponse>()
            .RequireAuthorization();

        app.MapPut($"{issuesPrefix}/{{issueId:guid}}",
            async (Guid issueId, IssueRequest request, IValidator<IssueRequest> validator, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                var invalid = await ValidateAsync(request, validator, ct);
                if (invalid is not null) { return invalid; }

                var forbidden = await AuthorizeAsync(request.HouseholdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<IssueResponse>>(
                    new UpdateIssueCommand(issueId, request.HouseholdId, request.Title, request.Description, request.AreaId, request.Severity, request.DueDate, request.Notes),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("UpdatePropertyIssue")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<IssueResponse>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization();

        app.MapPost($"{issuesPrefix}/{{issueId:guid}}/status",
            async (Guid issueId, ChangeIssueStatusRequest request, IValidator<ChangeIssueStatusRequest> validator, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                var invalid = await ValidateAsync(request, validator, ct);
                if (invalid is not null) { return invalid; }

                var forbidden = await AuthorizeAsync(request.HouseholdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<IssueResponse>>(new ChangeIssueStatusCommand(issueId, request.HouseholdId, request.Status), ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("ChangePropertyIssueStatus")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<IssueResponse>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization();

        app.MapDelete($"{issuesPrefix}/{{issueId:guid}}",
            async (Guid issueId, Guid householdId, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                var forbidden = await AuthorizeAsync(householdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ErrorOr.Deleted>>(new DeleteIssueCommand(issueId, householdId), ct);
                return result.ToProblemDetailsOr(_ => Results.NoContent());
            })
            .WithName("DeletePropertyIssue")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces(StatusCodes.Status204NoContent)
            .RequireAuthorization();

        MapLinks(app);
    }

    private static void MapLinks(IEndpointRouteBuilder app)
    {
        app.MapPost($"{issuesPrefix}/{{issueId:guid}}/links/maintenance-plan",
            async (Guid issueId, LinkIssueRequest request, IValidator<LinkIssueRequest> validator, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                var invalid = await ValidateAsync(request, validator, ct);
                if (invalid is not null) { return invalid; }

                var forbidden = await AuthorizeAsync(request.HouseholdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<IssueResponse>>(new LinkIssueToMaintenancePlanCommand(issueId, request.HouseholdId, request.TargetId), ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("LinkPropertyIssueToMaintenancePlan")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<IssueResponse>()
            .RequireAuthorization();

        app.MapPost($"{issuesPrefix}/{{issueId:guid}}/links/maintenance-occurrence",
            async (Guid issueId, LinkIssueRequest request, IValidator<LinkIssueRequest> validator, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                var invalid = await ValidateAsync(request, validator, ct);
                if (invalid is not null) { return invalid; }

                var forbidden = await AuthorizeAsync(request.HouseholdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<IssueResponse>>(new LinkIssueToMaintenanceOccurrenceCommand(issueId, request.HouseholdId, request.TargetId), ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("LinkPropertyIssueToMaintenanceOccurrence")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<IssueResponse>()
            .RequireAuthorization();

        app.MapDelete($"{issuesPrefix}/{{issueId:guid}}/links",
            async (Guid issueId, Guid householdId, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                var forbidden = await AuthorizeAsync(householdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<IssueResponse>>(new UnlinkIssueCommand(issueId, householdId), ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("UnlinkPropertyIssue")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<IssueResponse>()
            .RequireAuthorization();

        app.MapPost($"{issuesPrefix}/{{issueId:guid}}/promote",
            async (Guid issueId, PromoteIssueToProjectRequest request, IValidator<PromoteIssueToProjectRequest> validator, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                var invalid = await ValidateAsync(request, validator, ct);
                if (invalid is not null) { return invalid; }

                var forbidden = await AuthorizeAsync(request.HouseholdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<PromoteIssueToProjectResponse>>(
                    new PromoteIssueToProjectCommand(
                        issueId,
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
            .WithName("PromotePropertyIssueToProject")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<PromoteIssueToProjectResponse>(StatusCodes.Status201Created)
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
