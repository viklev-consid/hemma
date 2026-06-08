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

namespace Hemma.Modules.Economy.Features.Import.CommitImport;

internal static class CommitImportEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost($"{EconomyRoutes.Prefix}/import/commit",
            async (
                CommitImportRequest request,
                IValidator<CommitImportRequest> validator,
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
                    HouseholdsPermissions.HouseholdsRead,
                    authorization,
                    currentUser,
                    ct);
                if (forbidden is not null)
                {
                    return forbidden;
                }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<CommitImportResponse>>(
                    new CommitImportCommand(request.HouseholdId, request.AccountId, request.PreviewFingerprint, request.Rows),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("CommitEconomyImport")
        .WithSummary("Commit normalized economy transaction import rows.")
        .Produces<CommitImportResponse>()
        .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization();
}
