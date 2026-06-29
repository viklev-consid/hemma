using Hemma.Modules.Households.Domain;
using Hemma.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Households.Persistence;

public sealed class HouseholdsDbContext(DbContextOptions<HouseholdsDbContext> options) : ModuleDbContext(options)
{
    public DbSet<Household> Households => Set<Household>();
    public DbSet<HouseholdMembership> Memberships => Set<HouseholdMembership>();
    public DbSet<HouseholdInvitation> Invitations => Set<HouseholdInvitation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("households");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HouseholdsDbContext).Assembly);
    }
}
