using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hemma.Modules.Property.Persistence.Migrations;

/// <inheritdoc />
public partial class Phase1ProjectCore : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.EnsureSchema(
            name: "property");

        _ = migrationBuilder.CreateTable(
            name: "projects",
            schema: "property",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                household_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                area = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                target_start_date = table.Column<DateOnly>(type: "date", nullable: true),
                target_end_date = table.Column<DateOnly>(type: "date", nullable: true),
                budget_estimate_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                budget_estimate_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
            },
            constraints: table => _ = table.PrimaryKey("pk_projects", x => x.id));

        _ = migrationBuilder.CreateTable(
            name: "project_attachments",
            schema: "property",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                project_id = table.Column<Guid>(type: "uuid", nullable: false),
                blob_container = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                blob_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                size = table.Column<long>(type: "bigint", nullable: false),
            },
            constraints: table =>
            {
                _ = table.PrimaryKey("pk_project_attachments", x => x.id);
                _ = table.ForeignKey(
                    name: "fk_project_attachments_projects_project_id",
                    column: x => x.project_id,
                    principalTable: "projects",
                    principalColumn: "id",
                    principalSchema: "property",
                    onDelete: ReferentialAction.Cascade);
            });

        _ = migrationBuilder.CreateTable(
            name: "project_links",
            schema: "property",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                project_id = table.Column<Guid>(type: "uuid", nullable: false),
                label = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
            },
            constraints: table =>
            {
                _ = table.PrimaryKey("pk_project_links", x => x.id);
                _ = table.ForeignKey(
                    name: "fk_project_links_projects_project_id",
                    column: x => x.project_id,
                    principalTable: "projects",
                    principalColumn: "id",
                    principalSchema: "property",
                    onDelete: ReferentialAction.Cascade);
            });

        _ = migrationBuilder.CreateTable(
            name: "project_tasks",
            schema: "property",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                project_id = table.Column<Guid>(type: "uuid", nullable: false),
                title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                estimate_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                estimate_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                assignee_id = table.Column<Guid>(type: "uuid", nullable: true),
                due_date = table.Column<DateOnly>(type: "date", nullable: true),
                sort_order = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                _ = table.PrimaryKey("pk_project_tasks", x => x.id);
                _ = table.ForeignKey(
                    name: "fk_project_tasks_projects_project_id",
                    column: x => x.project_id,
                    principalTable: "projects",
                    principalColumn: "id",
                    principalSchema: "property",
                    onDelete: ReferentialAction.Cascade);
            });

        _ = migrationBuilder.CreateIndex(
            name: "ix_project_attachments_project_id",
            table: "project_attachments",
            column: "project_id",
            schema: "property");

        _ = migrationBuilder.CreateIndex(
            name: "ix_project_links_project_id",
            table: "project_links",
            column: "project_id",
            schema: "property");

        _ = migrationBuilder.CreateIndex(
            name: "ix_project_tasks_project_id_sort_order",
            table: "project_tasks",
            columns: ["project_id", "sort_order"],
            schema: "property");

        _ = migrationBuilder.CreateIndex(
            name: "ix_projects_household_id",
            table: "projects",
            column: "household_id",
            schema: "property");

        _ = migrationBuilder.CreateIndex(
            name: "ix_projects_household_id_area",
            table: "projects",
            columns: ["household_id", "area"],
            schema: "property");

        _ = migrationBuilder.CreateIndex(
            name: "ix_projects_household_id_status",
            table: "projects",
            columns: ["household_id", "status"],
            schema: "property");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.DropTable(
            name: "project_attachments",
            schema: "property");

        _ = migrationBuilder.DropTable(
            name: "project_links",
            schema: "property");

        _ = migrationBuilder.DropTable(
            name: "project_tasks",
            schema: "property");

        _ = migrationBuilder.DropTable(
            name: "projects",
            schema: "property");
    }
}
