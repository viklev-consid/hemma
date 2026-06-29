using Hemma.Modules.Property.Domain;
using Hemma.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Property.Persistence;

public sealed class PropertyDbContext(DbContextOptions<PropertyDbContext> options) : ModuleDbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<MaintenancePlan> MaintenancePlans => Set<MaintenancePlan>();
    public DbSet<MaintenanceOccurrence> MaintenanceOccurrences => Set<MaintenanceOccurrence>();
    public DbSet<HistoryEntry> HistoryEntries => Set<HistoryEntry>();
    public DbSet<PropertyArea> Areas => Set<PropertyArea>();
    public DbSet<PropertyTag> Tags => Set<PropertyTag>();
    public DbSet<PropertyTagAssignment> TagAssignments => Set<PropertyTagAssignment>();
    public DbSet<PropertyIssue> Issues => Set<PropertyIssue>();
    public DbSet<PropertyActivityEvent> ActivityEvents => Set<PropertyActivityEvent>();
    public DbSet<PropertyBlobDeletion> PendingBlobDeletions => Set<PropertyBlobDeletion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("property");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PropertyDbContext).Assembly);
    }
}
