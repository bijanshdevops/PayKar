using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace IndustrialPlatform.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBannerSlotsAndRefactorBannerAds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "banner_slot_id",
                table: "banner_ads",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "clicks_count",
                table: "banner_ads",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "duration_days",
                table: "banner_ads",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<DateTime>(
                name: "end_date",
                table: "banner_ads",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "impressions_count",
                table: "banner_ads",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "start_date",
                table: "banner_ads",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "total_amount",
                table: "banner_ads",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "banner_slots",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    placement = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    dimensions = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    daily_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("pk_banner_slots", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "banner_slots",
                columns: new[] { "id", "created_at_utc", "created_by", "daily_price", "deleted_at_utc", "deleted_by", "dimensions", "is_active", "last_modified_at_utc", "last_modified_by", "placement", "title" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 9, 4, 0, 0, 0, 0, DateTimeKind.Utc), null, 550000m, null, null, "1200x300", true, null, null, "Home", "جایگاه هدر صفحه اصلی" },
                    { 2, new DateTime(2026, 9, 4, 0, 0, 0, 0, DateTimeKind.Utc), null, 350000m, null, null, "728x90", true, null, null, "JobAdList", "جایگاه لیست آگهی‌ها" },
                    { 3, new DateTime(2026, 9, 4, 0, 0, 0, 0, DateTimeKind.Utc), null, 150000m, null, null, "728x90", true, null, null, "Both", "جایگاه بنر میانی / فوتر" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_banner_ads_banner_slot_id",
                table: "banner_ads",
                column: "banner_slot_id");

            migrationBuilder.CreateIndex(
                name: "ix_banner_slots_placement",
                table: "banner_slots",
                column: "placement");

            migrationBuilder.AddForeignKey(
                name: "fk_banner_ads_banner_slots_banner_slot_id",
                table: "banner_ads",
                column: "banner_slot_id",
                principalTable: "banner_slots",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_banner_ads_banner_slots_banner_slot_id",
                table: "banner_ads");

            migrationBuilder.DropTable(
                name: "banner_slots");

            migrationBuilder.DropIndex(
                name: "ix_banner_ads_banner_slot_id",
                table: "banner_ads");

            migrationBuilder.DropColumn(
                name: "banner_slot_id",
                table: "banner_ads");

            migrationBuilder.DropColumn(
                name: "clicks_count",
                table: "banner_ads");

            migrationBuilder.DropColumn(
                name: "duration_days",
                table: "banner_ads");

            migrationBuilder.DropColumn(
                name: "end_date",
                table: "banner_ads");

            migrationBuilder.DropColumn(
                name: "impressions_count",
                table: "banner_ads");

            migrationBuilder.DropColumn(
                name: "start_date",
                table: "banner_ads");

            migrationBuilder.DropColumn(
                name: "total_amount",
                table: "banner_ads");
        }
    }
}
