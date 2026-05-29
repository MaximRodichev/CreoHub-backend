using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CreoHub.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddProductEditHistory : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ProductEditHistories",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ProductId    = table.Column<int>(type: "integer",   nullable: false),
                EditorId     = table.Column<Guid>(type: "uuid",      nullable: true),
                CreatedAt    = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                SnapshotJson = table.Column<string>(type: "text",    nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProductEditHistories", x => x.Id);
                table.ForeignKey(
                    name:       "FK_ProductEditHistories_Products_ProductId",
                    column:     x => x.ProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete:   ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name:    "IX_ProductEditHistories_CreatedAt",
            table:   "ProductEditHistories",
            column:  "CreatedAt");

        migrationBuilder.CreateIndex(
            name:    "IX_ProductEditHistories_ProductId",
            table:   "ProductEditHistories",
            column:  "ProductId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ProductEditHistories");
    }
}
