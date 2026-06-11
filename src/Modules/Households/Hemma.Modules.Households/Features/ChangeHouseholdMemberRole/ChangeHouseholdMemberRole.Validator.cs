using FluentValidation;
using Hemma.Modules.Households.Domain;

namespace Hemma.Modules.Households.Features.ChangeHouseholdMemberRole;

internal sealed class ChangeHouseholdMemberRoleValidator : AbstractValidator<ChangeHouseholdMemberRoleRequest>
{
    public ChangeHouseholdMemberRoleValidator()
    {
        RuleFor(r => r.Role)
            .NotEmpty()
            .MaximumLength(32)
            .Must(role => !HouseholdRole.Create(role).IsError)
            .WithMessage("Household role is not valid.");
    }
}
