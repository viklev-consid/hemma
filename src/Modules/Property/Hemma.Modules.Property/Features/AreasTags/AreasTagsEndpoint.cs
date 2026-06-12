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

namespace Hemma.Modules.Property.Features.AreasTags;

internal static class AreasTagsEndpoint
{
    private const string areasPrefix = $"{PropertyRoutes.Prefix}/areas";
    private const string tagsPrefix = $"{PropertyRoutes.Prefix}/tags";

    public static void Map(IEndpointRouteBuilder app)
    {
        MapAreas(app);
        MapTags(app);
    }

    private static void MapAreas(IEndpointRouteBuilder app)
    {
        app.MapGet(areasPrefix,
            async (Guid householdId, bool? includeArchived, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                var forbidden = await AuthorizeAsync(householdId, PropertyPermissions.Read, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ListAreasResponse>>(new ListAreasQuery(householdId, includeArchived == true), ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("ListPropertyAreas")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<ListAreasResponse>()
            .RequireAuthorization();

        app.MapPost(areasPrefix,
            async (PropertyAreaRequest request, IValidator<PropertyAreaRequest> validator, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                var invalid = await ValidateAsync(request, validator, ct);
                if (invalid is not null) { return invalid; }

                var forbidden = await AuthorizeAsync(request.HouseholdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<PropertyAreaResponse>>(new CreateAreaCommand(request.HouseholdId, request.Name, request.Description), ct);
                return result.ToProblemDetailsOr(response => Results.Created($"{areasPrefix}/{response.AreaId}", response));
            })
            .WithName("CreatePropertyArea")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<PropertyAreaResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization();

        app.MapPut($"{areasPrefix}/{{areaId:guid}}",
            async (Guid areaId, PropertyAreaRequest request, IValidator<PropertyAreaRequest> validator, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                var invalid = await ValidateAsync(request, validator, ct);
                if (invalid is not null) { return invalid; }

                var forbidden = await AuthorizeAsync(request.HouseholdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<PropertyAreaResponse>>(new UpdateAreaCommand(areaId, request.HouseholdId, request.Name, request.Description), ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("UpdatePropertyArea")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<PropertyAreaResponse>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization();

        app.MapPost($"{areasPrefix}/{{areaId:guid}}/archive",
            async (Guid areaId, Guid householdId, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                var forbidden = await AuthorizeAsync(householdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<PropertyAreaResponse>>(new ArchiveAreaCommand(areaId, householdId), ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("ArchivePropertyArea")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<PropertyAreaResponse>()
            .RequireAuthorization();

        app.MapPost($"{areasPrefix}/reorder",
            async (ReorderAreasRequest request, IValidator<ReorderAreasRequest> validator, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                var invalid = await ValidateAsync(request, validator, ct);
                if (invalid is not null) { return invalid; }

                var forbidden = await AuthorizeAsync(request.HouseholdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ListAreasResponse>>(new ReorderAreasCommand(request.HouseholdId, request.AreaIds), ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("ReorderPropertyAreas")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<ListAreasResponse>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization();
    }

    private static void MapTags(IEndpointRouteBuilder app)
    {
        app.MapGet(tagsPrefix,
            async (Guid householdId, bool? includeArchived, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                var forbidden = await AuthorizeAsync(householdId, PropertyPermissions.Read, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ListTagsResponse>>(new ListTagsQuery(householdId, includeArchived == true), ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("ListPropertyTags")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<ListTagsResponse>()
            .RequireAuthorization();

        app.MapPost(tagsPrefix,
            async (PropertyTagRequest request, IValidator<PropertyTagRequest> validator, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                var invalid = await ValidateAsync(request, validator, ct);
                if (invalid is not null) { return invalid; }

                var forbidden = await AuthorizeAsync(request.HouseholdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<PropertyTagResponse>>(new CreateTagCommand(request.HouseholdId, request.Name, request.Color), ct);
                return result.ToProblemDetailsOr(response => Results.Created($"{tagsPrefix}/{response.TagId}", response));
            })
            .WithName("CreatePropertyTag")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<PropertyTagResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization();

        app.MapPut($"{tagsPrefix}/{{tagId:guid}}",
            async (Guid tagId, PropertyTagRequest request, IValidator<PropertyTagRequest> validator, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                var invalid = await ValidateAsync(request, validator, ct);
                if (invalid is not null) { return invalid; }

                var forbidden = await AuthorizeAsync(request.HouseholdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<PropertyTagResponse>>(new UpdateTagCommand(tagId, request.HouseholdId, request.Name, request.Color), ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("UpdatePropertyTag")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<PropertyTagResponse>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization();

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

        app.MapPut($"{tagsPrefix}/assignments",
            async (AssignTagsRequest request, IValidator<AssignTagsRequest> validator, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                var invalid = await ValidateAsync(request, validator, ct);
                if (invalid is not null) { return invalid; }

                var forbidden = await AuthorizeAsync(request.HouseholdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<AssignTagsResponse>>(
                    new AssignTagsCommand(request.HouseholdId, request.TargetType, request.TargetId, request.TagIds),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("AssignPropertyTags")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<AssignTagsResponse>()
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
