using FluentValidation;
using Hemma.Modules.Organizations.Domain;

namespace Hemma.Modules.Organizations.Features.ChangeOrganizationMemberRole;

internal sealed class ChangeOrganizationMemberRoleValidator : AbstractValidator<ChangeOrganizationMemberRoleRequest>
{
    public ChangeOrganizationMemberRoleValidator()
    {
        RuleFor(r => r.Role)
            .NotEmpty()
            .MaximumLength(32)
            .Must(role => !OrganizationRole.Create(role).IsError)
            .WithMessage("Organization role is not valid.");
    }
}
