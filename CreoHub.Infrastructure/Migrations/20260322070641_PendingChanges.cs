using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreoHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MediaProducts_StorageObjectId",
                table: "MediaProducts");

            migrationBuilder.CreateIndex(
                name: "IX_MediaProducts_StorageObjectId",
                table: "MediaProducts",
                column: "StorageObjectId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MediaProducts_StorageObjectId",
                table: "MediaProducts");

            migrationBuilder.CreateIndex(
                name: "IX_MediaProducts_StorageObjectId",
                table: "MediaProducts",
                column: "StorageObjectId");
        }
    }
}
