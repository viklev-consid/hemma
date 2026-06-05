using FluentValidation;
using Hemma.Modules.Economy.Features.Contracts;
using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Economy.Features.CategorizationRules;

internal static class CategorizationRuleEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet($"{EconomyRoutes.Prefix}/categorization-rules",
            async (
                Guid householdId,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var forbidden = await AuthorizeAsync(householdId, HouseholdsPermissions.HouseholdsRead, authorization, currentUser, ct);
                if (forbidden is not null)
                {
                    return forbidden;
                }

                var response = await bus.InvokeAsync<ListCategorizationRulesResponse>(new ListCategorizationRulesQuery(householdId), ct);
                return Results.Ok(response);
            })
            .WithName("ListEconomyCategorizationRules")
            .WithSummary("List economy categorization rules.")
            .Produces<ListCategorizationRulesResponse>()
            .RequireAuthorization();

        app.MapPost($"{EconomyRoutes.Prefix}/categorization-rules",
            async (
                CategorizationRuleRequest request,
                IValidator<CategorizationRuleRequest> validator,
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

                var forbidden = await AuthorizeAsync(request.HouseholdId, HouseholdsPermissions.HouseholdsWrite, authorization, currentUser, ct);
                if (forbidden is not null)
                {
                    return forbidden;
                }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<CategorizationRuleResponse>>(
                    new CreateCategorizationRuleCommand(request.HouseholdId, request.Match, request.Pattern, request.TargetCategoryId),
                    ct);
                return result.ToProblemDetailsOr(response => Results.Created($"{EconomyRoutes.Prefix}/categorization-rules/{response.CategorizationRuleId}", response));
            })
            .WithName("CreateEconomyCategorizationRule")
            .WithSummary("Create an economy categorization rule.")
            .Produces<CategorizationRuleResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization();

        app.MapPut($"{EconomyRoutes.Prefix}/categorization-rules/{{ruleId:guid}}",
            async (
                Guid ruleId,
                CategorizationRuleRequest request,
                IValidator<CategorizationRuleRequest> validator,
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

                var forbidden = await AuthorizeAsync(request.HouseholdId, HouseholdsPermissions.HouseholdsWrite, authorization, currentUser, ct);
                if (forbidden is not null)
                {
                    return forbidden;
                }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<CategorizationRuleResponse>>(
                    new UpdateCategorizationRuleCommand(ruleId, request.HouseholdId, request.Match, request.Pattern, request.TargetCategoryId),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("UpdateEconomyCategorizationRule")
            .WithSummary("Update an economy categorization rule.")
            .Produces<CategorizationRuleResponse>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization();

        app.MapPatch($"{EconomyRoutes.Prefix}/categorization-rules/{{ruleId:guid}}/enabled",
            async (
                Guid ruleId,
                SetCategorizationRuleEnabledRequest request,
                IValidator<SetCategorizationRuleEnabledRequest> validator,
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

                var forbidden = await AuthorizeAsync(request.HouseholdId, HouseholdsPermissions.HouseholdsWrite, authorization, currentUser, ct);
                if (forbidden is not null)
                {
                    return forbidden;
                }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<CategorizationRuleResponse>>(
                    new SetCategorizationRuleEnabledCommand(ruleId, request.HouseholdId, request.Enabled),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("SetEconomyCategorizationRuleEnabled")
            .WithSummary("Enable or disable an economy categorization rule.")
            .Produces<CategorizationRuleResponse>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization();

        app.MapDelete($"{EconomyRoutes.Prefix}/categorization-rules/{{ruleId:guid}}",
            async (
                Guid ruleId,
                Guid householdId,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var forbidden = await AuthorizeAsync(householdId, HouseholdsPermissions.HouseholdsWrite, authorization, currentUser, ct);
                if (forbidden is not null)
                {
                    return forbidden;
                }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ErrorOr.Deleted>>(
                    new DeleteCategorizationRuleCommand(ruleId, householdId),
                    ct);
                return result.ToProblemDetailsOr(_ => Results.NoContent());
            })
            .WithName("DeleteEconomyCategorizationRule")
            .WithSummary("Delete an economy categorization rule.")
            .Produces(StatusCodes.Status204NoContent)
            .RequireAuthorization();
    }

    private static Task<IResult?> AuthorizeAsync(
        Guid householdId,
        string permission,
        IScopedAuthorizationService<HouseholdScope> authorization,
        ICurrentUser currentUser,
        CancellationToken ct) =>
        EconomyEndpointAuthorization.AuthorizeHouseholdAsync(householdId, permission, authorization, currentUser, ct);
}
