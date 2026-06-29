using ErrorOr;
using Hemma.Modules.Users.Domain;
using Hemma.Modules.Users.Errors;
using Hemma.Modules.Users.Legal;
using Hemma.Modules.Users.Persistence;
using Hemma.Shared.Infrastructure.Persistence;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Users.Features.AcceptLegalDocuments;

public sealed class AcceptLegalDocumentsHandler(UsersDbContext db, IClock clock, ILegalComplianceService complianceService)
{
    public async Task<ErrorOr<Success>> Handle(AcceptLegalDocumentsCommand cmd, CancellationToken ct)
        => await UsersTelemetry.InstrumentAsync(nameof(AcceptLegalDocumentsHandler), () => HandleCoreAsync(cmd, ct));

    private async Task<ErrorOr<Success>> HandleCoreAsync(AcceptLegalDocumentsCommand cmd, CancellationToken ct)
    {
        var exists = await db.Users.AnyAsync(u => u.Id == cmd.UserId, ct);
        if (!exists)
        {
            return UsersErrors.UserNotFound;
        }

        var acceptedById = cmd.AcceptedDocuments
            .GroupBy(d => d.DocumentId)
            .ToDictionary(g => g.Key, g => g.First());

        var acceptedDocumentIds = acceptedById.Keys.Select(id => new LegalDocumentId(id)).ToArray();
        var documents = await db.LegalDocuments
            .Where(d => acceptedDocumentIds.Contains(d.Id))
            .ToListAsync(ct);

        if (documents.Count != acceptedById.Count)
        {
            return UsersErrors.LegalDocumentAcceptanceInvalid;
        }

        foreach (var document in documents)
        {
            var accepted = acceptedById[document.Id.Value];
            if (document.SupersededAt is not null ||
                (!document.IsRequiredForOnboarding && !document.IsRequiredForContinuedUse) ||
                !string.Equals(accepted.Version, document.Version, StringComparison.Ordinal) ||
                !string.Equals(accepted.ContentHash, document.ContentHash, StringComparison.Ordinal))
            {
                return UsersErrors.LegalDocumentAcceptanceInvalid;
            }
        }

        var now = clock.UtcNow;
        var alreadyAccepted = await db.TermsAcceptances
            .Where(a => a.UserId == cmd.UserId && a.LegalDocumentId != null && acceptedDocumentIds.Contains(a.LegalDocumentId!))
            .Select(a => a.LegalDocumentId!.Value)
            .ToHashSetAsync(ct);

        foreach (var document in documents)
        {
            if (alreadyAccepted.Contains(document.Id.Value))
            {
                continue;
            }

            db.TermsAcceptances.Add(TermsAcceptance.Record(cmd.UserId, document, now, cmd.IpAddress, cmd.UserAgent));
        }

        try
        {
            await db.SaveChangesAsync(ct);
            await complianceService.InvalidateContinuedUseComplianceAsync(cmd.UserId, ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            db.ChangeTracker.Clear();
        }

        return Result.Success;
    }
}
