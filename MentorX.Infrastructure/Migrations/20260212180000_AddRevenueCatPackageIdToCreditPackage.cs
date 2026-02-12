using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentorX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRevenueCatPackageIdToCreditPackage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RevenueCatPackageId",
                table: "CreditPackages",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            // Map RevenueCat package key to existing 100-credits package (store product id: com.erdiacar.mentorx.credits_100)
            migrationBuilder.Sql(
                "UPDATE \"CreditPackages\" SET \"RevenueCatPackageId\" = '$rc_credits_100' WHERE \"RevenueCatProductId\" = 'com.erdiacar.mentorx.credits_100';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RevenueCatPackageId",
                table: "CreditPackages");
        }
    }
}
