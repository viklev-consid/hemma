using Hemma.Modules.Households.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hemma.Modules.Households.Persistence.Migrations;

[DbContext(typeof(HouseholdsDbContext))]
[Migration("20260602120000_AddHouseholdOwnerMutationConcurrency")]
public partial class AddHouseholdOwnerMutationConcurrency : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "owner_mutation_version",
            schema: "households",
            table: "households",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "owner_mutation_version",
            schema: "households",
            table: "households");
    }
}
