using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hemma.Modules.Property.Persistence.Migrations;

/// <inheritdoc />
public partial class HardenPropertyPrivacyAndIndexes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "ix_project_tasks_assignee_id",
            schema: "property",
            table: "project_tasks",
            column: "assignee_id");

        migrationBuilder.CreateIndex(
            name: "ix_activity_events_actor_id",
            schema: "property",
            table: "activity_events",
            column: "actor_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_project_tasks_assignee_id",
            schema: "property",
            table: "project_tasks");

        migrationBuilder.DropIndex(
            name: "ix_activity_events_actor_id",
            schema: "property",
            table: "activity_events");
    }
}
