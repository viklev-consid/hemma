using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hemma.Modules.Economy.Persistence.Migrations;

/// <inheritdoc />
public partial class AddRecurringBills : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "is_pending",
            schema: "economy",
            table: "transactions",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateTable(
            name: "recurring_bills",
            schema: "economy",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                household_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                account_id = table.Column<Guid>(type: "uuid", nullable: false),
                category_id = table.Column<Guid>(type: "uuid", nullable: true),
                amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                cadence_frequency = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                cadence_interval = table.Column<int>(type: "integer", nullable: false),
                cadence_day_of_month = table.Column<int>(type: "integer", nullable: false),
                type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                direction = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                starts_on = table.Column<DateOnly>(type: "date", nullable: false),
                next_due_on = table.Column<DateOnly>(type: "date", nullable: false),
                note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_recurring_bills", x => x.id);
                table.ForeignKey(
                    name: "fk_recurring_bills_accounts_account_id",
                    column: x => x.account_id,
                    principalSchema: "economy",
                    principalTable: "accounts",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_recurring_bills_categories_category_id",
                    column: x => x.category_id,
                    principalSchema: "economy",
                    principalTable: "categories",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "recurring_bill_occurrences",
            schema: "economy",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                recurring_bill_id = table.Column<Guid>(type: "uuid", nullable: false),
                due_on = table.Column<DateOnly>(type: "date", nullable: false),
                state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                transaction_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_recurring_bill_occurrences", x => x.id);
                table.ForeignKey(
                    name: "fk_recurring_bill_occurrences_recurring_bills_recurring_bill_id",
                    column: x => x.recurring_bill_id,
                    principalSchema: "economy",
                    principalTable: "recurring_bills",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_recurring_bill_occurrences_transactions_transaction_id",
                    column: x => x.transaction_id,
                    principalSchema: "economy",
                    principalTable: "transactions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_recurring_bill_occurrences_recurring_bill_id_due_on",
            schema: "economy",
            table: "recurring_bill_occurrences",
            columns: new[] { "recurring_bill_id", "due_on" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_recurring_bill_occurrences_transaction_id",
            schema: "economy",
            table: "recurring_bill_occurrences",
            column: "transaction_id");

        migrationBuilder.CreateIndex(
            name: "ix_recurring_bills_account_id",
            schema: "economy",
            table: "recurring_bills",
            column: "account_id");

        migrationBuilder.CreateIndex(
            name: "ix_recurring_bills_category_id",
            schema: "economy",
            table: "recurring_bills",
            column: "category_id");

        migrationBuilder.CreateIndex(
            name: "ix_recurring_bills_household_id_next_due_on",
            schema: "economy",
            table: "recurring_bills",
            columns: new[] { "household_id", "next_due_on" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "recurring_bill_occurrences",
            schema: "economy");

        migrationBuilder.DropTable(
            name: "recurring_bills",
            schema: "economy");

        migrationBuilder.DropColumn(
            name: "is_pending",
            schema: "economy",
            table: "transactions");
    }
}
