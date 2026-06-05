using Hemma.Modules.Economy.Features.Contracts;
using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Economy.Features.AttachReceipt;

internal static class AttachReceiptEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost($"{EconomyRoutes.Prefix}/transactions/{{transactionId:guid}}/receipt",
            async (
                Guid transactionId,
                [FromForm] Guid householdId,
                IFormFile file,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                if (file.Length == 0)
                {
                    return Results.ValidationProblem(
                        new Dictionary<string, string[]>(StringComparer.Ordinal) { ["file"] = ["Receipt file is required."] },
                        statusCode: StatusCodes.Status422UnprocessableEntity);
                }

                var forbidden = await EconomyEndpointAuthorization.AuthorizeHouseholdAsync(
                    householdId,
                    HouseholdsPermissions.HouseholdsWrite,
                    authorization,
                    currentUser,
                    ct);
                if (forbidden is not null)
                {
                    return forbidden;
                }

                await using var stream = file.OpenReadStream();
                using var memory = new MemoryStream();
                await stream.CopyToAsync(memory, ct);

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<AttachReceiptResponse>>(
                    new AttachReceiptCommand(householdId, transactionId, memory.ToArray(), file.ContentType, file.FileName),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("AttachEconomyTransactionReceipt")
        .WithSummary("Upload and attach a receipt to an economy transaction.")
        .Accepts<IFormFile>("multipart/form-data")
        .Produces<AttachReceiptResponse>()
        .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
        .DisableAntiforgery()
        .RequireAuthorization();
}
