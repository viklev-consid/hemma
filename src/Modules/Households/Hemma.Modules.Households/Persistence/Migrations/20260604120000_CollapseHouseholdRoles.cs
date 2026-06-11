using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Hemma.Modules.Households.Persistence;

#nullable disable

namespace Hemma.Modules.Households.Persistence.Migrations;

[DbContext(typeof(HouseholdsDbContext))]
[Migration("20260604120000_CollapseHouseholdRoles")]
public partial class CollapseHouseholdRoles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE households.household_memberships
            SET role = 'member'
            WHERE role IN ('admin', 'viewer');
            """);

        migrationBuilder.Sql("""
            UPDATE households.household_invitations
            SET role = 'member'
            WHERE role IN ('admin', 'viewer');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // The old admin/viewer roles cannot be reconstructed once collapsed to member.
    }
}
