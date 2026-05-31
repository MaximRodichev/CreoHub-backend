using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CreoHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationsAndBroadcast : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Notification settings on User ──────────────────────────────────
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

            // ── BroadcastJobs table ────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "BroadcastJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Message      = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    Status       = table.Column<string>(type: "character varying(20)",   maxLength: 20,   nullable: false, defaultValue: "pending"),
                    CreatedBy    = table.Column<Guid>(type: "uuid",      nullable: false),
                    CreatedAt    = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt    = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt  = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalUsers   = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    SentCount    = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    FailedCount  = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ErrorLog     = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BroadcastJobs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BroadcastJobs_Status",
                table: "BroadcastJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BroadcastJobs_CreatedAt",
                table: "BroadcastJobs",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BroadcastJobs");

            migrationBuilder.DropColumn(name: "NotifyOnPurchase",   table: "Users");
            migrationBuilder.DropColumn(name: "NotifyOnModeration", table: "Users");
        }
    }
}
