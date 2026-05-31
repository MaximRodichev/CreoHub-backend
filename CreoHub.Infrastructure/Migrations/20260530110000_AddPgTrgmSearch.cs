using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreoHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPgTrgmSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable pg_trgm extension for trigram-based fuzzy search
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            // GIN index on lower(Name) for Products — powers ILike + trigram similarity
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Products_Name_trgm""
                ON ""Products"" USING gin (lower(""Name"") gin_trgm_ops);
            ");

            // GIN index on lower(Name) for Tags — enables tag-name search
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Tags_Name_trgm""
                ON ""Tags"" USING gin (lower(""Name"") gin_trgm_ops);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Products_Name_trgm"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Tags_Name_trgm"";");
            migrationBuilder.Sql("DROP EXTENSION IF EXISTS pg_trgm;");
        }
    }
}
