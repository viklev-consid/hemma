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

namespace Hemma.Modules.Property.Features.AssignTags;

public sealed record AssignTagsRequest(Guid HouseholdId, string TargetType, Guid TargetId, IReadOnlyList<Guid> TagIds);

internal sealed class AssignTagsValidator : AbstractValidator<AssignTagsRequest>
{
    public AssignTagsValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.TargetId).NotEmpty();
        RuleFor(x => x.TargetType).NotEmpty().Must(type =>
            Enum.TryParse<Hemma.Modules.Property.Domain.PropertyTagTargetType>(type, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed));
        RuleFor(x => x.TagIds).NotNull();
    }
}

internal static class AssignTagsEndpoint
{
    private const string tagsPrefix = $"{PropertyRoutes.Prefix}/tags";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPut($"{tagsPrefix}/assignments",
            async (AssignTagsRequest request, IValidator<AssignTagsRequest> validator, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                var validation = await validator.ValidateAsync(request, ct);
                if (!validation.IsValid) { return Results.ValidationProblem(validation.ToDictionary(), statusCode: StatusCodes.Status422UnprocessableEntity); }

                var forbidden = await PropertyEndpointAuthorization.AuthorizeHouseholdAsync(request.HouseholdId, PropertyPermissions.Write, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<AssignTagsResponse>>(new AssignTagsCommand(request.HouseholdId, request.TargetType, request.TargetId, request.TagIds), ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("AssignPropertyTags")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<AssignTagsResponse>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization();
}
