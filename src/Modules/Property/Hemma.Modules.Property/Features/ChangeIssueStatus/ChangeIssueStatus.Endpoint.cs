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

namespace Hemma.Modules.Property.Features.ChangeIssueStatus;

internal static class ChangeIssueStatusEndpoint
{
    private const string areasPrefix = $"{PropertyRoutes.Prefix}/areas";
    private const string tagsPrefix = $"{PropertyRoutes.Prefix}/tags";
    private const string issuesPrefix = $"{PropertyRoutes.Prefix}/issues";
    private const string plansPrefix = $"{PropertyRoutes.Prefix}/maintenance/plans";
    private const string occurrencesPrefix = $"{PropertyRoutes.Prefix}/maintenance/occurrences";

    public static void Map(IEndpointRouteBuilder app)
    {
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
