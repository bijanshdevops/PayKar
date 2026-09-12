using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndustrialPlatform.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJobApplicationEmployerNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "company_notes",
                table: "job_applications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "interview_date_time_utc",
                table: "job_applications",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "company_notes",
                table: "job_applications");

            migrationBuilder.DropColumn(
                name: "interview_date_time_utc",
                table: "job_applications");
        }
    }
}
