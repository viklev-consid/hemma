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

namespace Hemma.Modules.Property.Features.RemoveAttachment;

internal static class RemoveAttachmentEndpoint
{
    private const string areasPrefix = $"{PropertyRoutes.Prefix}/areas";
    private const string tagsPrefix = $"{PropertyRoutes.Prefix}/tags";
    private const string issuesPrefix = $"{PropertyRoutes.Prefix}/issues";
    private const string plansPrefix = $"{PropertyRoutes.Prefix}/maintenance/plans";
    private const string occurrencesPrefix = $"{PropertyRoutes.Prefix}/maintenance/occurrences";

    public static void Map(IEndpointRouteBuilder app)
    {
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
