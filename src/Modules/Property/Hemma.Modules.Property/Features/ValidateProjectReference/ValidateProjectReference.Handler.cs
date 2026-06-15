using Hemma.Modules.Property.Contracts.Queries;
using Hemma.Modules.Property.Domain;
using Hemma.Modules.Property.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Property.Features.ValidateProjectReference;

public sealed class ValidateProjectReferenceHandler(PropertyDbContext db)
{
    public async Task<ValidateProjectReferenceResult> Handle(ValidateProjectReferenceQuery query, CancellationToken ct)
    {
        var exists = await db.Projects
            .AsNoTracking()
            .AnyAsync(project => project.HouseholdId == query.HouseholdId && project.Id == new ProjectId(query.ProjectId), ct);

        return new ValidateProjectReferenceResult(exists);
    }
}
