using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Hemma.Modules.Economy.Persistence;

/// <summary>
/// Used only by EF Core tooling (dotnet ef migrations). Not used at runtime.
/// </summary>
public sealed class EconomyDbContextFactory : IDesignTimeDbContextFactory<EconomyDbContext>
{
    public EconomyDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EconomyDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=hemma;Username=postgres;Password=postgres",
            builder => builder.MigrationsHistoryTable("__ef_migrations_history", "economy"));
        return new EconomyDbContext(optionsBuilder.Options);
    }
}
