using Hemma.Modules.Economy.Persistence;
using Hemma.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hemma.Modules.Economy.IntegrationTests;

[CollectionDefinition("EconomyModule")]
public sealed class EconomyModuleCollection : ICollectionFixture<EconomyApiFixture> { }

public sealed class EconomyApiFixture : ApiTestFixture
{
    protected override async Task MigrateAsync(IServiceProvider services)
    {
        await services.GetRequiredService<EconomyDbContext>().Database.MigrateAsync();
    }

    protected override string[] GetSchemasToReset() => ["economy"];
}
