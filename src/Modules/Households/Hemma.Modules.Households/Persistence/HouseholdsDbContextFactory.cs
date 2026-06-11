using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Hemma.Modules.Households.Persistence;

/// <summary>
/// Used only by EF Core tooling (dotnet ef migrations). Not used at runtime.
/// </summary>
public sealed class HouseholdsDbContextFactory : IDesignTimeDbContextFactory<HouseholdsDbContext>
{
    public HouseholdsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<HouseholdsDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=hemma;Username=postgres;Password=postgres",
            b => b.MigrationsHistoryTable("__ef_migrations_history", "households"));
        return new HouseholdsDbContext(optionsBuilder.Options);
    }
}
