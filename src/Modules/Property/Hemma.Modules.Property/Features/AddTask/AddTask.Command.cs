using Hemma.Shared.Contracts;

namespace Hemma.Modules.Property.Features.AddTask;

public sealed record AddTaskCommand(Guid ProjectId, Guid HouseholdId, string Title, string Status, MoneyDto? Estimate, Guid? AssigneeId, DateOnly? DueDate);
