using System.Xml.Linq;

namespace Hemma.Architecture.Tests;

[Trait("Category", "Architecture")]
public sealed class ScaffoldTemplateTests
{
    [Fact]
    public void ModuleTemplate_IncludesEfCoreDesignPackage()
    {
        var repositoryRoot = FindRepositoryRoot();
        var templateProject = new FileInfo(Path.Combine(
            repositoryRoot.FullName,
            "templates",
            "module",
            "ModuleName",
            "Hemma.Modules.ModuleName",
            "Hemma.Modules.ModuleName.csproj"));

        var document = XDocument.Load(templateProject.FullName);
        var package = document
            .Descendants("PackageReference")
            .SingleOrDefault(element => string.Equals(
                element.Attribute("Include")?.Value,
                "Microsoft.EntityFrameworkCore.Design",
                StringComparison.Ordinal));

        Assert.NotNull(package);

        var privateAssets = package.Element("PrivateAssets")?.Value;
        Assert.True(
            string.Equals(privateAssets, "all", StringComparison.Ordinal),
            "FAIL: The hemma-module template must include Microsoft.EntityFrameworkCore.Design " +
            "as a private direct dependency so generated modules can run dotnet ef migrations.");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Hemma.slnx")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find repository root from test output directory.");
    }
}
