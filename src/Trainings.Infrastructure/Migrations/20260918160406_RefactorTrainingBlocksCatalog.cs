using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace Trainings.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorTrainingBlocksCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tags_Groups_GroupId",
                table: "Tags");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingBlocks_TrainingBlocks_SourceBlockId",
                table: "TrainingBlocks");

            migrationBuilder.DropTable(
                name: "TrainingBlockTags");

            migrationBuilder.DropIndex(
                name: "IX_TrainingBlocks_SourceBlockId",
                table: "TrainingBlocks");

            migrationBuilder.DropIndex(
                name: "IX_TrainingBlocks_TrainingId",
                table: "TrainingBlocks");

            migrationBuilder.DropIndex(
                name: "IX_Tags_GroupId",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "SourceBlockId",
                table: "TrainingBlocks");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Tags");

            migrationBuilder.AddColumn<int>(
                name: "DefinitionId",
                table: "TrainingBlocks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxParticipants",
                table: "TrainingBlocks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinParticipants",
                table: "TrainingBlocks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ColorToken",
                table: "Tags",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "Tags",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Tags",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "Tags",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsApproved = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSystemFallback = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Games_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Translations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EntityType = table.Column<int>(type: "INTEGER", nullable: false),
                    EntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    Culture = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Translations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainingBlockDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    DurationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    TagId = table.Column<int>(type: "INTEGER", nullable: false),
                    GameId = table.Column<int>(type: "INTEGER", nullable: true),
                    MinParticipants = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxParticipants = table.Column<int>(type: "INTEGER", nullable: false),
                    GroupId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsGlobal = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatorId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingBlockDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingBlockDefinitions_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingBlockDefinitions_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TrainingBlockDefinitions_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingBlockDefinitions_Users_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingBlocks_DefinitionId",
                table: "TrainingBlocks",
                column: "DefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingBlocks_TrainingId_OrderIndex",
                table: "TrainingBlocks",
                columns: new[] { "TrainingId", "OrderIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_DisplayOrder",
                table: "Tags",
                column: "DisplayOrder",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Key",
                table: "Tags",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_AttemptId",
                table: "NotificationLogs",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_CreatedAt",
                table: "NotificationLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Games_CreatedByUserId",
                table: "Games",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_IsActive_IsApproved",
                table: "Games",
                columns: new[] { "IsActive", "IsApproved" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingBlockDefinitions_CreatorId_IsActive",
                table: "TrainingBlockDefinitions",
                columns: new[] { "CreatorId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingBlockDefinitions_GameId",
                table: "TrainingBlockDefinitions",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingBlockDefinitions_GroupId_IsActive",
                table: "TrainingBlockDefinitions",
                columns: new[] { "GroupId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingBlockDefinitions_IsGlobal_IsActive",
                table: "TrainingBlockDefinitions",
                columns: new[] { "IsGlobal", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingBlockDefinitions_TagId",
                table: "TrainingBlockDefinitions",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_Translations_EntityType_EntityId",
                table: "Translations",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_Translations_EntityType_EntityId_Culture",
                table: "Translations",
                columns: new[] { "EntityType", "EntityId", "Culture" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingBlocks_TrainingBlockDefinitions_DefinitionId",
                table: "TrainingBlocks",
                column: "DefinitionId",
                principalTable: "TrainingBlockDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainingBlocks_TrainingBlockDefinitions_DefinitionId",
                table: "TrainingBlocks");

            migrationBuilder.DropTable(
                name: "TrainingBlockDefinitions");

            migrationBuilder.DropTable(
                name: "Translations");

            migrationBuilder.DropTable(
                name: "Games");

            migrationBuilder.DropIndex(
                name: "IX_TrainingBlocks_DefinitionId",
                table: "TrainingBlocks");

            migrationBuilder.DropIndex(
                name: "IX_TrainingBlocks_TrainingId_OrderIndex",
                table: "TrainingBlocks");

            migrationBuilder.DropIndex(
                name: "IX_Tags_DisplayOrder",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_Tags_Key",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_NotificationLogs_AttemptId",
                table: "NotificationLogs");

            migrationBuilder.DropIndex(
                name: "IX_NotificationLogs_CreatedAt",
                table: "NotificationLogs");

            migrationBuilder.DropColumn(
                name: "DefinitionId",
                table: "TrainingBlocks");

            migrationBuilder.DropColumn(
                name: "MaxParticipants",
                table: "TrainingBlocks");

            migrationBuilder.DropColumn(
                name: "MinParticipants",
                table: "TrainingBlocks");

            migrationBuilder.DropColumn(
                name: "ColorToken",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "Key",
                table: "Tags");

            migrationBuilder.AddColumn<int>(
                name: "SourceBlockId",
                table: "TrainingBlocks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GroupId",
                table: "Tags",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Tags",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "TrainingBlockTags",
                columns: table => new
                {
                    TrainingBlockId = table.Column<int>(type: "INTEGER", nullable: false),
                    TagId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingBlockTags", x => new { x.TrainingBlockId, x.TagId });
                    table.ForeignKey(
                        name: "FK_TrainingBlockTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainingBlockTags_TrainingBlocks_TrainingBlockId",
                        column: x => x.TrainingBlockId,
                        principalTable: "TrainingBlocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingBlocks_SourceBlockId",
                table: "TrainingBlocks",
                column: "SourceBlockId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingBlocks_TrainingId",
                table: "TrainingBlocks",
                column: "TrainingId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_GroupId",
                table: "Tags",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingBlockTags_TagId",
                table: "TrainingBlockTags",
                column: "TagId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_Groups_GroupId",
                table: "Tags",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingBlocks_TrainingBlocks_SourceBlockId",
                table: "TrainingBlocks",
                column: "SourceBlockId",
                principalTable: "TrainingBlocks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
#pragma warning restore CA1861
}
