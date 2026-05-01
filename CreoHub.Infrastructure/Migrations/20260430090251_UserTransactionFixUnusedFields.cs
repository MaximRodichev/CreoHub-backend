using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreoHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UserTransactionFixUnusedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NetAmount",
                table: "UserTransactions");

            migrationBuilder.DropColumn(
                name: "PlatformFeeAmount",
                table: "UserTransactions");

            migrationBuilder.DropColumn(
                name: "PlatformFeePercent",
                table: "UserTransactions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "NetAmount",
                table: "UserTransactions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PlatformFeeAmount",
                table: "UserTransactions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PlatformFeePercent",
                table: "UserTransactions",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
