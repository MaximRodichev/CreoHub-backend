using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreoHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ContentFilesAndFinances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Shops_Description",
                table: "Shops");

            migrationBuilder.AlterColumn<decimal>(
                name: "Discount",
                table: "Users",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AddColumn<Guid>(
                name: "BalanceId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BalanceId",
                table: "Shops",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TransactionId",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ContentFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceWeight = table.Column<int>(type: "integer", nullable: false),
                    PreviewName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StorageObjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentFiles", x => x.Id);
                    table.CheckConstraint("CK_ContentFile_PriceWeight", "\"PriceWeight\" >= 1 AND \"PriceWeight\" <= 10");
                    table.ForeignKey(
                        name: "FK_ContentFiles_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContentFiles_StorageObjects_StorageObjectId",
                        column: x => x.StorageObjectId,
                        principalTable: "StorageObjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShopBalances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShopId = table.Column<Guid>(type: "uuid", nullable: false),
                    AvailableAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PendingAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopBalances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShopTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShopId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PlatformFeePercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    PlatformFeeAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TransactionType = table.Column<string>(type: "text", nullable: false),
                    TransactionStatus = table.Column<string>(type: "text", nullable: false),
                    TxHash = table.Column<string>(type: "text", nullable: true),
                    SenderAddress = table.Column<string>(type: "text", nullable: true),
                    TrackId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopTransactions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShopTransactions_Shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "Shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserBalances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AvailableAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PendingAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBalances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PlatformFeePercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    PlatformFeeAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TransactionType = table.Column<string>(type: "text", nullable: false),
                    TransactionStatus = table.Column<string>(type: "text", nullable: false),
                    TxHash = table.Column<string>(type: "text", nullable: true),
                    SenderAddress = table.Column<string>(type: "text", nullable: true),
                    TrackId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTransactions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserTransactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContentAccesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentAccesses_ContentFiles_ContentFileId",
                        column: x => x.ContentFileId,
                        principalTable: "ContentFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContentAccesses_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContentAccesses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_BalanceId",
                table: "Users",
                column: "BalanceId",
                unique: true,
                filter: "\"BalanceId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Shops_BalanceId",
                table: "Shops",
                column: "BalanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TransactionId",
                table: "Orders",
                column: "TransactionId",
                unique: true,
                filter: "\"TransactionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ContentAccesses_ContentFileId",
                table: "ContentAccesses",
                column: "ContentFileId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentAccesses_OrderId",
                table: "ContentAccesses",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentAccesses_UserId_ContentFileId",
                table: "ContentAccesses",
                columns: new[] { "UserId", "ContentFileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentFiles_ProductId",
                table: "ContentFiles",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentFiles_StorageObjectId",
                table: "ContentFiles",
                column: "StorageObjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopBalances_ShopId",
                table: "ShopBalances",
                column: "ShopId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopTransactions_OrderId",
                table: "ShopTransactions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopTransactions_ShopId",
                table: "ShopTransactions",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopTransactions_TrackId",
                table: "ShopTransactions",
                column: "TrackId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBalances_UserId",
                table: "UserBalances",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserTransactions_OrderId",
                table: "UserTransactions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTransactions_TrackId",
                table: "UserTransactions",
                column: "TrackId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTransactions_UserId",
                table: "UserTransactions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_UserTransactions_TransactionId",
                table: "Orders",
                column: "TransactionId",
                principalTable: "UserTransactions",
                principalColumn: "Id");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_UserTransactions_TransactionId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Shops_ShopBalances_BalanceId",
                table: "Shops");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_UserBalances_BalanceId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "ContentAccesses");

            migrationBuilder.DropTable(
                name: "ShopBalances");

            migrationBuilder.DropTable(
                name: "ShopTransactions");

            migrationBuilder.DropTable(
                name: "UserBalances");

            migrationBuilder.DropTable(
                name: "UserTransactions");

            migrationBuilder.DropTable(
                name: "ContentFiles");

            migrationBuilder.DropIndex(
                name: "IX_Users_BalanceId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Shops_BalanceId",
                table: "Shops");

            migrationBuilder.DropIndex(
                name: "IX_Orders_TransactionId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BalanceId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BalanceId",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "TransactionId",
                table: "Orders");

            migrationBuilder.AlterColumn<double>(
                name: "Discount",
                table: "Users",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.CreateIndex(
                name: "IX_Shops_Description",
                table: "Shops",
                column: "Description",
                unique: true);
        }
    }
}
