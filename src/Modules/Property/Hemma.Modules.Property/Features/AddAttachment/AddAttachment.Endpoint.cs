using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Modules.Property.Contracts.Authorization;
using Hemma.Modules.Property.Domain;
using Hemma.Modules.Property.Errors;
using Hemma.Modules.Property.Features.Shared;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Property.Features.AddAttachment;

internal static class AddAttachmentEndpoint
{

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost($"{PropertyRoutes.Prefix}/projects/{{projectId:guid}}/attachments",
                    async (Guid projectId, Guid householdId, IFormFile file, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
                    {
                        var forbidden = await AuthorizeAsync(householdId, PropertyPermissions.Write, authorization, currentUser, ct);
                        if (forbidden is not null) { return forbidden; }

                        if (!ProjectAttachmentRules.IsAllowed(file.ContentType, file.Length))
                        {
                            return Results.ValidationProblem(
                                new Dictionary<string, string[]>(StringComparer.Ordinal)
                                {
                                    [PropertyErrors.AttachmentFileInvalid.Code] = [PropertyErrors.AttachmentFileInvalid.Description]
                                },
                                statusCode: StatusCodes.Status422UnprocessableEntity);
                        }

                        await using var stream = file.OpenReadStream();
                        using var memory = new MemoryStream();
                        await stream.CopyToAsync(memory, ct);

                        var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ProjectAttachmentResponse>>(
                            new AddAttachmentCommand(projectId, householdId, file.FileName, file.ContentType, memory.ToArray()),
                            ct);
                        return result.ToProblemDetailsOr(response => Results.Created($"{PropertyRoutes.Prefix}/projects/{projectId}/attachments/{response.AttachmentId}", response));
                    })
                    .WithName("AddPropertyProjectAttachment")
                    .WithTags(PropertyRoutes.GroupTag)
                    .DisableAntiforgery()
                    .WithMetadata(new RequestSizeLimitAttribute(ProjectAttachmentRules.MaxSizeBytes + 1024 * 1024))
                    .RequireAuthorization();
    }

    private static Task<IResult?> AuthorizeAsync(
        Guid householdId,
        string permission,
        IScopedAuthorizationService<HouseholdScope> authorization,
        ICurrentUser currentUser,
        CancellationToken ct) =>
        PropertyEndpointAuthorization.AuthorizeHouseholdAsync(householdId, permission, authorization, currentUser, ct);
}
