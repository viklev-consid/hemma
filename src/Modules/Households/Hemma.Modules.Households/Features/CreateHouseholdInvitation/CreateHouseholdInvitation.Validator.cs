using FluentValidation;
using Hemma.Modules.Households.Domain;

namespace Hemma.Modules.Households.Features.CreateHouseholdInvitation;

internal sealed class CreateHouseholdInvitationValidator : AbstractValidator<CreateHouseholdInvitationRequest>
{
    public CreateHouseholdInvitationValidator()
    {
        RuleFor(r => r.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(r => r.Role)
            .NotEmpty()
            .MaximumLength(32)
            .Must(role => !HouseholdRole.Create(role).IsError)
            .WithMessage("Household role is not valid.");
    }
}
