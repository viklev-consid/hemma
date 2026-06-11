using Hemma.Shared.Contracts;
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
    private const long maxReceiptBytes = 10 * 1024 * 1024;

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
                    return InvalidFile("Receipt file is required.");
                }

                if (file.Length > maxReceiptBytes)
                {
                    return InvalidFile("Receipt file cannot exceed 10 MB.");
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
                if (!await IsSupportedReceiptAsync(stream, ct))
                {
                    return InvalidFile("Receipt file must be a PDF, PNG, or JPEG.");
                }

                if (!stream.CanSeek)
                {
                    return InvalidFile("Receipt stream must be seekable.");
                }

                stream.Position = 0;
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

    private static IResult InvalidFile(string message) =>
        Results.ValidationProblem(
            new Dictionary<string, string[]>(StringComparer.Ordinal) { ["file"] = [message] },
            statusCode: StatusCodes.Status422UnprocessableEntity);

    private static async Task<bool> IsSupportedReceiptAsync(Stream stream, CancellationToken ct)
    {
        var header = new byte[8];
        var read = await stream.ReadAsync(header, ct);

        return IsPdf(header, read) || IsPng(header, read) || IsJpeg(header, read);
    }

    private static bool IsPdf(byte[] header, int read) =>
        read >= 4 &&
        header[0] == 0x25 &&
        header[1] == 0x50 &&
        header[2] == 0x44 &&
        header[3] == 0x46;

    private static bool IsPng(byte[] header, int read) =>
        read >= 8 &&
        header[0] == 0x89 &&
        header[1] == 0x50 &&
        header[2] == 0x4E &&
        header[3] == 0x47 &&
        header[4] == 0x0D &&
        header[5] == 0x0A &&
        header[6] == 0x1A &&
        header[7] == 0x0A;

    private static bool IsJpeg(byte[] header, int read) =>
        read >= 3 &&
        header[0] == 0xFF &&
        header[1] == 0xD8 &&
        header[2] == 0xFF;
}
