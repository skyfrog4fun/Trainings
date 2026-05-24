using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trainings.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCountryTableAndRefactorCountryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create Countries table first
            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsRealCountry = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Countries_Code",
                table: "Countries",
                column: "Code",
                unique: true);

            // 2. Seed initial country data
            migrationBuilder.InsertData(
                table: "Countries",
                columns: ["Code", "Name", "IsRealCountry"],
                values: new object[,]
                {
                    { "??", "Undefined", false },
                    { "AT", "Austria", true },
                    { "CH", "Switzerland", true },
                    { "DE", "Germany", true }
                });

            // 3. Add CountryId columns (nullable)
            migrationBuilder.AddColumn<int>(
                name: "CountryId",
                table: "Users",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CountryId",
                table: "Locations",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CountryId",
                table: "Groups",
                type: "INTEGER",
                nullable: true);

            // 4. Migrate existing Country string values to FK references
            migrationBuilder.Sql(
                "UPDATE \"Groups\" SET \"CountryId\" = (SELECT \"Id\" FROM \"Countries\" WHERE \"Code\" = \"Groups\".\"Country\") " +
                "WHERE \"Country\" IS NOT NULL AND \"Country\" != '';");
            migrationBuilder.Sql(
                "UPDATE \"Users\" SET \"CountryId\" = (SELECT \"Id\" FROM \"Countries\" WHERE \"Code\" = \"Users\".\"Country\") " +
                "WHERE \"Country\" IS NOT NULL AND \"Country\" != '';");

            // 5. Drop old string Country columns
            migrationBuilder.DropColumn(
                name: "Country",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Groups");

            // 6. Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_Users_CountryId",
                table: "Users",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_CountryId",
                table: "Locations",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_CountryId",
                table: "Groups",
                column: "CountryId");

            // 7. Add FK constraints
            migrationBuilder.AddForeignKey(
                name: "FK_Groups_Countries_CountryId",
                table: "Groups",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Locations_Countries_CountryId",
                table: "Locations",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Countries_CountryId",
                table: "Users",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Groups_Countries_CountryId",
                table: "Groups");

            migrationBuilder.DropForeignKey(
                name: "FK_Locations_Countries_CountryId",
                table: "Locations");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Countries_CountryId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Countries");

            migrationBuilder.DropIndex(
                name: "IX_Users_CountryId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Locations_CountryId",
                table: "Locations");

            migrationBuilder.DropIndex(
                name: "IX_Groups_CountryId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "CountryId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CountryId",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "CountryId",
                table: "Groups");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Users",
                type: "TEXT",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Groups",
                type: "TEXT",
                maxLength: 2,
                nullable: false,
                defaultValue: "CH");
        }
    }
}
