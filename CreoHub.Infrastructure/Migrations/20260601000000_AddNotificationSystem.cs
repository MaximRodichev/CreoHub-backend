using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CreoHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1. UserNotificationSettings (1:1 с User) ──────────────────────
            migrationBuilder.CreateTable(
                name: "UserNotificationSettings",
                columns: table => new
                {
                    UserId            = table.Column<Guid>(type: "uuid", nullable: false),
                    TelegramEnabled   = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EmailEnabled      = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    NotifyOnPurchase  = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    NotifyOnModeration= table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    NotifyOnBalance   = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    NotifyOnBroadcast = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    UpdatedAt         = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotificationSettings", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserNotificationSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Данные из старых колонок Users → новая таблица для всех существующих юзеров
            migrationBuilder.Sql(@"
                INSERT INTO ""UserNotificationSettings""
                    (""UserId"", ""TelegramEnabled"", ""EmailEnabled"",
                     ""NotifyOnPurchase"", ""NotifyOnModeration"",
                     ""NotifyOnBalance"", ""NotifyOnBroadcast"", ""UpdatedAt"")
                SELECT ""Id"", true, true,
                       ""NotifyOnPurchase"", ""NotifyOnModeration"",
                       true, true, NOW()
                FROM ""Users""
                ON CONFLICT DO NOTHING;
            ");

            // ── 2. Удаляем старые колонки из Users ────────────────────────────
            migrationBuilder.DropColumn(name: "NotifyOnPurchase",   table: "Users");
            migrationBuilder.DropColumn(name: "NotifyOnModeration", table: "Users");

            // ── 3. InAppNotifications ──────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "InAppNotifications",
                columns: table => new
                {
                    Id        = table.Column<int>(type: "integer", nullable: false)
                                     .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId    = table.Column<Guid>(type: "uuid", nullable: false),
                    Type      = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Message   = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ActionUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRead    = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ReadAt    = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InAppNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InAppNotifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InAppNotifications_UserId_IsRead",
                table: "InAppNotifications",
                columns: new[] { "UserId", "IsRead" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Возвращаем колонки в Users
            migrationBuilder.AddColumn<bool>(
                name: "NotifyOnPurchase",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyOnModeration",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            // Копируем данные обратно
            migrationBuilder.Sql(@"
                UPDATE ""Users"" u
                SET ""NotifyOnPurchase""   = s.""NotifyOnPurchase"",
                    ""NotifyOnModeration"" = s.""NotifyOnModeration""
                FROM ""UserNotificationSettings"" s
                WHERE u.""Id"" = s.""UserId"";
            ");

            migrationBuilder.DropTable(name: "InAppNotifications");
            migrationBuilder.DropTable(name: "UserNotificationSettings");
        }
    }
}
