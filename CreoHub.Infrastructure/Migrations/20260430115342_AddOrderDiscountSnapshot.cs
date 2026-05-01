using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreoHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderDiscountSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CartDiscountPercent",
                table: "Orders",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "Orders",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PersonalDiscountPercent",
                table: "Orders",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Subtotal",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CartDiscountPercent",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PersonalDiscountPercent",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Subtotal",
                table: "Orders");
        }
    }
}
