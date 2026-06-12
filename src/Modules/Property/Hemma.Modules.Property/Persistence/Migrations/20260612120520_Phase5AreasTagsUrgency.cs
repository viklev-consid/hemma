using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hemma.Modules.Property.Persistence.Migrations;

/// <inheritdoc />
public partial class Phase5AreasTagsUrgency : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_projects_household_id_area",
            schema: "property",
            table: "projects");

        migrationBuilder.DropIndex(
            name: "ix_history_entries_household_id_area",
            schema: "property",
            table: "history_entries");

        migrationBuilder.DropColumn(
            name: "area",
            schema: "property",
            table: "projects");

        migrationBuilder.DropColumn(
            name: "area",
            schema: "property",
            table: "maintenance_plans");

        migrationBuilder.DropColumn(
            name: "area",
            schema: "property",
            table: "history_entries");

        migrationBuilder.AddColumn<Guid>(
            name: "area_id",
            schema: "property",
            table: "projects",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "priority",
            schema: "property",
            table: "projects",
            type: "character varying(16)",
            maxLength: 16,
            nullable: false,
            defaultValue: "Medium");

        migrationBuilder.AddColumn<Guid>(
            name: "area_id",
            schema: "property",
            table: "maintenance_plans",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "area_id",
            schema: "property",
            table: "history_entries",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "areas",
            schema: "property",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                household_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                sort_order = table.Column<int>(type: "integer", nullable: false),
                is_archived = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_areas", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "tag_assignments",
            schema: "property",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                household_id = table.Column<Guid>(type: "uuid", nullable: false),
                tag_id = table.Column<Guid>(type: "uuid", nullable: false),
                target_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                target_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_tag_assignments", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "tags",
            schema: "property",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                household_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                color = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                is_archived = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_tags", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_projects_household_id_area_id",
            schema: "property",
            table: "projects",
            columns: new[] { "household_id", "area_id" });

        migrationBuilder.CreateIndex(
            name: "ix_projects_household_id_priority",
            schema: "property",
            table: "projects",
            columns: new[] { "household_id", "priority" });

        migrationBuilder.CreateIndex(
            name: "ix_maintenance_plans_household_id_area_id",
            schema: "property",
            table: "maintenance_plans",
            columns: new[] { "household_id", "area_id" });

        migrationBuilder.CreateIndex(
            name: "ix_history_entries_household_id_area_id",
            schema: "property",
            table: "history_entries",
            columns: new[] { "household_id", "area_id" });

        migrationBuilder.CreateIndex(
            name: "ix_areas_household_id",
            schema: "property",
            table: "areas",
            column: "household_id");

        migrationBuilder.CreateIndex(
            name: "ix_areas_household_id_name",
            schema: "property",
            table: "areas",
            columns: new[] { "household_id", "name" });

        migrationBuilder.Sql("""
            CREATE UNIQUE INDEX ux_areas_household_id_name_ci
            ON property.areas (household_id, lower(name));
            """);

        migrationBuilder.CreateIndex(
            name: "ix_tag_assignments_household_id",
            schema: "property",
            table: "tag_assignments",
            column: "household_id");

        migrationBuilder.CreateIndex(
            name: "ix_tag_assignments_household_id_tag_id_target_type_target_id",
            schema: "property",
            table: "tag_assignments",
            columns: new[] { "household_id", "tag_id", "target_type", "target_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_tag_assignments_household_id_target_type_target_id",
            schema: "property",
            table: "tag_assignments",
            columns: new[] { "household_id", "target_type", "target_id" });

        migrationBuilder.CreateIndex(
            name: "ix_tags_household_id",
            schema: "property",
            table: "tags",
            column: "household_id");

        migrationBuilder.CreateIndex(
            name: "ix_tags_household_id_name",
            schema: "property",
            table: "tags",
            columns: new[] { "household_id", "name" });

        migrationBuilder.Sql("""
            CREATE UNIQUE INDEX ux_tags_household_id_name_ci
            ON property.tags (household_id, lower(name));
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "areas",
            schema: "property");

        migrationBuilder.DropTable(
            name: "tag_assignments",
            schema: "property");

        migrationBuilder.DropTable(
            name: "tags",
            schema: "property");

        migrationBuilder.DropIndex(
            name: "ix_projects_household_id_area_id",
            schema: "property",
            table: "projects");

        migrationBuilder.DropIndex(
            name: "ix_projects_household_id_priority",
            schema: "property",
            table: "projects");

        migrationBuilder.DropIndex(
            name: "ix_maintenance_plans_household_id_area_id",
            schema: "property",
            table: "maintenance_plans");

        migrationBuilder.DropIndex(
            name: "ix_history_entries_household_id_area_id",
            schema: "property",
            table: "history_entries");

        migrationBuilder.DropColumn(
            name: "area_id",
            schema: "property",
            table: "projects");

        migrationBuilder.DropColumn(
            name: "priority",
            schema: "property",
            table: "projects");

        migrationBuilder.DropColumn(
            name: "area_id",
            schema: "property",
            table: "maintenance_plans");

        migrationBuilder.DropColumn(
            name: "area_id",
            schema: "property",
            table: "history_entries");

        migrationBuilder.AddColumn<string>(
            name: "area",
            schema: "property",
            table: "projects",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "area",
            schema: "property",
            table: "maintenance_plans",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "area",
            schema: "property",
            table: "history_entries",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_projects_household_id_area",
            schema: "property",
            table: "projects",
            columns: new[] { "household_id", "area" });

        migrationBuilder.CreateIndex(
            name: "ix_history_entries_household_id_area",
            schema: "property",
            table: "history_entries",
            columns: new[] { "household_id", "area" });
    }
}
