using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace IndustrialPlatform.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BusinessFoundation_AdminApprovalAndFlatFee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ad_boost_plans");

            migrationBuilder.DropColumn(
                name: "plan_id",
                table: "payment_transactions");

            migrationBuilder.AddColumn<string>(
                name: "purpose",
                table: "payment_transactions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "additional_benefits",
                table: "job_ads",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "application_deadline_utc",
                table: "job_ads",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contract_type",
                table: "job_ads",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "gender_preference",
                table: "job_ads",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "headcount_needed",
                table: "job_ads",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_fee_paid",
                table: "job_ads",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "max_age",
                table: "job_ads",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "military_service_status",
                table: "job_ads",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "min_age",
                table: "job_ads",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "min_education_level",
                table: "job_ads",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "min_experience_years",
                table: "job_ads",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rejection_reason",
                table: "job_ads",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "required_skills",
                table: "job_ads",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "submitted_for_review_at_utc",
                table: "job_ads",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_job_ads_status",
                table: "job_ads",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_job_ads_status",
                table: "job_ads");

            migrationBuilder.DropColumn(
                name: "purpose",
                table: "payment_transactions");

            migrationBuilder.DropColumn(
                name: "additional_benefits",
                table: "job_ads");

            migrationBuilder.DropColumn(
                name: "application_deadline_utc",
                table: "job_ads");

            migrationBuilder.DropColumn(
                name: "contract_type",
                table: "job_ads");

            migrationBuilder.DropColumn(
                name: "gender_preference",
                table: "job_ads");

            migrationBuilder.DropColumn(
                name: "headcount_needed",
                table: "job_ads");

            migrationBuilder.DropColumn(
                name: "is_fee_paid",
                table: "job_ads");

            migrationBuilder.DropColumn(
                name: "max_age",
                table: "job_ads");

            migrationBuilder.DropColumn(
                name: "military_service_status",
                table: "job_ads");

            migrationBuilder.DropColumn(
                name: "min_age",
                table: "job_ads");

            migrationBuilder.DropColumn(
                name: "min_education_level",
                table: "job_ads");

            migrationBuilder.DropColumn(
                name: "min_experience_years",
                table: "job_ads");

            migrationBuilder.DropColumn(
                name: "rejection_reason",
                table: "job_ads");

            migrationBuilder.DropColumn(
                name: "required_skills",
                table: "job_ads");

            migrationBuilder.DropColumn(
                name: "submitted_for_review_at_utc",
                table: "job_ads");

            migrationBuilder.AddColumn<Guid>(
                name: "plan_id",
                table: "payment_transactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "ad_boost_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    duration_days = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    price_in_rials = table.Column<long>(type: "bigint", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ad_boost_plans", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "ad_boost_plans",
                columns: new[] { "id", "created_at_utc", "created_by", "deleted_at_utc", "deleted_by", "duration_days", "is_active", "last_modified_at_utc", "last_modified_by", "name", "price_in_rials" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 7, true, null, null, "پلن ۷ روزه", 500000L },
                    { new Guid("10000000-0000-0000-0000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 15, true, null, null, "پلن ۱۵ روزه", 850000L },
                    { new Guid("10000000-0000-0000-0000-000000000003"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 30, true, null, null, "پلن ۳۰ روزه", 1500000L }
                });
        }
    }
}
