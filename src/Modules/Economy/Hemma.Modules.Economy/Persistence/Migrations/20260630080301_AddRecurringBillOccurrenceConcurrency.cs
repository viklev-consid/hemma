using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hemma.Modules.Economy.Persistence.Migrations;

/// <inheritdoc />
public partial class AddRecurringBillOccurrenceConcurrency : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "settlement_version",
            schema: "economy",
            table: "recurring_bill_occurrences",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "settlement_version",
            schema: "economy",
            table: "recurring_bill_occurrences");
    }
}
