using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreoHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountMergeLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountMergeLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KeepUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MergedUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MergedName = table.Column<string>(type: "text", nullable: true),
                    MergedEmail = table.Column<string>(type: "text", nullable: true),
                    MergedTelegramId = table.Column<long>(type: "bigint", nullable: true),
                    MergedTelegramUsername = table.Column<string>(type: "text", nullable: true),
                    AdminId = table.Column<Guid>(type: "uuid", nullable: false),
                    MovedContentAccess = table.Column<int>(type: "integer", nullable: false),
                    MovedOrders = table.Column<int>(type: "integer", nullable: false),
                    MovedTransactions = table.Column<int>(type: "integer", nullable: false),
                    MovedSubscriptions = table.Column<int>(type: "integer", nullable: false),
                    AddedBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AddedSpent = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountMergeLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountMergeLogs_CreatedAt",
                table: "AccountMergeLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AccountMergeLogs_KeepUserId",
                table: "AccountMergeLogs",
                column: "KeepUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountMergeLogs");
        }
    }
}
