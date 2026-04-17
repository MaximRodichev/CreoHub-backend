using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreoHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderItemFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderItemFiles",
                columns: table => new
                {
                    ContentFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderItemId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItemFiles", x => new { x.OrderItemId, x.ContentFileId });
                    table.ForeignKey(
                        name: "FK_OrderItemFiles_ContentFiles_ContentFileId",
                        column: x => x.ContentFileId,
                        principalTable: "ContentFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItemFiles_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemFiles_ContentFileId",
                table: "OrderItemFiles",
                column: "ContentFileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderItemFiles");
        }
    }
}
