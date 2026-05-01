using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreoHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLifetimeSpentToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LifetimeSpent",
                table: "Users",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LifetimeSpent",
                table: "Users");
        }
    }
}
