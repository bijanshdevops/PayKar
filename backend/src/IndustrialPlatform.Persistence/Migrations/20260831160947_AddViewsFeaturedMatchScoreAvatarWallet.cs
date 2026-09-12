using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndustrialPlatform.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddViewsFeaturedMatchScoreAvatarWallet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "match_score_percent",
                table: "job_applications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_featured",
                table: "job_ads",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "views_count",
                table: "job_ads",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "wallet_balance_in_rials",
                table: "companies",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "avatar_url",
                table: "candidates",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_job_ads_is_featured",
                table: "job_ads",
                column: "is_featured");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_job_ads_is_featured",
                table: "job_ads");

            migrationBuilder.DropColumn(
                name: "match_score_percent",
                table: "job_applications");

            migrationBuilder.DropColumn(
                name: "is_featured",
                table: "job_ads");

            migrationBuilder.DropColumn(
                name: "views_count",
                table: "job_ads");

            migrationBuilder.DropColumn(
                name: "wallet_balance_in_rials",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "avatar_url",
                table: "candidates");
        }
    }
}
