using FluentValidation;
using Hemma.Modules.Property.Domain;

namespace Hemma.Modules.Property.Features.UpdateTask;

internal sealed class UpdateTaskValidator : AbstractValidator<ProjectTaskRequest>
{
    public UpdateTaskValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Status).NotEmpty().Must(status =>
            Enum.TryParse<Domain.ProjectTaskStatus>(status, true, out var parsed) && Enum.IsDefined(parsed));
    }
}
