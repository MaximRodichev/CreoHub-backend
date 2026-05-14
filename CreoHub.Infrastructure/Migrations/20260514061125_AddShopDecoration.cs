using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreoHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShopDecoration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BannerId",
                table: "Shops",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LogoId",
                table: "Shops",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shops_BannerId",
                table: "Shops",
                column: "BannerId");

            migrationBuilder.CreateIndex(
                name: "IX_Shops_LogoId",
                table: "Shops",
                column: "LogoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Shops_StorageObjects_BannerId",
                table: "Shops",
                column: "BannerId",
                principalTable: "StorageObjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Shops_StorageObjects_LogoId",
                table: "Shops",
                column: "LogoId",
                principalTable: "StorageObjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Shops_StorageObjects_BannerId",
                table: "Shops");

            migrationBuilder.DropForeignKey(
                name: "FK_Shops_StorageObjects_LogoId",
                table: "Shops");

            migrationBuilder.DropIndex(
                name: "IX_Shops_BannerId",
                table: "Shops");

            migrationBuilder.DropIndex(
                name: "IX_Shops_LogoId",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "BannerId",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "LogoId",
                table: "Shops");
        }
    }
}
