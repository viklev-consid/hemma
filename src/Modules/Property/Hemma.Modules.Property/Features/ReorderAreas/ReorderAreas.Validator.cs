using FluentValidation;
using Hemma.Modules.Property.Domain;

namespace Hemma.Modules.Property.Features.ReorderAreas;

internal sealed class ReorderAreasValidator : AbstractValidator<ReorderAreasRequest>
{
    public const int MaxAreaIds = 200;

    public ReorderAreasValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.AreaIds).NotEmpty().Must(ids => ids.Count <= MaxAreaIds);
    }
}
