using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CreoHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBannedStatusAndStatusLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // BanReason column on Products
            migrationBuilder.AddColumn<string>(
                name: "BanReason",
                table: "Products",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            // ProductStatusLogs table
            migrationBuilder.CreateTable(
                name: "ProductStatusLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId   = table.Column<int>(type: "integer", nullable: false),
                    OldStatus   = table.Column<string>(type: "text", nullable: false),
                    NewStatus   = table.Column<string>(type: "text", nullable: false),
                    Reason      = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ChangedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt   = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductStatusLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductStatusLogs_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductStatusLogs_ProductId",
                table: "ProductStatusLogs",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductStatusLogs_CreatedAt",
                table: "ProductStatusLogs",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ProductStatusLogs");

            migrationBuilder.DropColumn(name: "BanReason", table: "Products");
        }
    }
}
