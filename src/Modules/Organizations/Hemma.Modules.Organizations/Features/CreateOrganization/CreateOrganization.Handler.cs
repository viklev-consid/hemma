using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Hemma.Modules.Organizations.Contracts.Events;
using Hemma.Modules.Organizations.Domain;
using Hemma.Modules.Organizations.Errors;
using Hemma.Modules.Organizations.Persistence;
using Hemma.Shared.Infrastructure.Persistence;
using Hemma.Shared.Kernel.Interfaces;
using Wolverine;

namespace Hemma.Modules.Organizations.Features.CreateOrganization;

public sealed class CreateOrganizationHandler(OrganizationsDbContext db, IClock clock, IMessageBus bus)
{
    public async Task<ErrorOr<CreateOrganizationResponse>> Handle(CreateOrganizationCommand cmd, CancellationToken ct)
    {
        var slugResult = string.IsNullOrWhiteSpace(cmd.Slug)
            ? OrganizationSlug.FromName(cmd.Name)
            : OrganizationSlug.Create(cmd.Slug);
        if (slugResult.IsError)
        {
            return slugResult.Errors;
        }

        var slug = slugResult.Value;
        if (await db.Organizations.AnyAsync(o => o.Slug == slug, ct))
        {
            return OrganizationsErrors.SlugAlreadyExists;
        }

        var organizationResult = Organization.Create(cmd.Name, slug, cmd.CreatedByUserId, clock);
        if (organizationResult.IsError)
        {
            return organizationResult.Errors;
        }

        var organization = organizationResult.Value;
        db.Organizations.Add(organization);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            db.ChangeTracker.Clear();
            return OrganizationsErrors.SlugAlreadyExists;
        }

        await bus.PublishAsync(new OrganizationCreatedV1(
            organization.Id.Value,
            organization.Name,
            organization.Slug.Value,
            cmd.CreatedByUserId,
            Guid.NewGuid()));

        return new CreateOrganizationResponse(
            organization.Id.Value,
            organization.Name,
            organization.Slug.Value,
            OrganizationRole.Owner.Name);
    }
}
