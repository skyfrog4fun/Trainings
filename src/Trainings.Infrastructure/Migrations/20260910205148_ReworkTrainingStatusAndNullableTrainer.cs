using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trainings.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReworkTrainingStatusAndNullableTrainer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "TrainerId",
                table: "Trainings",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            // Data backfill: under the previous workflow every training had a mandatory trainer,
            // so legacy trainings sitting in status "New" (0) already have a trainer assigned and
            // must be promoted to "InPlanning" (1) to match the new status semantics, where "New"
            // now means "unassigned". Legacy "Planning" rows were already stored as ordinal 1 and
            // require no change since "InPlanning" reuses that same ordinal.
            migrationBuilder.Sql(
                "UPDATE Trainings SET Status = 1 WHERE Status = 0 AND TrainerId IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "TrainerId",
                table: "Trainings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);
        }
    }
}
