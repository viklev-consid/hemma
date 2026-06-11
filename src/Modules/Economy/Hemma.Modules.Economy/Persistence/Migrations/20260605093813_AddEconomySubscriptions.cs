using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hemma.Modules.Economy.Persistence.Migrations;

/// <inheritdoc />
public partial class AddEconomySubscriptions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "subscriptions",
            schema: "economy",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                household_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                cadence_frequency = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                cadence_interval = table.Column<int>(type: "integer", nullable: false),
                cadence_charge_day = table.Column<int>(type: "integer", nullable: false),
                expected_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                expected_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                lifecycle_state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                trial_ends_on = table.Column<DateOnly>(type: "date", nullable: true),
                account_id = table.Column<Guid>(type: "uuid", nullable: true),
                starts_on = table.Column<DateOnly>(type: "date", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_subscriptions", x => x.id);
                table.ForeignKey(
                    name: "fk_subscriptions_accounts_account_id",
                    column: x => x.account_id,
                    principalSchema: "economy",
                    principalTable: "accounts",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_transactions_subscription_id",
            schema: "economy",
            table: "transactions",
            column: "subscription_id");

        migrationBuilder.CreateIndex(
            name: "ix_subscriptions_account_id",
            schema: "economy",
            table: "subscriptions",
            column: "account_id");

        migrationBuilder.CreateIndex(
            name: "ix_subscriptions_household_id_name",
            schema: "economy",
            table: "subscriptions",
            columns: new[] { "household_id", "name" });

        migrationBuilder.CreateIndex(
            name: "ix_subscriptions_household_id_trial_ends_on",
            schema: "economy",
            table: "subscriptions",
            columns: new[] { "household_id", "trial_ends_on" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "subscriptions",
            schema: "economy");

        migrationBuilder.DropIndex(
            name: "ix_transactions_subscription_id",
            schema: "economy",
            table: "transactions");
    }
}
