using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Trainings.Infrastructure.Data;

#nullable disable

namespace Trainings.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260721103000_AddUserLanguageAndThemePreferences")]
    /// <inheritdoc />
    public partial class AddUserLanguageAndThemePreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Users",
                type: "TEXT",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Theme",
                table: "Users",
                type: "TEXT",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Language",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Theme",
                table: "Users");
        }
    }
}
