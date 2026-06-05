using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hemma.Modules.Economy.Persistence.Migrations;

/// <inheritdoc />
public partial class AddEconomyTransactions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
            migrationBuilder.CreateTable(
                name: "transactions",
                schema: "economy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    household_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    occurred_on = table.Column<DateOnly>(type: "date", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    receipt_blob_container = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    receipt_blob_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    transfer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_transfer_outflow = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transactions", x => x.id);
                    table.ForeignKey(
                        name: "fk_transactions_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "economy",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_transactions_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "economy",
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "transfers",
                schema: "economy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    household_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outflow_transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inflow_transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transfers", x => x.id);
                    table.ForeignKey(
                        name: "fk_transfers_transactions_inflow_transaction_id",
                        column: x => x.inflow_transaction_id,
                        principalSchema: "economy",
                        principalTable: "transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_transfers_transactions_outflow_transaction_id",
                        column: x => x.outflow_transaction_id,
                        principalSchema: "economy",
                        principalTable: "transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_transactions_account_id",
                schema: "economy",
                table: "transactions",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_transactions_category_id",
                schema: "economy",
                table: "transactions",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_transactions_household_id_occurred_on",
                schema: "economy",
                table: "transactions",
                columns: new[] { "household_id", "occurred_on" });

            migrationBuilder.CreateIndex(
                name: "ix_transactions_payer_id",
                schema: "economy",
                table: "transactions",
                column: "payer_id");

            migrationBuilder.CreateIndex(
                name: "ix_transactions_transfer_id",
                schema: "economy",
                table: "transactions",
                column: "transfer_id");

            migrationBuilder.CreateIndex(
                name: "ix_transfers_household_id_inflow_transaction_id",
                schema: "economy",
                table: "transfers",
                columns: new[] { "household_id", "inflow_transaction_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_transfers_household_id_outflow_transaction_id",
                schema: "economy",
                table: "transfers",
                columns: new[] { "household_id", "outflow_transaction_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_transfers_inflow_transaction_id",
                schema: "economy",
                table: "transfers",
                column: "inflow_transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_transfers_outflow_transaction_id",
                schema: "economy",
                table: "transfers",
                column: "outflow_transaction_id");

    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
            migrationBuilder.DropTable(
                name: "transfers",
                schema: "economy");

            migrationBuilder.DropTable(
                name: "transactions",
                schema: "economy");
    }
}
