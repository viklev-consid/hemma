using Hemma.Modules.Property.Domain;
using Hemma.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Property.Persistence;

public sealed class PropertyDbContext(DbContextOptions<PropertyDbContext> options) : ModuleDbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("property");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PropertyDbContext).Assembly);
    }
}
