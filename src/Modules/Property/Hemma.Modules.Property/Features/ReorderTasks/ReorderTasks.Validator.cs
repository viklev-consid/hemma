using FluentValidation;
using Hemma.Modules.Property.Domain;

namespace Hemma.Modules.Property.Features.ReorderTasks;

internal sealed class ReorderTasksValidator : AbstractValidator<ReorderTasksRequest>
{
    public const int MaxTaskIds = 500;

    public ReorderTasksValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.TaskIds).NotEmpty().Must(ids => ids.Count <= MaxTaskIds);
    }
}
