using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hemma.Modules.Economy.Persistence.Migrations;

/// <inheritdoc />
public partial class AddEconomyCore : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "economy");

        migrationBuilder.CreateTable(
            name: "accounts",
            schema: "economy",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                household_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                opening_balance_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                opening_balance_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_accounts", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "budgets",
            schema: "economy",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                household_id = table.Column<Guid>(type: "uuid", nullable: false),
                period_starts_on = table.Column<DateOnly>(type: "date", nullable: false),
                period_ends_on = table.Column<DateOnly>(type: "date", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_budgets", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "categories",
            schema: "economy",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                household_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                parent_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                budgetable = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_categories", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "economy_settings",
            schema: "economy",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                household_id = table.Column<Guid>(type: "uuid", nullable: false),
                cycle_start_day = table.Column<int>(type: "integer", nullable: false),
                default_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                created_on = table.Column<DateOnly>(type: "date", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_economy_settings", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "budget_lines",
            schema: "economy",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                budget_id = table.Column<Guid>(type: "uuid", nullable: false),
                category_id = table.Column<Guid>(type: "uuid", nullable: false),
                amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_budget_lines", x => x.id);
                table.ForeignKey(
                    name: "fk_budget_lines_budgets_budget_id",
                    column: x => x.budget_id,
                    principalSchema: "economy",
                    principalTable: "budgets",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_accounts_household_id_name",
            schema: "economy",
            table: "accounts",
            columns: new[] { "household_id", "name" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_budget_lines_budget_id_category_id",
            schema: "economy",
            table: "budget_lines",
            columns: new[] { "budget_id", "category_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_budgets_household_id_period_starts_on",
            schema: "economy",
            table: "budgets",
            columns: new[] { "household_id", "period_starts_on" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_categories_household_id",
            schema: "economy",
            table: "categories",
            column: "household_id");

        migrationBuilder.CreateIndex(
            name: "ix_categories_household_id_parent_category_id_name",
            schema: "economy",
            table: "categories",
            columns: new[] { "household_id", "parent_category_id", "name" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_economy_settings_household_id",
            schema: "economy",
            table: "economy_settings",
            column: "household_id",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "accounts",
            schema: "economy");

        migrationBuilder.DropTable(
            name: "budget_lines",
            schema: "economy");

        migrationBuilder.DropTable(
            name: "categories",
            schema: "economy");

        migrationBuilder.DropTable(
            name: "economy_settings",
            schema: "economy");

        migrationBuilder.DropTable(
            name: "budgets",
            schema: "economy");
    }
}
