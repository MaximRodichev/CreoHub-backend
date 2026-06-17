using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreoHub.Infrastructure.Migrations
{
    /// <summary>
    /// Чистка user-таблицы:
    ///  - удаляем хранимое User.Discount (скидка теперь считается из LifetimeSpent, нигде не хранится);
    ///  - снимаем уникальность с User.Name (отображаемые имена могут повторяться);
    ///  - убираем CHECK CK_User_Discount (колонки больше нет).
    /// Инкрементальная миграция (snapshot отражает конечное состояние).
    /// </summary>
    public partial class RemoveUserDiscountAndNameUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_User_Discount",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Name",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Discount",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Discount",
                table: "Users",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Name",
                table: "Users",
                column: "Name",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_User_Discount",
                table: "Users",
                sql: "\"Discount\" >= 0 AND \"Discount\" <= 15");
        }
    }
}
