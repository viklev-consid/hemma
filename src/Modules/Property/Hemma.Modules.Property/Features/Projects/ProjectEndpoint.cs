using FluentValidation;
using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Modules.Property.Contracts.Authorization;
using Hemma.Modules.Property.Domain;
using Hemma.Modules.Property.Errors;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Property.Features.Projects;

internal static class ProjectEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost($"{PropertyRoutes.Prefix}/projects",
            async (
                ProjectRequest request,
                IValidator<ProjectRequest> validator,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var invalid = await ValidateAsync(request, validator, ct);
                if (invalid is not null) { return invalid; }

                var forbidden = await AuthorizeAsync(request.HouseholdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ProjectResponse>>(
                    new CreateProjectCommand(
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

                return result.ToProblemDetailsOr(response => Results.Created($"{PropertyRoutes.Prefix}/projects/{response.ProjectId}", response));
            })
            .WithName("CreatePropertyProject")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<ProjectResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization();

        app.MapGet($"{PropertyRoutes.Prefix}/projects",
            async (
                Guid householdId,
                string? status,
                string? area,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var forbidden = await AuthorizeAsync(householdId, PropertyPermissions.Read, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ListProjectsResponse>>(new ListProjectsQuery(householdId, status, area), ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("ListPropertyProjects")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<ListProjectsResponse>()
            .RequireAuthorization();

        app.MapGet($"{PropertyRoutes.Prefix}/projects/{{projectId:guid}}",
            async (
                Guid projectId,
                Guid householdId,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var forbidden = await AuthorizeAsync(householdId, PropertyPermissions.Read, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ProjectResponse>>(new GetProjectQuery(projectId, householdId), ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("GetPropertyProject")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<ProjectResponse>()
            .RequireAuthorization();

        app.MapGet($"{PropertyRoutes.Prefix}/projects/{{projectId:guid}}/budget",
            async (
                Guid projectId,
                Guid householdId,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var forbidden = await AuthorizeAsync(householdId, PropertyPermissions.Read, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<GetProjectBudgetResponse>>(new GetProjectBudgetQuery(projectId, householdId), ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("GetPropertyProjectBudget")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<GetProjectBudgetResponse>()
            .RequireAuthorization();

        app.MapPut($"{PropertyRoutes.Prefix}/projects/{{projectId:guid}}",
            async (
                Guid projectId,
                ProjectRequest request,
                IValidator<ProjectRequest> validator,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var invalid = await ValidateAsync(request, validator, ct);
                if (invalid is not null) { return invalid; }

                var forbidden = await AuthorizeAsync(request.HouseholdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ProjectResponse>>(
                    new UpdateProjectCommand(
                        projectId,
                        request.HouseholdId,
                        request.Name,
                        request.Description,
                        request.Area,
                        request.TargetStartDate,
                        request.TargetEndDate,
                        request.BudgetEstimate,
                        request.Notes),
                    ct);

                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("UpdatePropertyProject")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<ProjectResponse>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization();

        app.MapPost($"{PropertyRoutes.Prefix}/projects/{{projectId:guid}}/status",
            async (
                Guid projectId,
                ChangeProjectStatusRequest request,
                IValidator<ChangeProjectStatusRequest> validator,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var invalid = await ValidateAsync(request, validator, ct);
                if (invalid is not null) { return invalid; }

                var forbidden = await AuthorizeAsync(request.HouseholdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ChangeProjectStatusResponse>>(
                    new ChangeProjectStatusCommand(projectId, request.HouseholdId, request.Status),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("ChangePropertyProjectStatus")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<ChangeProjectStatusResponse>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization();

        app.MapDelete($"{PropertyRoutes.Prefix}/projects/{{projectId:guid}}",
            async (
                Guid projectId,
                Guid householdId,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var forbidden = await AuthorizeAsync(householdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ErrorOr.Deleted>>(new DeleteProjectCommand(projectId, householdId), ct);
                return result.ToProblemDetailsOr(_ => Results.NoContent());
            })
            .WithName("DeletePropertyProject")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces(StatusCodes.Status204NoContent)
            .RequireAuthorization();

        MapTasks(app);
        MapLinks(app);
        MapAttachments(app);
    }

    private static void MapTasks(IEndpointRouteBuilder app)
    {
        app.MapGet($"{PropertyRoutes.Prefix}/projects/{{projectId:guid}}/tasks",
            async (Guid projectId, Guid householdId, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                var forbidden = await AuthorizeAsync(householdId, PropertyPermissions.Read, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<GetProjectTasksResponse>>(new GetProjectTasksQuery(projectId, householdId), ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("GetPropertyProjectTasks")
            .WithTags(PropertyRoutes.GroupTag)
            .RequireAuthorization();

        app.MapPost($"{PropertyRoutes.Prefix}/projects/{{projectId:guid}}/tasks",
            async (Guid projectId, ProjectTaskRequest request, IValidator<ProjectTaskRequest> validator, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                var invalid = await ValidateAsync(request, validator, ct);
                if (invalid is not null) { return invalid; }
                var forbidden = await AuthorizeAsync(request.HouseholdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ProjectTaskResponse>>(
                    new AddTaskCommand(projectId, request.HouseholdId, request.Title, request.Status, request.Estimate, request.AssigneeId, request.DueDate),
                    ct);
                return result.ToProblemDetailsOr(response => Results.Created($"{PropertyRoutes.Prefix}/projects/{projectId}/tasks/{response.TaskId}", response));
            })
            .WithName("AddPropertyProjectTask")
            .WithTags(PropertyRoutes.GroupTag)
            .RequireAuthorization();

        app.MapPut($"{PropertyRoutes.Prefix}/projects/{{projectId:guid}}/tasks/{{taskId:guid}}",
            async (Guid projectId, Guid taskId, ProjectTaskRequest request, IValidator<ProjectTaskRequest> validator, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                var invalid = await ValidateAsync(request, validator, ct);
                if (invalid is not null) { return invalid; }
                var forbidden = await AuthorizeAsync(request.HouseholdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ProjectTaskResponse>>(
                    new UpdateTaskCommand(projectId, taskId, request.HouseholdId, request.Title, request.Status, request.Estimate, request.AssigneeId, request.DueDate),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("UpdatePropertyProjectTask")
            .WithTags(PropertyRoutes.GroupTag)
            .RequireAuthorization();

        app.MapPost($"{PropertyRoutes.Prefix}/projects/{{projectId:guid}}/tasks/reorder",
            async (Guid projectId, ReorderTasksRequest request, IValidator<ReorderTasksRequest> validator, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                var invalid = await ValidateAsync(request, validator, ct);
                if (invalid is not null) { return invalid; }
                var forbidden = await AuthorizeAsync(request.HouseholdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<GetProjectTasksResponse>>(
                    new ReorderTasksCommand(projectId, request.HouseholdId, request.TaskIds),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("ReorderPropertyProjectTasks")
            .WithTags(PropertyRoutes.GroupTag)
            .RequireAuthorization();

        app.MapDelete($"{PropertyRoutes.Prefix}/projects/{{projectId:guid}}/tasks/{{taskId:guid}}",
            async (Guid projectId, Guid taskId, Guid householdId, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                var forbidden = await AuthorizeAsync(householdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ErrorOr.Deleted>>(new DeleteTaskCommand(projectId, taskId, householdId), ct);
                return result.ToProblemDetailsOr(_ => Results.NoContent());
            })
            .WithName("DeletePropertyProjectTask")
            .WithTags(PropertyRoutes.GroupTag)
            .RequireAuthorization();
    }

    private static void MapLinks(IEndpointRouteBuilder app)
    {
        app.MapPost($"{PropertyRoutes.Prefix}/projects/{{projectId:guid}}/links",
            async (Guid projectId, ProjectLinkRequest request, IValidator<ProjectLinkRequest> validator, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                var invalid = await ValidateAsync(request, validator, ct);
                if (invalid is not null) { return invalid; }
                var forbidden = await AuthorizeAsync(request.HouseholdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ProjectLinkResponse>>(
                    new AddLinkCommand(projectId, request.HouseholdId, request.Label, request.Url),
                    ct);
                return result.ToProblemDetailsOr(response => Results.Created($"{PropertyRoutes.Prefix}/projects/{projectId}/links/{response.LinkId}", response));
            })
            .WithName("AddPropertyProjectLink")
            .WithTags(PropertyRoutes.GroupTag)
            .RequireAuthorization();

        app.MapDelete($"{PropertyRoutes.Prefix}/projects/{{projectId:guid}}/links/{{linkId:guid}}",
            async (Guid projectId, Guid linkId, Guid householdId, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                var forbidden = await AuthorizeAsync(householdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ErrorOr.Deleted>>(new RemoveLinkCommand(projectId, linkId, householdId), ct);
                return result.ToProblemDetailsOr(_ => Results.NoContent());
            })
            .WithName("RemovePropertyProjectLink")
            .WithTags(PropertyRoutes.GroupTag)
            .RequireAuthorization();
    }

    private static void MapAttachments(IEndpointRouteBuilder app)
    {
        app.MapPost($"{PropertyRoutes.Prefix}/projects/{{projectId:guid}}/attachments",
            async (Guid projectId, Guid householdId, IFormFile file, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                var forbidden = await AuthorizeAsync(householdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                if (!ProjectAttachmentRules.IsAllowed(file.ContentType, file.Length))
                {
                    return Results.ValidationProblem(
                        new Dictionary<string, string[]>(StringComparer.Ordinal)
                        {
                            [PropertyErrors.AttachmentFileInvalid.Code] = [PropertyErrors.AttachmentFileInvalid.Description]
                        },
                        statusCode: StatusCodes.Status422UnprocessableEntity);
                }

                await using var stream = file.OpenReadStream();
                using var memory = new MemoryStream();
                await stream.CopyToAsync(memory, ct);

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ProjectAttachmentResponse>>(
                    new AddAttachmentCommand(projectId, householdId, file.FileName, file.ContentType, memory.ToArray()),
                    ct);
                return result.ToProblemDetailsOr(response => Results.Created($"{PropertyRoutes.Prefix}/projects/{projectId}/attachments/{response.AttachmentId}", response));
            })
            .WithName("AddPropertyProjectAttachment")
            .WithTags(PropertyRoutes.GroupTag)
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(ProjectAttachmentRules.MaxSizeBytes + 1024 * 1024))
            .RequireAuthorization();

        app.MapGet($"{PropertyRoutes.Prefix}/projects/{{projectId:guid}}/attachments/{{attachmentId:guid}}/content",
            async (Guid projectId, Guid attachmentId, Guid householdId, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                var forbidden = await AuthorizeAsync(householdId, PropertyPermissions.Read, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<AttachmentContentResponse>>(
                    new GetAttachmentContentQuery(projectId, attachmentId, householdId),
                    ct);
                return result.Match<IResult>(
                    content => Results.File(content.Content, content.ContentType, content.FileName),
                    Problems.FromErrors);
            })
            .WithName("GetPropertyProjectAttachmentContent")
            .WithTags(PropertyRoutes.GroupTag)
            .RequireAuthorization();

        app.MapDelete($"{PropertyRoutes.Prefix}/projects/{{projectId:guid}}/attachments/{{attachmentId:guid}}",
            async (Guid projectId, Guid attachmentId, Guid householdId, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                var forbidden = await AuthorizeAsync(householdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ErrorOr.Deleted>>(new RemoveAttachmentCommand(projectId, attachmentId, householdId), ct);
                return result.ToProblemDetailsOr(_ => Results.NoContent());
            })
            .WithName("RemovePropertyProjectAttachment")
            .WithTags(PropertyRoutes.GroupTag)
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
