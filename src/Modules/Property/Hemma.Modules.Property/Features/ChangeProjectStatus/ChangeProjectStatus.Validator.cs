using FluentValidation;
using Hemma.Modules.Property.Domain;

namespace Hemma.Modules.Property.Features.ChangeProjectStatus;

internal sealed class ChangeProjectStatusValidator : AbstractValidator<ChangeProjectStatusRequest>
{
    public ChangeProjectStatusValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.Status).NotEmpty().Must(status =>
            Enum.TryParse<Domain.ProjectStatus>(status, true, out var parsed) && Enum.IsDefined(parsed));
    }
}
