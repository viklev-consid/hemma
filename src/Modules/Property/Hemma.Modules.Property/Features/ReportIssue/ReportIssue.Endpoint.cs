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

namespace Hemma.Modules.Property.Features.ReportIssue;

internal static class ReportIssueEndpoint
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
