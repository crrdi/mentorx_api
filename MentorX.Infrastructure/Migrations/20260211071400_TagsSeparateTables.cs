using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentorX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TagsSeparateTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: columns/tables may already exist (e.g. Supabase migrations)
            migrationBuilder.Sql(@"ALTER TABLE ""Mentors"" DROP COLUMN IF EXISTS ""ExpertiseTags"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Insights"" DROP COLUMN IF EXISTS ""Tags"";");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""Tags"" (
                    ""Id"" uuid NOT NULL PRIMARY KEY,
                    ""Name"" varchar(100) NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL,
                    ""UpdatedAt"" timestamp with time zone NOT NULL,
                    ""DeletedAt"" timestamp with time zone NULL
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Tags_Name"" ON ""Tags"" (""Name"");
            ");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""InsightTags"" (
                    ""InsightId"" uuid NOT NULL,
                    ""TagId"" uuid NOT NULL,
                    PRIMARY KEY (""InsightId"", ""TagId""),
                    CONSTRAINT ""FK_InsightTags_Insights_InsightId"" FOREIGN KEY (""InsightId"") REFERENCES ""Insights"" (""Id"") ON DELETE CASCADE,
                    CONSTRAINT ""FK_InsightTags_Tags_TagId"" FOREIGN KEY (""TagId"") REFERENCES ""Tags"" (""Id"") ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS ""IX_InsightTags_TagId"" ON ""InsightTags"" (""TagId"");
            ");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""MentorTags"" (
                    ""MentorId"" uuid NOT NULL,
                    ""TagId"" uuid NOT NULL,
                    PRIMARY KEY (""MentorId"", ""TagId""),
                    CONSTRAINT ""FK_MentorTags_Mentors_MentorId"" FOREIGN KEY (""MentorId"") REFERENCES ""Mentors"" (""Id"") ON DELETE CASCADE,
                    CONSTRAINT ""FK_MentorTags_Tags_TagId"" FOREIGN KEY (""TagId"") REFERENCES ""Tags"" (""Id"") ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS ""IX_MentorTags_TagId"" ON ""MentorTags"" (""TagId"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InsightTags");

            migrationBuilder.DropTable(
                name: "MentorTags");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.AddColumn<string>(
                name: "ExpertiseTags",
                table: "Mentors",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Insights",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
