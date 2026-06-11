using Hemma.Modules.Economy.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hemma.Modules.Economy.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(EconomyDbContext))]
[Migration("20260608120000_AddSubscriptionTrialReminderMarker")]
public partial class AddSubscriptionTrialReminderMarker : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateOnly>(
            name: "trial_reminder_sent_for_trial_ends_on",
            schema: "economy",
            table: "subscriptions",
            type: "date",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "trial_reminder_sent_for_trial_ends_on",
            schema: "economy",
            table: "subscriptions");
    }
}
