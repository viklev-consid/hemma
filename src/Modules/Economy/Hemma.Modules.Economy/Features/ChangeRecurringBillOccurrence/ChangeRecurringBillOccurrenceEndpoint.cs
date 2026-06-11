using FluentValidation;
using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Shared.Contracts;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Economy.Features.ChangeRecurringBillOccurrence;

internal static class ChangeRecurringBillOccurrenceEndpoint
{
    public static RouteHandlerBuilder Map(
        IEndpointRouteBuilder app,
        string route,
        string name,
        string summary,
        RecurringBillOccurrenceAction action) =>
        app.MapPost(route,
            async (
                Guid recurringBillId,
                ChangeRecurringBillOccurrenceRequest request,
                IValidator<ChangeRecurringBillOccurrenceRequest> validator,
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

                var forbidden = await EconomyEndpointAuthorization.AuthorizeHouseholdAsync(
                    request.HouseholdId,
                    HouseholdsPermissions.HouseholdsWrite,
                    authorization,
                    currentUser,
                    ct);
                if (forbidden is not null)
                {
                    return forbidden;
                }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<RecurringBillResponse>>(
                    new ChangeRecurringBillOccurrenceCommand(request.HouseholdId, recurringBillId, request.DueOn, action),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName(name)
        .WithSummary(summary)
        .Produces<RecurringBillResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization();
}
