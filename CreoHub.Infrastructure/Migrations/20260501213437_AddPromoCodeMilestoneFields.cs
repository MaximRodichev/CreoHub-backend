using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreoHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPromoCodeMilestoneFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "IssuedToUserId",
                table: "SubscriptionPromoCodes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MilestoneTag",
                table: "SubscriptionPromoCodes",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPromoCodes_IssuedToUserId_MilestoneTag",
                table: "SubscriptionPromoCodes",
                columns: new[] { "IssuedToUserId", "MilestoneTag" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubscriptionPromoCodes_IssuedToUserId_MilestoneTag",
                table: "SubscriptionPromoCodes");

            migrationBuilder.DropColumn(
                name: "IssuedToUserId",
                table: "SubscriptionPromoCodes");

            migrationBuilder.DropColumn(
                name: "MilestoneTag",
                table: "SubscriptionPromoCodes");
        }
    }
}
