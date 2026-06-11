using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hemma.Modules.Households.Persistence.Migrations;

/// <inheritdoc />
public partial class AddHouseholds : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "households");

        migrationBuilder.CreateTable(
            name: "household_invitations",
            schema: "households",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                household_id = table.Column<Guid>(type: "uuid", nullable: false),
                email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                token_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                invited_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                invited_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                accepted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                accepted_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                revoked_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                is_pending = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_household_invitations", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "households",
            schema: "households",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_households", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "household_memberships",
            schema: "households",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                household_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                removed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                removed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                is_anonymized = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_household_memberships", x => x.id);
                table.ForeignKey(
                    name: "fk_household_memberships_households_household_id",
                    column: x => x.household_id,
                    principalSchema: "households",
                    principalTable: "households",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_household_invitations_household_id_email",
            schema: "households",
            table: "household_invitations",
            columns: new[] { "household_id", "email" },
            unique: true,
            filter: "is_pending = true");

        migrationBuilder.CreateIndex(
            name: "ix_household_invitations_token_hash",
            schema: "households",
            table: "household_invitations",
            column: "token_hash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_household_memberships_household_id_user_id",
            schema: "households",
            table: "household_memberships",
            columns: new[] { "household_id", "user_id" },
            unique: true,
            filter: "is_active = true");

        migrationBuilder.CreateIndex(
            name: "ix_household_memberships_user_id",
            schema: "households",
            table: "household_memberships",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ix_households_slug",
            schema: "households",
            table: "households",
            column: "slug",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "household_invitations",
            schema: "households");

        migrationBuilder.DropTable(
            name: "household_memberships",
            schema: "households");

        migrationBuilder.DropTable(
            name: "households",
            schema: "households");
    }
}
