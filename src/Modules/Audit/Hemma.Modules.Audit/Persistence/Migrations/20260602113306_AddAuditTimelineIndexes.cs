using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hemma.Modules.Audit.Persistence.Migrations;

/// <inheritdoc />
public partial class AddAuditTimelineIndexes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "ix_audit_entries_actor_id_occurred_at",
            schema: "audit",
            table: "audit_entries",
            columns: new[] { "actor_id", "occurred_at" });

        migrationBuilder.CreateIndex(
            name: "ix_audit_entries_household_id_occurred_at",
            schema: "audit",
            table: "audit_entries",
            columns: new[] { "household_id", "occurred_at" });

        migrationBuilder.CreateIndex(
            name: "ix_audit_entries_resource_id_occurred_at",
            schema: "audit",
            table: "audit_entries",
            columns: new[] { "resource_id", "occurred_at" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_audit_entries_actor_id_occurred_at",
            schema: "audit",
            table: "audit_entries");

        migrationBuilder.DropIndex(
            name: "ix_audit_entries_household_id_occurred_at",
            schema: "audit",
            table: "audit_entries");

        migrationBuilder.DropIndex(
            name: "ix_audit_entries_resource_id_occurred_at",
            schema: "audit",
            table: "audit_entries");
    }
}
