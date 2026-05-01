using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trainings.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeTrainingGroupIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trainings_Groups_GroupId",
                table: "Trainings");

            migrationBuilder.AlterColumn<int>(
                name: "GroupId",
                table: "Trainings",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_Trainings_Groups_GroupId",
                table: "Trainings",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trainings_Groups_GroupId",
                table: "Trainings");

            migrationBuilder.AlterColumn<int>(
                name: "GroupId",
                table: "Trainings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Trainings_Groups_GroupId",
                table: "Trainings",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
