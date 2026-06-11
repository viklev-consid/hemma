using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Hemma.Modules.Property.Persistence;

/// <summary>
/// Used only by EF Core tooling (dotnet ef migrations). Not used at runtime.
/// </summary>
public sealed class PropertyDbContextFactory : IDesignTimeDbContextFactory<PropertyDbContext>
{
    public PropertyDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PropertyDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=hemma;Username=postgres;Password=postgres",
            builder => builder.MigrationsHistoryTable("__ef_migrations_history", "property"));
        return new PropertyDbContext(optionsBuilder.Options);
    }
}
