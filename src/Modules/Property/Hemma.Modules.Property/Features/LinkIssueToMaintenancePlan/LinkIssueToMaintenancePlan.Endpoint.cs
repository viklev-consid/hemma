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

namespace Hemma.Modules.Property.Features.LinkIssueToMaintenancePlan;

internal static class LinkIssueToMaintenancePlanEndpoint
{
    private const string issuesPrefix = $"{PropertyRoutes.Prefix}/issues";

    public static void Map(IEndpointRouteBuilder app)
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
