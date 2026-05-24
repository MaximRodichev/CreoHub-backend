using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreoHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Добавляем колонку как nullable для заполнения существующих строк
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Products",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            // 2. Заполняем slug из Name: lowercase, спецсимволы → пробел, пробелы → дефис
            migrationBuilder.Sql("""
                UPDATE "Products"
                SET "Slug" = TRIM(
                    BOTH '-' FROM
                    REGEXP_REPLACE(
                        REGEXP_REPLACE(
                            REGEXP_REPLACE(
                                LOWER("Name"),
                                '[^\w\s-]', ' ', 'g'
                            ),
                            '\s+', '-', 'g'
                        ),
                        '-{2,}', '-', 'g'
                    )
                )
                WHERE "Slug" IS NULL;
                """);

            // 3. Делаем NOT NULL
            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Products",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            // 4. Уникальный индекс
            migrationBuilder.CreateIndex(
                name: "IX_Products_Slug",
                table: "Products",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_Slug",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Products");
        }
    }
}
