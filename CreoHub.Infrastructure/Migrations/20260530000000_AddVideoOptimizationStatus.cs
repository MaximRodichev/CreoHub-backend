using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreoHub.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddVideoOptimizationStatus : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name:         "VideoOptimizationStatus",
            table:        "StorageObjects",
            type:         "text",
            nullable:     false,
            defaultValue: "None");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name:  "VideoOptimizationStatus",
            table: "StorageObjects");
    }
}
