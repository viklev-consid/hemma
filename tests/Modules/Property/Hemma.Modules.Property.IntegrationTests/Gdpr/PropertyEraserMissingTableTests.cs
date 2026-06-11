using Hemma.Modules.Property.Gdpr;
using Hemma.Modules.Property.Persistence;
using Hemma.Shared.Infrastructure.Blobs;
using Hemma.Shared.Kernel.Gdpr;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hemma.Modules.Property.IntegrationTests.Gdpr;

/// <summary>
/// Verifies the eraser tolerates a host where Property migrations have not run (the table
/// is undefined) AND that the skipped erasure is logged as a Warning, so a genuine
/// deployment error cannot silently report a successful GDPR erasure.
/// The eraser is constructed directly with a capturing logger, bypassing the host's
/// Serilog pipeline, so the assertion observes exactly what the catch block emits.
/// </summary>
[Collection("UnmigratedProperty")]
[Trait("Category", "Integration")]
public sealed class PropertyEraserMissingTableTests(UnmigratedPropertyFixture fixture)
{
    [Fact]
    public async Task EraseAsync_WithMissingTable_ReturnsZeroAndLogsWarning()
    {
        var userId = Guid.NewGuid();
        var logger = new CapturingLogger<PropertyPersonalDataEraser>();

        using var scope = fixture.Services.CreateScope();
        var eraser = new PropertyPersonalDataEraser(
            scope.ServiceProvider.GetRequiredService<PropertyDbContext>(),
            scope.ServiceProvider.GetRequiredService<IBlobStore>(),
            logger);

        var result = await eraser.EraseAsync(new UserRef(userId), ErasureStrategy.Anonymize, CancellationToken.None);

        Assert.Equal(userId, result.UserId);
        Assert.Equal(0, result.RecordsAffected);

        var warning = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, warning.Level);
        Assert.Contains(userId.ToString(), warning.Message, StringComparison.Ordinal);
        Assert.NotNull(warning.Exception);
    }

    [Fact]
    public async Task EraseHouseholdAsync_WithMissingTable_ReturnsZeroAndLogsWarning()
    {
        var householdId = Guid.NewGuid();
        var logger = new CapturingLogger<PropertyPersonalDataEraser>();

        using var scope = fixture.Services.CreateScope();
        var eraser = new PropertyPersonalDataEraser(
            scope.ServiceProvider.GetRequiredService<PropertyDbContext>(),
            scope.ServiceProvider.GetRequiredService<IBlobStore>(),
            logger);

        var affected = await eraser.EraseHouseholdAsync(householdId, CancellationToken.None);

        Assert.Equal(0, affected);

        var warning = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, warning.Level);
        Assert.Contains(householdId.ToString(), warning.Message, StringComparison.Ordinal);
        Assert.NotNull(warning.Exception);
    }
}
