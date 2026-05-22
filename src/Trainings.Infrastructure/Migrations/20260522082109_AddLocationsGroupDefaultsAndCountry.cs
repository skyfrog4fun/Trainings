using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trainings.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationsGroupDefaultsAndCountry : Migration
    {
        private static readonly string[] LocationColumns = { "Id", "Name", "CityName", "IsSystemWide", "IsActive" };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "Trainings");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Users",
                type: "TEXT",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                table: "Trainings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeetingPoint",
                table: "Trainings",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecialLocationDescription",
                table: "Trainings",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Groups",
                type: "TEXT",
                maxLength: 2,
                nullable: false,
                defaultValue: "CH");

            migrationBuilder.AddColumn<int>(
                name: "DurationMinutes",
                table: "Groups",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                table: "Groups",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxParticipants",
                table: "Groups",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "StartTime",
                table: "Groups",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Weekday",
                table: "Groups",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CityName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsSystemWide = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GroupLocations",
                columns: table => new
                {
                    GroupId = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupLocations", x => new { x.GroupId, x.LocationId });
                    table.ForeignKey(
                        name: "FK_GroupLocations_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupLocations_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trainings_LocationId",
                table: "Trainings",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_LocationId",
                table: "Groups",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupLocations_LocationId",
                table: "GroupLocations",
                column: "LocationId");

            migrationBuilder.InsertData(
                table: "Locations",
                columns: LocationColumns,
                values: new object[,]
                {
                    { 1, "Outside", "", true, true },
                    { 2, "Special", "", true, true }
                });

            migrationBuilder.Sql("UPDATE \"Users\" SET \"Country\" = 'CH' WHERE \"Country\" IS NULL;");

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_Locations_LocationId",
                table: "Groups",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Trainings_Locations_LocationId",
                table: "Trainings",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Groups_Locations_LocationId",
                table: "Groups");

            migrationBuilder.DropForeignKey(
                name: "FK_Trainings_Locations_LocationId",
                table: "Trainings");

            migrationBuilder.DropTable(
                name: "GroupLocations");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropIndex(
                name: "IX_Trainings_LocationId",
                table: "Trainings");

            migrationBuilder.DropIndex(
                name: "IX_Groups_LocationId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "Trainings");

            migrationBuilder.DropColumn(
                name: "MeetingPoint",
                table: "Trainings");

            migrationBuilder.DropColumn(
                name: "SpecialLocationDescription",
                table: "Trainings");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "DurationMinutes",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "MaxParticipants",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "Weekday",
                table: "Groups");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Trainings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
