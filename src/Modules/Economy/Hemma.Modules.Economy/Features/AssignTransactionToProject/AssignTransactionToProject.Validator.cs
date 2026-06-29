using FluentValidation;

namespace Hemma.Modules.Economy.Features.AssignTransactionToProject;

public sealed class AssignTransactionToProjectValidator : AbstractValidator<AssignTransactionToProjectRequest>
{
    public AssignTransactionToProjectValidator()
    {
        RuleFor(request => request.HouseholdId).NotEmpty();
        RuleFor(request => request.ProjectId)
            .NotEqual(Guid.Empty)
            .When(request => request.ProjectId is not null);
    }
}
