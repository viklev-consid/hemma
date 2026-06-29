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

namespace Hemma.Modules.Property.Features.SnoozeOccurrence;

internal static class SnoozeOccurrenceEndpoint
{
    private const string occurrencesPrefix = $"{PropertyRoutes.Prefix}/maintenance/occurrences";

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost($"{occurrencesPrefix}/{{occurrenceId:guid}}/snooze",
                    async (
                        Guid occurrenceId,
                        SnoozeOccurrenceRequest request,
                        IValidator<SnoozeOccurrenceRequest> validator,
                        IScopedAuthorizationService<HouseholdScope> authorization,
                        ICurrentUser currentUser,
                        IMessageBus bus,
                        CancellationToken ct) =>
                    {
                        var validation = await validator.ValidateAsync(request, ct);
                        if (!validation.IsValid)
                        {
                            return Results.ValidationProblem(validation.ToDictionary(), statusCode: StatusCodes.Status422UnprocessableEntity);
                        }

                        var forbidden = await PropertyEndpointAuthorization.AuthorizeHouseholdAsync(
                            request.HouseholdId,
                            PropertyPermissions.Write,
                            authorization,
                            currentUser,
                            ct);
                        if (forbidden is not null) { return forbidden; }

                        var result = await bus.InvokeAsync<ErrorOr.ErrorOr<MaintenanceOccurrenceResponse>>(
                            new SnoozeOccurrenceCommand(occurrenceId, request.HouseholdId, request.SnoozedUntil, request.Reason),
                            ct);
                        return result.ToProblemDetailsOr(Results.Ok);
                    })
                    .WithName("SnoozePropertyMaintenanceOccurrence")
                    .WithTags(PropertyRoutes.GroupTag)
                    .Produces<MaintenanceOccurrenceResponse>()
                    .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
                    .RequireAuthorization();
    }
}
