using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.GetProjectBudget;

public sealed class GetProjectBudgetHandler(ProjectsOperations operations)
{
    public Task<ErrorOr<GetProjectBudgetResponse>> Handle(GetProjectBudgetQuery message, CancellationToken ct) => operations.GetProjectBudgetAsync(message, ct);
}
