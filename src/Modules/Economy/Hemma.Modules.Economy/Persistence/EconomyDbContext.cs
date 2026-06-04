using Microsoft.EntityFrameworkCore;
using Hemma.Shared.Infrastructure.Persistence;

namespace Hemma.Modules.Economy.Persistence;

public sealed class EconomyDbContext(DbContextOptions<EconomyDbContext> options) : ModuleDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("economy");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EconomyDbContext).Assembly);
    }
}
