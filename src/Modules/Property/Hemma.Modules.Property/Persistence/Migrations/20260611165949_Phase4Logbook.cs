using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hemma.Modules.Property.Persistence.Migrations;

/// <inheritdoc />
public partial class Phase4Logbook : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "history_entries",
            schema: "property",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                household_id = table.Column<Guid>(type: "uuid", nullable: false),
                date = table.Column<DateOnly>(type: "date", nullable: false),
                title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                area = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                cost_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                cost_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                source_project_id = table.Column<Guid>(type: "uuid", nullable: true),
                source_maintenance_occurrence_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_history_entries", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "history_entry_photos",
            schema: "property",
            columns: table => new
            {
                history_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                blob_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                blob_container = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                size = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_history_entry_photos", x => new { x.history_entry_id, x.blob_key });
                table.ForeignKey(
                    name: "fk_history_entry_photos_history_entries_history_entry_id",
                    column: x => x.history_entry_id,
                    principalSchema: "property",
                    principalTable: "history_entries",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_history_entries_household_id_area",
            schema: "property",
            table: "history_entries",
            columns: new[] { "household_id", "area" });

        migrationBuilder.CreateIndex(
            name: "ix_history_entries_household_id_date",
            schema: "property",
            table: "history_entries",
            columns: new[] { "household_id", "date" });

        migrationBuilder.CreateIndex(
            name: "ix_history_entries_household_id_type",
            schema: "property",
            table: "history_entries",
            columns: new[] { "household_id", "type" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "history_entry_photos",
            schema: "property");

        migrationBuilder.DropTable(
            name: "history_entries",
            schema: "property");
    }
}
