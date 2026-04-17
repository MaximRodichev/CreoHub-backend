using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CreoHub.Infrastructure.Migrations
{
    /// <summary>
    /// Baseline migration: creates the original schema that existed before migration tracking began.
    /// Inserted before "Initial" so a fresh empty database can be fully populated.
    /// Production databases that already have this schema should mark this migration as applied
    /// without running it: INSERT INTO "__EFMigrationsHistory" VALUES ('20260101000000_CreateSchema', '9.0.1');
    /// </summary>
    public partial class CreateSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Users (includes TelegramUsername which is dropped in the next migration)
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Discount = table.Column<double>(type: "double precision", nullable: false),
                    EmailAddress = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RegistrationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    TelegramId = table.Column<long>(type: "bigint", nullable: true),
                    TelegramUsername = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.CheckConstraint("CK_User_Discount", "\"Discount\" >= 0 AND \"Discount\" <= 15");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmailAddress",
                table: "Users",
                column: "EmailAddress",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Name",
                table: "Users",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_TelegramId",
                table: "Users",
                column: "TelegramId",
                unique: true);

            // Tags
            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                table: "Tags",
                column: "Name",
                unique: true);

            // Shops (FK → Users)
            migrationBuilder.CreateTable(
                name: "Shops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Shops_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Shops_Description",
                table: "Shops",
                column: "Description",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shops_Name",
                table: "Shops",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shops_OwnerId",
                table: "Shops",
                column: "OwnerId",
                unique: true);

            // Products (FK → Shops via OwnerId)
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.CheckConstraint("CK_Product_ProductType", "\"ProductType\" IN ('Single','Bundle')");
                    table.ForeignKey(
                        name: "FK_Products_Shops_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_OwnerId",
                table: "Products",
                column: "OwnerId");

            // Orders (FK → Users + Products)
            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OrderDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Orders_Users_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ProductId_CustomerId",
                table: "Orders",
                columns: new[] { "ProductId", "CustomerId" },
                unique: true);

            // Prices (FK → Products)
            migrationBuilder.CreateTable(
                name: "Prices",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prices", x => new { x.ProductId, x.Date });
                    table.CheckConstraint("CK_Price_Value", "\"Value\" >= 0 AND \"Value\" <= 1000");
                    table.ForeignKey(
                        name: "FK_Prices_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ProductBundles (FK → Products × 2)
            migrationBuilder.CreateTable(
                name: "ProductBundles",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    BundleId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductBundles", x => new { x.ProductId, x.BundleId });
                    table.ForeignKey(
                        name: "FK_ProductBundles_Products_BundleId",
                        column: x => x.BundleId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductBundles_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductBundles_BundleId",
                table: "ProductBundles",
                column: "BundleId");

            // ProductTag join table
            migrationBuilder.CreateTable(
                name: "ProductTag",
                columns: table => new
                {
                    ProductsId = table.Column<int>(type: "integer", nullable: false),
                    TagsId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductTag", x => new { x.ProductsId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_ProductTag_Products_ProductsId",
                        column: x => x.ProductsId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductTag_Tags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductTag_TagsId",
                table: "ProductTag",
                column: "TagsId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ProductTag");
            migrationBuilder.DropTable(name: "ProductBundles");
            migrationBuilder.DropTable(name: "Prices");
            migrationBuilder.DropTable(name: "Orders");
            migrationBuilder.DropTable(name: "Products");
            migrationBuilder.DropTable(name: "Shops");
            migrationBuilder.DropTable(name: "Tags");
            migrationBuilder.DropTable(name: "Users");
        }
    }
}
