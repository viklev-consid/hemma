using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hemma.Modules.Economy.Persistence.Migrations;

/// <inheritdoc />
public partial class AddEconomyNotificationPreferences : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "notification_preferences",
            schema: "economy",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                household_id = table.Column<Guid>(type: "uuid", nullable: false),
                budget_alerts_enabled = table.Column<bool>(type: "boolean", nullable: false),
                bill_alerts_enabled = table.Column<bool>(type: "boolean", nullable: false),
                trial_alerts_enabled = table.Column<bool>(type: "boolean", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_notification_preferences", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_notification_preferences_household_id",
            schema: "economy",
            table: "notification_preferences",
            column: "household_id",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "notification_preferences",
            schema: "economy");
    }
}
