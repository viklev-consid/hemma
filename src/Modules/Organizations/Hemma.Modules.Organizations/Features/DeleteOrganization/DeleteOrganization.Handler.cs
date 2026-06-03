using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Hemma.Modules.Organizations.Contracts.Events;
using Hemma.Modules.Organizations.Errors;
using Hemma.Modules.Organizations.Persistence;
using Hemma.Shared.Kernel.Interfaces;
using Wolverine;

namespace Hemma.Modules.Organizations.Features.DeleteOrganization;

public sealed class DeleteOrganizationHandler(OrganizationsDbContext db, IClock clock, IMessageBus bus)
{
    public async Task<ErrorOr<Success>> Handle(DeleteOrganizationCommand cmd, CancellationToken ct)
    {
        var organization = await db.Organizations
            .Include(o => o.Memberships)
            .FirstOrDefaultAsync(o => o.Id == cmd.OrganizationId, ct);

        if (organization is null)
        {
            return OrganizationsErrors.OrganizationNotFound;
        }

        var delete = organization.Delete(cmd.DeletedByUserId, clock);
        if (delete.IsError)
        {
            return delete.Errors;
        }

        await db.SaveChangesAsync(ct);
        await bus.PublishAsync(new OrganizationDeletedV1(cmd.OrganizationId.Value, cmd.DeletedByUserId, Guid.NewGuid()));
        return Result.Success;
    }
}
