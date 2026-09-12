using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndustrialPlatform.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBannerAds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "job_ad_id",
                table: "payment_transactions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "banner_ad_id",
                table: "payment_transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "banner_ads",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    destination_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    placement = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_fee_paid = table.Column<bool>(type: "boolean", nullable: false),
                    rejection_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    submitted_for_review_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    activated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_banner_ads", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_payment_transactions_banner_ad_id",
                table: "payment_transactions",
                column: "banner_ad_id");

            migrationBuilder.CreateIndex(
                name: "ix_banner_ads_company_id",
                table: "banner_ads",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_banner_ads_status",
                table: "banner_ads",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "banner_ads");

            migrationBuilder.DropIndex(
                name: "ix_payment_transactions_banner_ad_id",
                table: "payment_transactions");

            migrationBuilder.DropColumn(
                name: "banner_ad_id",
                table: "payment_transactions");

            migrationBuilder.AlterColumn<Guid>(
                name: "job_ad_id",
                table: "payment_transactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
