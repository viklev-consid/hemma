using FluentValidation;
using Hemma.Modules.Property.Domain;

namespace Hemma.Modules.Property.Features.ReorderAreas;

internal sealed class ReorderAreasValidator : AbstractValidator<ReorderAreasRequest>
{
    public ReorderAreasValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.AreaIds).NotEmpty();
    }
}
