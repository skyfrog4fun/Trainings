using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trainings.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TrainingLifecycleFeedbackRedesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EffectiveDurationMinutes",
                table: "TrainingBlocks");

            migrationBuilder.DropColumn(
                name: "TrainerComment",
                table: "TrainingBlocks");

            migrationBuilder.CreateTable(
                name: "ParticipantFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TrainingId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Rating = table.Column<int>(type: "INTEGER", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipantFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParticipantFeedbacks_Trainings_TrainingId",
                        column: x => x.TrainingId,
                        principalTable: "Trainings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParticipantFeedbacks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrainerFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TrainingId = table.Column<int>(type: "INTEGER", nullable: false),
                    TrainerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Rating = table.Column<int>(type: "INTEGER", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainerFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainerFeedbacks_Trainings_TrainingId",
                        column: x => x.TrainingId,
                        principalTable: "Trainings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainerFeedbacks_Users_TrainerId",
                        column: x => x.TrainerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantFeedbacks_TrainingId_UserId",
                table: "ParticipantFeedbacks",
                columns: new[] { "TrainingId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantFeedbacks_UserId",
                table: "ParticipantFeedbacks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainerFeedbacks_TrainerId",
                table: "TrainerFeedbacks",
                column: "TrainerId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainerFeedbacks_TrainingId",
                table: "TrainerFeedbacks",
                column: "TrainingId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParticipantFeedbacks");

            migrationBuilder.DropTable(
                name: "TrainerFeedbacks");

            migrationBuilder.AddColumn<int>(
                name: "EffectiveDurationMinutes",
                table: "TrainingBlocks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrainerComment",
                table: "TrainingBlocks",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);
        }
    }
}
