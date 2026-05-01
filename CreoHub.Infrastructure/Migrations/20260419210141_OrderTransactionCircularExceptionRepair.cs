using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreoHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OrderTransactionCircularExceptionRepair : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_UserTransactions_TransactionId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_UserTransactions_OrderId",
                table: "UserTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Orders_TransactionId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TransactionId",
                table: "Orders");

            migrationBuilder.CreateIndex(
                name: "IX_UserTransactions_OrderId",
                table: "UserTransactions",
                column: "OrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserTransactions_OrderId",
                table: "UserTransactions");

            migrationBuilder.AddColumn<Guid>(
                name: "TransactionId",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserTransactions_OrderId",
                table: "UserTransactions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TransactionId",
                table: "Orders",
                column: "TransactionId",
                unique: true,
                filter: "\"TransactionId\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_UserTransactions_TransactionId",
                table: "Orders",
                column: "TransactionId",
                principalTable: "UserTransactions",
                principalColumn: "Id");
        }
    }
}
