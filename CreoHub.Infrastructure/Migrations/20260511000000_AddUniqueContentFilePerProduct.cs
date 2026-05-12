using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreoHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueContentFilePerProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove duplicate (ProductId, StorageObjectId) rows that accumulated before
            // this constraint existed — keep the first-inserted row per pair.
            migrationBuilder.Sql("""
                DELETE FROM "ContentFiles"
                WHERE "Id" IN (
                    SELECT "Id" FROM (
                        SELECT "Id",
                               ROW_NUMBER() OVER (
                                   PARTITION BY "ProductId", "StorageObjectId"
                                   ORDER BY "Id"
                               ) AS rn
                        FROM "ContentFiles"
                    ) t
                    WHERE rn > 1
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ContentFiles_ProductId_StorageObjectId",
                table: "ContentFiles",
                columns: new[] { "ProductId", "StorageObjectId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContentFiles_ProductId_StorageObjectId",
                table: "ContentFiles");
        }
    }
}
