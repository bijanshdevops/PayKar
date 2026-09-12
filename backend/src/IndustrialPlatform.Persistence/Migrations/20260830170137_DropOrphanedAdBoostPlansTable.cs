using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndustrialPlatform.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropOrphanedAdBoostPlansTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // جدول ad_boost_plans یک بازمانده (Orphan) از migration InitialCreate است که با تاریخ
            // جدیدتر از BusinessFoundation_AdminApprovalAndFlatFee تولید شده و آن را دوباره ساخته،
            // درحالی‌که مدل فعلی EF هیچ Entity ای برای آن ندارد. چون هیچ Entity ای در مدل وجود ندارد،
            // DropTable معمولی قابل تولید نیست؛ به همین دلیل از SQL خام استفاده می‌شود.
            migrationBuilder.Sql("DROP TABLE IF EXISTS ad_boost_plans;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ad_boost_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    duration_days = table.Column<int>(type: "integer", nullable: false),
                    price_in_rials = table.Column<long>(type: "bigint", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ad_boost_plans", x => x.id);
                });
        }
    }
}
