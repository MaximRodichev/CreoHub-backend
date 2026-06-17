using CreoHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreoHub.Infrastructure.Migrations
{
    /// <summary>
    /// Data-only миграция: перегенерирует слаги ВСЕХ товаров из "Name" по полной
    /// карте транслитерации (как Product.GenerateSlug).
    ///
    /// Зачем вторая миграция: майский бэкфилл (AddProductSlug) использовал '\w' в
    /// regexp_replace, а его значение в PostgreSQL зависит от локали БД — при
    /// lc_ctype=C кириллица не считается '\w' и была ВЫРЕЗАНА из слагов
    /// ("Joker Stoker Анимационные материалы" → "joker-stoker"). Поэтому
    /// TransliterateProductSlugs (искала кириллицу в слагах) оказалась no-op.
    ///
    /// Здесь локале-независимо: заглавная и строчная кириллица транслитерируются
    /// явной картой, очистка — явным ASCII-классом [^a-z0-9_\s-].
    ///
    /// Двухфазный UPDATE: на "Slug" unique index, и при массовой перегенерации
    /// новое значение строки A может совпасть со СТАРЫМ значением строки B —
    /// Postgres проверяет уникальность per-row, словили бы transient-конфликт.
    /// Поэтому сначала все слаги уводятся во временные ('#' невозможен в финальном
    /// слаге), затем ставятся финальные.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260610130000_RegenerateProductSlugsFromNames")]
    public partial class RegenerateProductSlugsFromNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Фаза 1: временные заведомо уникальные значения, не пересекающиеся с финальными
            migrationBuilder.Sql("""UPDATE "Products" SET "Slug" = '#' || "Id";""");

            // Фаза 2: финальные слаги из Name + дедупликация транслит-коллизий
            migrationBuilder.Sql("""
                WITH t AS (
                    SELECT "Id",
                           btrim(
                               regexp_replace(
                                   regexp_replace(
                                       regexp_replace(
                                           lower(
                                               translate(
                                                   replace(replace(replace(replace(replace(replace(replace(
                                                   replace(replace(replace(replace(replace(replace(replace(
                                                       "Name",
                                                       'ё','yo'), 'Ё','yo'), 'ж','zh'), 'Ж','zh'),
                                                       'ч','ch'), 'Ч','ch'), 'ш','sh'), 'Ш','sh'),
                                                       'щ','shch'), 'Щ','shch'), 'ю','yu'), 'Ю','yu'),
                                                       'я','ya'), 'Я','ya'),
                                                   'абвгдезийклмнопрстуфхцыэАБВГДЕЗИЙКЛМНОПРСТУФХЦЫЭъьЪЬ',
                                                   'abvgdeziyklmnoprstufhcyeabvgdeziyklmnoprstufhcye'
                                               )
                                           ),
                                           '[^a-z0-9_\s-]', ' ', 'g'
                                       ),
                                       '\s+', '-', 'g'
                                   ),
                                   '-{2,}', '-', 'g'
                               ),
                           '-') AS base
                    FROM "Products"
                ),
                d AS (
                    SELECT t."Id",
                           CASE
                               WHEN t.base = '' THEN 'product-' || t."Id"
                               WHEN ROW_NUMBER() OVER (PARTITION BY t.base ORDER BY t."Id") > 1
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
            // Старые обрезанные слаги не восстановить — миграция необратима (no-op).
        }
    }
}
