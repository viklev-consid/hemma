using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hemma.Modules.Property.Persistence.Migrations;

/// <inheritdoc />
public partial class Phase7SnoozeOverdue : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateOnly>(
            name: "original_due_date",
            schema: "property",
            table: "maintenance_occurrences",
            type: "date",
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE property.maintenance_occurrences
            SET original_due_date = due_date
            WHERE original_due_date IS NULL;
            """);

        migrationBuilder.AlterColumn<DateOnly>(
            name: "original_due_date",
            schema: "property",
            table: "maintenance_occurrences",
            type: "date",
            nullable: false,
            oldClrType: typeof(DateOnly),
            oldType: "date",
            oldNullable: true);

        migrationBuilder.AddColumn<string>(
            name: "snooze_reason",
            schema: "property",
            table: "maintenance_occurrences",
            type: "character varying(2000)",
            maxLength: 2000,
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "snoozed_at",
            schema: "property",
            table: "maintenance_occurrences",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateOnly>(
            name: "snoozed_until",
            schema: "property",
            table: "maintenance_occurrences",
            type: "date",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "original_due_date",
            schema: "property",
            table: "maintenance_occurrences");

        migrationBuilder.DropColumn(
            name: "snooze_reason",
            schema: "property",
            table: "maintenance_occurrences");

        migrationBuilder.DropColumn(
            name: "snoozed_at",
            schema: "property",
            table: "maintenance_occurrences");

        migrationBuilder.DropColumn(
            name: "snoozed_until",
            schema: "property",
            table: "maintenance_occurrences");
    }
}
