using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Hemma.Modules.Households.Contracts.Events;
using Hemma.Modules.Households.Domain;
using Hemma.Modules.Households.Errors;
using Hemma.Modules.Households.Persistence;
using Hemma.Shared.Infrastructure.Persistence;
using Hemma.Shared.Kernel.Interfaces;
using Wolverine;

namespace Hemma.Modules.Households.Features.CreateHousehold;

public sealed class CreateHouseholdHandler(HouseholdsDbContext db, IClock clock, IMessageBus bus)
{
    public async Task<ErrorOr<CreateHouseholdResponse>> Handle(CreateHouseholdCommand cmd, CancellationToken ct)
    {
        var slugResult = string.IsNullOrWhiteSpace(cmd.Slug)
            ? HouseholdSlug.FromName(cmd.Name)
            : HouseholdSlug.Create(cmd.Slug);
        if (slugResult.IsError)
        {
            return slugResult.Errors;
        }

        var slug = slugResult.Value;
        if (await db.Households.AnyAsync(o => o.Slug == slug, ct))
        {
            return HouseholdsErrors.SlugAlreadyExists;
        }

        var householdResult = Household.Create(cmd.Name, slug, cmd.CreatedByUserId, clock);
        if (householdResult.IsError)
        {
            return householdResult.Errors;
        }

        var household = householdResult.Value;
        db.Households.Add(household);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            db.ChangeTracker.Clear();
            return HouseholdsErrors.SlugAlreadyExists;
        }

        await bus.PublishAsync(new HouseholdCreatedV1(
            household.Id.Value,
            household.Name,
            household.Slug.Value,
            cmd.CreatedByUserId,
            Guid.NewGuid()));

        return new CreateHouseholdResponse(
            household.Id.Value,
            household.Name,
            household.Slug.Value,
            HouseholdRole.Owner.Name);
    }
}
