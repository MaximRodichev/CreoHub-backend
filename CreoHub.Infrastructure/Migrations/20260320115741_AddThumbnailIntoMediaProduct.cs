using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreoHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddThumbnailIntoMediaProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ThumbnailId",
                table: "MediaProducts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaProducts_ThumbnailId",
                table: "MediaProducts",
                column: "ThumbnailId");

            migrationBuilder.AddForeignKey(
                name: "FK_MediaProducts_StorageObjects_ThumbnailId",
                table: "MediaProducts",
                column: "ThumbnailId",
                principalTable: "StorageObjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MediaProducts_StorageObjects_ThumbnailId",
                table: "MediaProducts");

            migrationBuilder.DropIndex(
                name: "IX_MediaProducts_ThumbnailId",
                table: "MediaProducts");

            migrationBuilder.DropColumn(
                name: "ThumbnailId",
                table: "MediaProducts");
        }
    }
}
