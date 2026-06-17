using CreoHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreoHub.Infrastructure.Migrations
{
    /// <summary>
    /// Data-only миграция: перегенерирует кириллические слаги товаров в латиницу
    /// той же картой транслитерации, что Product.GenerateSlug.
    /// Модель не меняется, поэтому Designer-файла нет — атрибуты объявлены на классе.
    ///
    /// Дедупликация обязательна: на "Slug" висит unique index, а транслит может
    /// схлопнуть разные кириллические слаги в один латинский — таким добавляется "-{Id}".
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260610100000_TransliterateProductSlugs")]
    public partial class TransliterateProductSlugs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                WITH t AS (
                    SELECT "Id",
                           btrim(
                               translate(
                                   replace(replace(replace(replace(replace(replace(replace(
                                       "Slug",
                                       'ё','yo'), 'ж','zh'), 'ч','ch'), 'ш','sh'), 'щ','shch'), 'ю','yu'), 'я','ya'),
                                   'абвгдезийклмнопрстуфхцыэъь',
                                   'abvgdeziyklmnoprstufhcye'
                               ),
                           '-') AS base
                    FROM "Products"
                    WHERE "Slug" ~ '[а-яё]'
                ),
                d AS (
                    SELECT t."Id",
                           CASE
                               WHEN t.base = '' THEN 'product-' || t."Id"
                               WHEN ROW_NUMBER() OVER (PARTITION BY t.base ORDER BY t."Id") > 1
                                    OR EXISTS (SELECT 1 FROM "Products" x
                                               WHERE x."Slug" = t.base AND x."Slug" !~ '[а-яё]')
                                   THEN t.base || '-' || t."Id"
                               ELSE t.base
                           END AS new_slug
                    FROM t
                )
                UPDATE "Products" p
                SET "Slug" = d.new_slug
                FROM d
                WHERE p."Id" = d."Id";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Обратной транслитерации не существует — миграция необратима (no-op).
        }
    }
}
