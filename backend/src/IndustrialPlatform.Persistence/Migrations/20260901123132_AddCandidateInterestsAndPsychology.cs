using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndustrialPlatform.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateInterestsAndPsychology : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "interests",
                table: "candidates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "psychology_answers",
                table: "candidates",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "interests",
                table: "candidates");

            migrationBuilder.DropColumn(
                name: "psychology_answers",
                table: "candidates");
        }
    }
}
