using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hemma.Modules.Property.Persistence.Migrations;

/// <inheritdoc />
public partial class Phase8PropertyActivity : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "activity_events",
            schema: "property",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                household_id = table.Column<Guid>(type: "uuid", nullable: false),
                occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                verb = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                target_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                target_id = table.Column<Guid>(type: "uuid", nullable: false),
                summary = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                metadata = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb")
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_activity_events", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_activity_events_household_id_occurred_at",
            schema: "property",
            table: "activity_events",
            columns: new[] { "household_id", "occurred_at" });

        migrationBuilder.CreateIndex(
            name: "ix_activity_events_household_id_target_type_target_id",
            schema: "property",
            table: "activity_events",
            columns: new[] { "household_id", "target_type", "target_id" });

        migrationBuilder.CreateIndex(
            name: "ix_activity_events_household_id_verb",
            schema: "property",
            table: "activity_events",
            columns: new[] { "household_id", "verb" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "activity_events",
            schema: "property");
    }
}
