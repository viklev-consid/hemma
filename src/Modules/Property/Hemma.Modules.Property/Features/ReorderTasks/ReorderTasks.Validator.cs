using FluentValidation;
using Hemma.Modules.Property.Domain;

namespace Hemma.Modules.Property.Features.ReorderTasks;

internal sealed class ReorderTasksValidator : AbstractValidator<ReorderTasksRequest>
{
    public ReorderTasksValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.TaskIds).NotEmpty();
    }
}
