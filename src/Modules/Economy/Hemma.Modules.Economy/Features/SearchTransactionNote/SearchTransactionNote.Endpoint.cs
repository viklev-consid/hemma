using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Shared.Contracts;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Economy.Features.SearchTransactionNote;

internal static class SearchTransactionNoteEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet($"{EconomyRoutes.Prefix}/transactions/search",
            async (
                Guid householdId,
                string search,
                int? page,
                int? pageSize,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(search))
                {
                    return Results.ValidationProblem(
                        new Dictionary<string, string[]>(StringComparer.Ordinal) { ["search"] = ["Search text is required."] },
                        statusCode: StatusCodes.Status422UnprocessableEntity);
                }

                if (search.Trim().Length > 100)
                {
                    return Results.ValidationProblem(
                        new Dictionary<string, string[]>(StringComparer.Ordinal) { ["search"] = ["Search text cannot exceed 100 characters."] },
                        statusCode: StatusCodes.Status422UnprocessableEntity);
                }

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

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<SearchTransactionNoteResponse>>(
                    new SearchTransactionNoteQuery(householdId, search, page ?? 1, pageSize ?? 50),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("SearchEconomyTransactionNotes")
        .WithSummary("Search economy transaction notes.")
        .Produces<SearchTransactionNoteResponse>()
        .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization();
}
