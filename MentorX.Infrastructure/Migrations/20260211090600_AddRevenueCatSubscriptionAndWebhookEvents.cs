using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentorX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRevenueCatSubscriptionAndWebhookEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RevenueCatCustomerId",
                table: "Users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionExpiresAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionProductId",
                table: "Users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionStatus",
                table: "Users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RevenueCatWebhookEvents",
                columns: table => new
                {
                    EventId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevenueCatWebhookEvents", x => x.EventId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_RevenueCatCustomerId",
                table: "Users",
                column: "RevenueCatCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_SubscriptionStatus",
                table: "Users",
                column: "SubscriptionStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RevenueCatWebhookEvents");

            migrationBuilder.DropIndex(
                name: "IX_Users_RevenueCatCustomerId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_SubscriptionStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RevenueCatCustomerId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SubscriptionExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SubscriptionProductId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SubscriptionStatus",
                table: "Users");
        }
    }
}
