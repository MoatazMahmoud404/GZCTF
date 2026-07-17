using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AddAiHints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AiHintsEnabled",
                table: "GameChallenges",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AiHintBudget",
                table: "GameChallenges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "AiHintsEnabled",
                table: "ExerciseChallenges",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AiHintBudget",
                table: "ExerciseChallenges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AiHintLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChallengeId = table.Column<int>(type: "integer", nullable: false),
                    ParticipationId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    HintText = table.Column<string>(type: "text", nullable: true),
                    Progression = table.Column<int>(type: "integer", nullable: false),
                    HintType = table.Column<string>(type: "text", nullable: true),
                    IsSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    RequestTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiHintLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiHintLogs_GameChallenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "GameChallenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AiHintLogs_Participations_ParticipationId",
                        column: x => x.ParticipationId,
                        principalTable: "Participations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiHintLogs_ChallengeId",
                table: "AiHintLogs",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_AiHintLogs_ParticipationId",
                table: "AiHintLogs",
                column: "ParticipationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiHintBudget",
                table: "GameChallenges");

            migrationBuilder.DropColumn(
                name: "AiHintsEnabled",
                table: "GameChallenges");

            migrationBuilder.DropColumn(
                name: "AiHintBudget",
                table: "ExerciseChallenges");

            migrationBuilder.DropColumn(
                name: "AiHintsEnabled",
                table: "ExerciseChallenges");

            migrationBuilder.DropTable(
                name: "AiHintLogs");
        }
    }
}
