using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hemma.Modules.Property.Persistence.Migrations;

/// <inheritdoc />
public partial class AddPendingPropertyBlobDeletions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "pending_blob_deletions",
            schema: "property",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                household_id = table.Column<Guid>(type: "uuid", nullable: false),
                blob_container = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                blob_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                last_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                attempt_count = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_pending_blob_deletions", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_pending_blob_deletions_blob_container_blob_key",
            schema: "property",
            table: "pending_blob_deletions",
            columns: new[] { "blob_container", "blob_key" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_pending_blob_deletions_household_id_created_at",
            schema: "property",
            table: "pending_blob_deletions",
            columns: new[] { "household_id", "created_at" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "pending_blob_deletions",
            schema: "property");
    }
}
