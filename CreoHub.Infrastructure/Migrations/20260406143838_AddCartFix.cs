using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreoHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCartFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CartItemFiles_CartItems_CartItemId",
                table: "CartItemFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Carts_Users_UserId",
                table: "Carts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CartItems",
                table: "CartItems");

            migrationBuilder.AddColumn<Guid>(
                name: "CartItemCartId",
                table: "CartItemFiles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "CartItemProductId",
                table: "CartItemFiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_CartItems",
                table: "CartItems",
                columns: new[] { "CartId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_CartItemFiles_CartItemCartId_CartItemProductId",
                table: "CartItemFiles",
                columns: new[] { "CartItemCartId", "CartItemProductId" });

            migrationBuilder.AddForeignKey(
                name: "FK_CartItemFiles_CartItems_CartItemCartId_CartItemProductId",
                table: "CartItemFiles",
                columns: new[] { "CartItemCartId", "CartItemProductId" },
                principalTable: "CartItems",
                principalColumns: new[] { "CartId", "ProductId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Carts_Users_UserId",
                table: "Carts",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CartItemFiles_CartItems_CartItemCartId_CartItemProductId",
                table: "CartItemFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Carts_Users_UserId",
                table: "Carts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CartItems",
                table: "CartItems");

            migrationBuilder.DropIndex(
                name: "IX_CartItemFiles_CartItemCartId_CartItemProductId",
                table: "CartItemFiles");

            migrationBuilder.DropColumn(
                name: "CartItemCartId",
                table: "CartItemFiles");

            migrationBuilder.DropColumn(
                name: "CartItemProductId",
                table: "CartItemFiles");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CartItems",
                table: "CartItems",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CartItemFiles_CartItems_CartItemId",
                table: "CartItemFiles",
                column: "CartItemId",
                principalTable: "CartItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Carts_Users_UserId",
                table: "Carts",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
