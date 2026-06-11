using Hemma.Modules.Property.Domain;
using Hemma.Shared.Kernel.Domain;
using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Modules.Property.UnitTests.Domain;

[Trait("Category", "Unit")]
public sealed class ProjectTests
{
    [Fact]
    public void Create_WhenNameIsBlank_ReturnsValidationFailure()
    {
        var result = Project.Create(Guid.NewGuid(), " ", null, ProjectStatus.Planning, null, null, null, null, null);

        Assert.True(result.IsError);
    }

    [Fact]
    public void ChangeStatus_WhenDone_SetsCompletedAt()
    {
        var now = new DateTimeOffset(2026, 06, 11, 12, 0, 0, TimeSpan.Zero);
        var project = CreateProject();

        var result = project.ChangeStatus(ProjectStatus.Done, new FixedClock(now));

        Assert.False(result.IsError);
        Assert.Equal(ProjectStatus.Done, project.Status);
        Assert.Equal(now, project.CompletedAt);
    }

    [Fact]
    public void ChangeStatus_WhenReopened_ClearsCompletedAt()
    {
        var project = CreateProject();
        project.ChangeStatus(ProjectStatus.Done, new FixedClock(DateTimeOffset.UtcNow));

        project.ChangeStatus(ProjectStatus.Active, new FixedClock(DateTimeOffset.UtcNow));

        Assert.Equal(ProjectStatus.Active, project.Status);
        Assert.Null(project.CompletedAt);
    }

    [Fact]
    public void ReorderTasks_WhenIdsMatch_ReassignsZeroBasedSortOrder()
    {
        var project = CreateProject();
        var first = project.AddTask("First", ProjectTaskStatus.Todo, null, null, null).Value;
        var second = project.AddTask("Second", ProjectTaskStatus.Doing, null, null, null).Value;
        var third = project.AddTask("Third", ProjectTaskStatus.Done, null, null, null).Value;

        var result = project.ReorderTasks([third.Id, first.Id, second.Id]);

        Assert.False(result.IsError);
        Assert.Equal([third.Id, first.Id, second.Id], project.Tasks.OrderBy(task => task.SortOrder).Select(task => task.Id).ToArray());
        Assert.Equal([0, 1, 2], project.Tasks.OrderBy(task => task.SortOrder).Select(task => task.SortOrder).ToArray());
    }

    [Fact]
    public void ReorderTasks_WhenSetDoesNotMatch_ReturnsValidationFailure()
    {
        var project = CreateProject();
        var first = project.AddTask("First", ProjectTaskStatus.Todo, null, null, null).Value;
        project.AddTask("Second", ProjectTaskStatus.Todo, null, null, null);

        var result = project.ReorderTasks([first.Id]);

        Assert.True(result.IsError);
    }

    [Fact]
    public void AddAttachment_WhenFileIsTooLarge_ReturnsValidationFailure()
    {
        var project = CreateProject();

        var result = project.AddAttachment(
            "property",
            "key",
            "receipt.pdf",
            "application/pdf",
            ProjectAttachmentRules.MaxSizeBytes + 1);

        Assert.True(result.IsError);
    }

    [Fact]
    public void AddAttachment_WhenContentTypeIsMissing_ReturnsValidationFailure()
    {
        var project = CreateProject();

        var result = project.AddAttachment("property", "key", "receipt.pdf", string.Empty, 128);

        Assert.True(result.IsError);
    }

    [Fact]
    public void AddLink_WhenUrlUsesScriptScheme_ReturnsValidationFailure()
    {
        var project = CreateProject();

        var result = project.AddLink("Bad", "javascript:alert(1)");

        Assert.True(result.IsError);
    }

    [Fact]
    public void AddLink_WhenUrlUsesHttpsScheme_AddsLink()
    {
        var project = CreateProject();

        var result = project.AddLink("Docs", "https://example.com/spec");

        Assert.False(result.IsError);
        Assert.Equal("https://example.com/spec", result.Value.Url);
    }

    [Fact]
    public void Create_WithMoneyEstimate_StoresEstimate()
    {
        var estimate = Money.Create(123.456m, Money.SupportedCurrency).Value;

        var project = Project.Create(Guid.NewGuid(), "Kitchen", null, ProjectStatus.Planning, null, null, null, estimate, null).Value;

        Assert.Equal(123.46m, project.BudgetEstimate!.Amount);
    }

    private static Project CreateProject() =>
        Project.Create(Guid.NewGuid(), "Kitchen refresh", null, ProjectStatus.Planning, "Kitchen", null, null, null, null).Value;

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
