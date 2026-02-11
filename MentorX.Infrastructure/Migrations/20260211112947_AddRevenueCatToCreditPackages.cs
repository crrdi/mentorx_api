using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentorX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRevenueCatToCreditPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: columns may already exist (e.g. applied via Supabase)
            migrationBuilder.Sql(@"ALTER TABLE ""CreditPackages"" ADD COLUMN IF NOT EXISTS ""RevenueCatProductId"" VARCHAR(255) NULL;");
            migrationBuilder.Sql(@"ALTER TABLE ""CreditPackages"" ADD COLUMN IF NOT EXISTS ""Type"" VARCHAR(50) NOT NULL DEFAULT 'one_time';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RevenueCatProductId",
                table: "CreditPackages");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "CreditPackages");
        }
    }
}
