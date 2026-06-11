using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Hemma.Modules.Households.Persistence;

#nullable disable

namespace Hemma.Modules.Households.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(HouseholdsDbContext))]
[Migration("20260521211618_AnonymizeHouseholdUserReferences")]
public partial class AnonymizeHouseholdUserReferences : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<Guid>(
            name: "invited_by_user_id",
            schema: "households",
            table: "household_invitations",
            type: "uuid",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AlterColumn<Guid>(
            name: "user_id",
            schema: "households",
            table: "household_memberships",
            type: "uuid",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uuid");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM households.household_invitations
            WHERE invited_by_user_id IS NULL;
            """);

        migrationBuilder.Sql("""
            DELETE FROM households.household_memberships
            WHERE user_id IS NULL;
            """);

        migrationBuilder.AlterColumn<Guid>(
            name: "invited_by_user_id",
            schema: "households",
            table: "household_invitations",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "user_id",
            schema: "households",
            table: "household_memberships",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);
    }
}
