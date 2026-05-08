using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trainings.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceMailFailureTrackingWithStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastSuccessSentAt",
                table: "MailConfigurations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "MailConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                table: "MailConfigurations",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "MailConfigurations"
                SET "Status" = CASE
                    WHEN "FailureCount" > 0 THEN 2
                    ELSE 0
                END
                """);

            migrationBuilder.DropColumn(
                name: "FailureCount",
                table: "MailConfigurations");

            migrationBuilder.DropColumn(
                name: "LastFailedOn",
                table: "MailConfigurations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailureCount",
                table: "MailConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastFailedOn",
                table: "MailConfigurations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "MailConfigurations"
                SET "FailureCount" = CASE
                    WHEN "Status" = 2 THEN 1
                    ELSE 0
                END
                """);

            migrationBuilder.DropColumn(
                name: "LastError",
                table: "MailConfigurations");

            migrationBuilder.DropColumn(
                name: "LastSuccessSentAt",
                table: "MailConfigurations");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "MailConfigurations");
        }
    }
}
