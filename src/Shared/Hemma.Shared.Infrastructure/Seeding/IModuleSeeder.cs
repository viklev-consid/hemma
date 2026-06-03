namespace Hemma.Shared.Infrastructure.Seeding;

public interface IModuleSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
