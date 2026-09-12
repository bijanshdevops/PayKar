using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndustrialPlatform.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateResumeProfileModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "github_url",
                table: "candidates",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_actively_looking_for_job",
                table: "candidates",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "job_title",
                table: "candidates",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "linkedin_url",
                table: "candidates",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "max_requested_salary_in_toman",
                table: "candidates",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "min_requested_salary_in_toman",
                table: "candidates",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "personal_website_url",
                table: "candidates",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "preferred_work_type",
                table: "candidates",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "professional_summary",
                table: "candidates",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "candidate_certifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    issuing_organization = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    year_obtained = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_candidate_certifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_candidate_certifications_candidates_candidate_id",
                        column: x => x.candidate_id,
                        principalTable: "candidates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "candidate_languages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    proficiency_level = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_candidate_languages", x => x.id);
                    table.ForeignKey(
                        name: "fk_candidate_languages_candidates_candidate_id",
                        column: x => x.candidate_id,
                        principalTable: "candidates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "candidate_skills",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_candidate_skills", x => x.id);
                    table.ForeignKey(
                        name: "fk_candidate_skills_candidates_candidate_id",
                        column: x => x.candidate_id,
                        principalTable: "candidates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_candidate_certifications_candidate_id",
                table: "candidate_certifications",
                column: "candidate_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_languages_candidate_id",
                table: "candidate_languages",
                column: "candidate_id");

            migrationBuilder.CreateIndex(
                name: "ux_candidate_languages_candidate_id_name",
                table: "candidate_languages",
                columns: new[] { "candidate_id", "name" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_skills_candidate_id",
                table: "candidate_skills",
                column: "candidate_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "candidate_certifications");

            migrationBuilder.DropTable(
                name: "candidate_languages");

            migrationBuilder.DropTable(
                name: "candidate_skills");

            migrationBuilder.DropColumn(
                name: "github_url",
                table: "candidates");

            migrationBuilder.DropColumn(
                name: "is_actively_looking_for_job",
                table: "candidates");

            migrationBuilder.DropColumn(
                name: "job_title",
                table: "candidates");

            migrationBuilder.DropColumn(
                name: "linkedin_url",
                table: "candidates");

            migrationBuilder.DropColumn(
                name: "max_requested_salary_in_toman",
                table: "candidates");

            migrationBuilder.DropColumn(
                name: "min_requested_salary_in_toman",
                table: "candidates");

            migrationBuilder.DropColumn(
                name: "personal_website_url",
                table: "candidates");

            migrationBuilder.DropColumn(
                name: "preferred_work_type",
                table: "candidates");

            migrationBuilder.DropColumn(
                name: "professional_summary",
                table: "candidates");
        }
    }
}
