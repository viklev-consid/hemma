using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hemma.Modules.Economy.Persistence.Migrations;

/// <inheritdoc />
public partial class AddAnalyticsTransactionIndex : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "ix_transactions_household_id_occurred_on_category_id_kind",
            schema: "economy",
            table: "transactions",
            columns: new[] { "household_id", "occurred_on", "category_id", "kind" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_transactions_household_id_occurred_on_category_id_kind",
            schema: "economy",
            table: "transactions");
    }
}
