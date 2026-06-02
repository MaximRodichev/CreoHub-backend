using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CreoHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShopRequestsAndSecurityHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserTransactions_TrackId",
                table: "UserTransactions");

            migrationBuilder.DropIndex(
                name: "IX_ShopTransactions_TrackId",
                table: "ShopTransactions");

            migrationBuilder.CreateTable(
                name: "PendingUploads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ShopId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    MimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MaxBytes = table.Column<long>(type: "bigint", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingUploads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShopRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShopId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuyerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuyerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BuyerEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SellerReply = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RepliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopRequests_Shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "Shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShopRequests_Users_BuyerUserId",
                        column: x => x.BuyerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserTransactions_TrackId",
                table: "UserTransactions",
                column: "TrackId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopTransactions_TrackId",
                table: "ShopTransactions",
                column: "TrackId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PendingUploads_ExpiresAt",
                table: "PendingUploads",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_PendingUploads_Key_ShopId",
                table: "PendingUploads",
                columns: new[] { "Key", "ShopId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopRequests_BuyerUserId",
                table: "ShopRequests",
                column: "BuyerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopRequests_ShopId_Status",
                table: "ShopRequests",
                columns: new[] { "ShopId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingUploads");

            migrationBuilder.DropTable(
                name: "ShopRequests");

            migrationBuilder.DropIndex(
                name: "IX_UserTransactions_TrackId",
                table: "UserTransactions");

            migrationBuilder.DropIndex(
                name: "IX_ShopTransactions_TrackId",
                table: "ShopTransactions");

            migrationBuilder.CreateIndex(
                name: "IX_UserTransactions_TrackId",
                table: "UserTransactions",
                column: "TrackId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopTransactions_TrackId",
                table: "ShopTransactions",
                column: "TrackId");
        }
    }
}
