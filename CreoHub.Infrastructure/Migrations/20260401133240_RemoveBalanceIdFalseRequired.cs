using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreoHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBalanceIdFalseRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Shops_ShopBalances_BalanceId",
                table: "Shops");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_UserBalances_BalanceId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_BalanceId",
                table: "Users");

            migrationBuilder.AlterColumn<Guid>(
                name: "BalanceId",
                table: "Users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "BalanceId",
                table: "Shops",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_BalanceId",
                table: "Users",
                column: "BalanceId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Shops_ShopBalances_BalanceId",
                table: "Shops",
                column: "BalanceId",
                principalTable: "ShopBalances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_UserBalances_BalanceId",
                table: "Users",
                column: "BalanceId",
                principalTable: "UserBalances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Shops_ShopBalances_BalanceId",
                table: "Shops");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_UserBalances_BalanceId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_BalanceId",
                table: "Users");

            migrationBuilder.AlterColumn<Guid>(
                name: "BalanceId",
                table: "Users",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "BalanceId",
                table: "Shops",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "IX_Users_BalanceId",
                table: "Users",
                column: "BalanceId",
                unique: true,
                filter: "\"BalanceId\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Shops_ShopBalances_BalanceId",
                table: "Shops",
                column: "BalanceId",
                principalTable: "ShopBalances",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_UserBalances_BalanceId",
                table: "Users",
                column: "BalanceId",
                principalTable: "UserBalances",
                principalColumn: "Id");
        }
    }
}
