using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hemma.Modules.Economy.Persistence.Migrations;

/// <inheritdoc />
public partial class Phase2ProjectLinking : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "project_id",
            schema: "economy",
            table: "transactions",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_transactions_household_id_project_id",
            schema: "economy",
            table: "transactions",
            columns: new[] { "household_id", "project_id" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_transactions_household_id_project_id",
            schema: "economy",
            table: "transactions");

        migrationBuilder.DropColumn(
            name: "project_id",
            schema: "economy",
            table: "transactions");
    }
}
