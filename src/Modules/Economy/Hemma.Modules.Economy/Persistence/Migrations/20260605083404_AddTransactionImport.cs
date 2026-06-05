using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hemma.Modules.Economy.Persistence.Migrations;

/// <inheritdoc />
public partial class AddTransactionImport : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "import_fingerprint",
            schema: "economy",
            table: "transactions",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "categorization_rules",
            schema: "economy",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                household_id = table.Column<Guid>(type: "uuid", nullable: false),
                match = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                pattern = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                target_category_id = table.Column<Guid>(type: "uuid", nullable: false),
                enabled = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_categorization_rules", x => x.id);
                table.ForeignKey(
                    name: "fk_categorization_rules_categories_target_category_id",
                    column: x => x.target_category_id,
                    principalSchema: "economy",
                    principalTable: "categories",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_transactions_account_id_import_fingerprint",
            schema: "economy",
            table: "transactions",
            columns: new[] { "account_id", "import_fingerprint" },
            unique: true,
            filter: "\"import_fingerprint\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "ix_categorization_rules_household_id",
            schema: "economy",
            table: "categorization_rules",
            column: "household_id");

        migrationBuilder.CreateIndex(
            name: "ix_categorization_rules_household_id_match_pattern",
            schema: "economy",
            table: "categorization_rules",
            columns: new[] { "household_id", "match", "pattern" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_categorization_rules_target_category_id",
            schema: "economy",
            table: "categorization_rules",
            column: "target_category_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "categorization_rules",
            schema: "economy");

        migrationBuilder.DropIndex(
            name: "ix_transactions_account_id_import_fingerprint",
            schema: "economy",
            table: "transactions");

        migrationBuilder.DropColumn(
            name: "import_fingerprint",
            schema: "economy",
            table: "transactions");
    }
}
