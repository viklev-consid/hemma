using FluentValidation;
using Hemma.Shared.Contracts;
using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Economy.Features.Subscriptions;

internal static class SubscriptionEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost($"{EconomyRoutes.Prefix}/subscriptions",
            async (
                CreateSubscriptionRequest request,
                IValidator<CreateSubscriptionRequest> validator,
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

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<SubscriptionResponse>>(
                    new CreateSubscriptionCommand(
                        request.HouseholdId,
                        request.Name,
                        request.CadenceFrequency,
                        request.CadenceInterval,
                        request.ChargeDay,
                        request.ExpectedAmount.Amount,
                        request.ExpectedAmount.Currency,
                        request.LifecycleState,
                        request.TrialEndsOn,
                        request.AccountId,
                        request.StartsOn),
                    ct);

                return result.ToProblemDetailsOr(response => Results.Created($"{EconomyRoutes.Prefix}/subscriptions/{response.SubscriptionId}", response));
            })
        .WithName("CreateEconomySubscription")
        .WithSummary("Create an observe-only economy subscription.")
        .Produces<SubscriptionResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization();

        app.MapGet($"{EconomyRoutes.Prefix}/subscriptions",
            async (
                Guid householdId,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var forbidden = await EconomyEndpointAuthorization.AuthorizeHouseholdAsync(
                    householdId,
                    HouseholdsPermissions.HouseholdsRead,
                    authorization,
                    currentUser,
                    ct);
                if (forbidden is not null)
                {
                    return forbidden;
                }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ListSubscriptionsResponse>>(
                    new ListSubscriptionsQuery(householdId),
                    ct);

                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("ListEconomySubscriptions")
        .WithSummary("List economy subscriptions for a household.")
        .Produces<ListSubscriptionsResponse>(StatusCodes.Status200OK)
        .RequireAuthorization();

        app.MapGet($"{EconomyRoutes.Prefix}/subscriptions/{{subscriptionId:guid}}",
            async (
                Guid subscriptionId,
                Guid householdId,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var forbidden = await EconomyEndpointAuthorization.AuthorizeHouseholdAsync(
                    householdId,
                    HouseholdsPermissions.HouseholdsRead,
                    authorization,
                    currentUser,
                    ct);
                if (forbidden is not null)
                {
                    return forbidden;
                }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<SubscriptionResponse>>(
                    new GetSubscriptionQuery(householdId, subscriptionId),
                    ct);

                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("GetEconomySubscription")
        .WithSummary("Get a single economy subscription.")
        .Produces<SubscriptionResponse>(StatusCodes.Status200OK)
        .RequireAuthorization();

        app.MapPut($"{EconomyRoutes.Prefix}/subscriptions/{{subscriptionId:guid}}/state",
            async (
                Guid subscriptionId,
                ChangeLifecycleStateRequest request,
                IValidator<ChangeLifecycleStateRequest> validator,
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

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<SubscriptionResponse>>(
                    new ChangeLifecycleStateCommand(request.HouseholdId, subscriptionId, request.LifecycleState, request.TrialEndsOn),
                    ct);

                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("ChangeEconomySubscriptionLifecycleState")
        .WithSummary("Change an economy subscription lifecycle state.")
        .Produces<SubscriptionResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization();

        app.MapGet($"{EconomyRoutes.Prefix}/subscriptions/{{subscriptionId:guid}}/charge-history",
            async (
                Guid subscriptionId,
                Guid householdId,
                int? page,
                int? pageSize,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var forbidden = await EconomyEndpointAuthorization.AuthorizeHouseholdAsync(
                    householdId,
                    HouseholdsPermissions.HouseholdsRead,
                    authorization,
                    currentUser,
                    ct);
                if (forbidden is not null)
                {
                    return forbidden;
                }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ChargeHistoryResponse>>(
                    new GetChargeHistoryQuery(householdId, subscriptionId, page ?? 1, pageSize ?? 50),
                    ct);

                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("GetEconomySubscriptionChargeHistory")
        .WithSummary("Get linked charge history and derived price changes for an economy subscription.")
        .Produces<ChargeHistoryResponse>(StatusCodes.Status200OK)
        .RequireAuthorization();

        app.MapGet($"{EconomyRoutes.Prefix}/subscriptions/{{subscriptionId:guid}}/link-candidates",
            async (
                Guid subscriptionId,
                Guid householdId,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var forbidden = await EconomyEndpointAuthorization.AuthorizeHouseholdAsync(
                    householdId,
                    HouseholdsPermissions.HouseholdsRead,
                    authorization,
                    currentUser,
                    ct);
                if (forbidden is not null)
                {
                    return forbidden;
                }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<LinkCandidatesResponse>>(
                    new GetLinkCandidatesQuery(householdId, subscriptionId),
                    ct);

                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("GetEconomySubscriptionLinkCandidates")
        .WithSummary("Get unlinked transactions that likely belong to an economy subscription.")
        .Produces<LinkCandidatesResponse>(StatusCodes.Status200OK)
        .RequireAuthorization();

        app.MapPost($"{EconomyRoutes.Prefix}/subscriptions/{{subscriptionId:guid}}/link",
            async (
                Guid subscriptionId,
                LinkTransactionRequest request,
                IValidator<LinkTransactionRequest> validator,
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

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<TransactionResponse>>(
                    new LinkTransactionCommand(request.HouseholdId, subscriptionId, request.TransactionId),
                    ct);

                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("LinkEconomySubscriptionTransaction")
        .WithSummary("Link a transaction to an economy subscription.")
        .Produces<TransactionResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization();

        app.MapPost($"{EconomyRoutes.Prefix}/subscriptions/{{subscriptionId:guid}}/unlink",
            async (
                Guid subscriptionId,
                LinkTransactionRequest request,
                IValidator<LinkTransactionRequest> validator,
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

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<TransactionResponse>>(
                    new UnlinkTransactionCommand(request.HouseholdId, subscriptionId, request.TransactionId),
                    ct);

                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("UnlinkEconomySubscriptionTransaction")
        .WithSummary("Unlink a transaction from an economy subscription.")
        .Produces<TransactionResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization();

        app.MapGet($"{EconomyRoutes.Prefix}/subscriptions/payment-schedule",
            async (
                Guid householdId,
                int year,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var forbidden = await EconomyEndpointAuthorization.AuthorizeHouseholdAsync(
                    householdId,
                    HouseholdsPermissions.HouseholdsRead,
                    authorization,
                    currentUser,
                    ct);
                if (forbidden is not null)
                {
                    return forbidden;
                }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<PaymentScheduleResponse>>(
                    new GetPaymentScheduleQuery(householdId, year),
                    ct);

                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("GetEconomySubscriptionPaymentSchedule")
        .WithSummary("Get the year-view subscription payment schedule.")
        .Produces<PaymentScheduleResponse>(StatusCodes.Status200OK)
        .RequireAuthorization();

        app.MapGet($"{EconomyRoutes.Prefix}/subscriptions/month-calendar",
            async (
                Guid householdId,
                DateOnly month,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var forbidden = await EconomyEndpointAuthorization.AuthorizeHouseholdAsync(
                    householdId,
                    HouseholdsPermissions.HouseholdsRead,
                    authorization,
                    currentUser,
                    ct);
                if (forbidden is not null)
                {
                    return forbidden;
                }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<MonthChargeCalendarResponse>>(
                    new GetMonthChargeCalendarQuery(householdId, month),
                    ct);

                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("GetEconomySubscriptionMonthCalendar")
        .WithSummary("Get the month-view subscription charge calendar.")
        .Produces<MonthChargeCalendarResponse>(StatusCodes.Status200OK)
        .RequireAuthorization();
    }
}
