using Hemma.Shared.Contracts;

namespace Hemma.Modules.Property.Features.UpdateTask;

public sealed record UpdateTaskCommand(Guid ProjectId, Guid TaskId, Guid HouseholdId, string Title, string Status, MoneyDto? Estimate, Guid? AssigneeId, DateOnly? DueDate);
