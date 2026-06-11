using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hemma.Modules.Property.Persistence.Migrations;

/// <inheritdoc />
public partial class Phase3Maintenance : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "maintenance_occurrences",
            schema: "property",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                household_id = table.Column<Guid>(type: "uuid", nullable: false),
                due_date = table.Column<DateOnly>(type: "date", nullable: false),
                status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                spawned_project_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_maintenance_occurrences", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "maintenance_plans",
            schema: "property",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                household_id = table.Column<Guid>(type: "uuid", nullable: false),
                title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                area = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                recurrence_unit = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                recurrence_interval = table.Column<int>(type: "integer", nullable: false),
                anchor_date = table.Column<DateOnly>(type: "date", nullable: false),
                lead_time_days = table.Column<int>(type: "integer", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_maintenance_plans", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_maintenance_occurrences_household_id_status_due_date",
            schema: "property",
            table: "maintenance_occurrences",
            columns: new[] { "household_id", "status", "due_date" });

        migrationBuilder.CreateIndex(
            name: "ix_maintenance_occurrences_plan_id_due_date",
            schema: "property",
            table: "maintenance_occurrences",
            columns: new[] { "plan_id", "due_date" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_maintenance_occurrences_plan_id_status",
            schema: "property",
            table: "maintenance_occurrences",
            columns: new[] { "plan_id", "status" });

        migrationBuilder.CreateIndex(
            name: "ix_maintenance_plans_household_id",
            schema: "property",
            table: "maintenance_plans",
            column: "household_id");

        migrationBuilder.CreateIndex(
            name: "ix_maintenance_plans_household_id_is_active",
            schema: "property",
            table: "maintenance_plans",
            columns: new[] { "household_id", "is_active" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "maintenance_occurrences",
            schema: "property");

        migrationBuilder.DropTable(
            name: "maintenance_plans",
            schema: "property");
    }
}
