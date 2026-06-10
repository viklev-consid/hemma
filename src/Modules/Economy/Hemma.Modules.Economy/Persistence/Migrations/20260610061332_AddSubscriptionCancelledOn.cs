using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hemma.Modules.Economy.Persistence.Migrations;

/// <inheritdoc />
public partial class AddSubscriptionCancelledOn : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateOnly>(
            name: "cancelled_on",
            schema: "economy",
            table: "subscriptions",
            type: "date",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "cancelled_on",
            schema: "economy",
            table: "subscriptions");
    }
}
