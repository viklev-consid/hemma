using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hemma.Modules.Property.Persistence.Migrations;

/// <inheritdoc />
public partial class Phase6PropertyIssues : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "issues",
            schema: "property",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                household_id = table.Column<Guid>(type: "uuid", nullable: false),
                title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                area_id = table.Column<Guid>(type: "uuid", nullable: true),
                severity = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                reported_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                due_date = table.Column<DateOnly>(type: "date", nullable: true),
                resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                linked_project_id = table.Column<Guid>(type: "uuid", nullable: true),
                linked_maintenance_plan_id = table.Column<Guid>(type: "uuid", nullable: true),
                linked_maintenance_occurrence_id = table.Column<Guid>(type: "uuid", nullable: true),
                notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_issues", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_issues_household_id",
            schema: "property",
            table: "issues",
            column: "household_id");

        migrationBuilder.CreateIndex(
            name: "ix_issues_household_id_area_id",
            schema: "property",
            table: "issues",
            columns: new[] { "household_id", "area_id" });

        migrationBuilder.CreateIndex(
            name: "ix_issues_household_id_due_date",
            schema: "property",
            table: "issues",
            columns: new[] { "household_id", "due_date" });

        migrationBuilder.CreateIndex(
            name: "ix_issues_household_id_linked_project_id",
            schema: "property",
            table: "issues",
            columns: new[] { "household_id", "linked_project_id" });

        migrationBuilder.CreateIndex(
            name: "ix_issues_household_id_severity",
            schema: "property",
            table: "issues",
            columns: new[] { "household_id", "severity" });

        migrationBuilder.CreateIndex(
            name: "ix_issues_household_id_status",
            schema: "property",
            table: "issues",
            columns: new[] { "household_id", "status" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "issues",
            schema: "property");
    }
}
