using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndustrialPlatform.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketPriorityDepartmentAndAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
{
    // ۱) نگاشت وضعیت‌های قدیمی به مدل ۵ وضعیتی جدید — پیش از هر تغییر شِمایی.
    migrationBuilder.Sql("UPDATE support_tickets SET status = 'PendingResponse' WHERE status = 'Open';");
    migrationBuilder.Sql("UPDATE support_tickets SET status = 'Answered' WHERE status = 'Resolved';");

    // ۲) دپارتمان/اولویت — ستون NOT NULL با مقدار پیش‌فرض معقول برای رکوردهای موجود.
    migrationBuilder.AddColumn<string>(
        name: "department",
        table: "support_tickets",
        type: "character varying(50)",
        maxLength: 50,
        nullable: false,
        defaultValue: "General");

    migrationBuilder.AddColumn<string>(
        name: "priority",
        table: "support_tickets",
        type: "character varying(20)",
        maxLength: 20,
        nullable: false,
        defaultValue: "Medium");

    // ۳) شناسه خوانا (ticket_number) — ابتدا Nullable، بعد از پرکردن داده‌های قدیمی به NOT NULL تبدیل می‌شود.
    migrationBuilder.AddColumn<string>(
        name: "ticket_number",
        table: "support_tickets",
        type: "character varying(30)",
        maxLength: 30,
        nullable: true);

    migrationBuilder.Sql(@"
        WITH numbered AS (
            SELECT id, EXTRACT(YEAR FROM created_at_utc)::int AS yr,
                   ROW_NUMBER() OVER (PARTITION BY EXTRACT(YEAR FROM created_at_utc) ORDER BY created_at_utc) AS rn
            FROM support_tickets
        )
        UPDATE support_tickets t
        SET ticket_number = 'TK-' || numbered.yr || '-' || LPAD(numbered.rn::text, 4, '0')
        FROM numbered
        WHERE t.id = numbered.id;
    ");

    migrationBuilder.AlterColumn<string>(
        name: "ticket_number",
        table: "support_tickets",
        type: "character varying(30)",
        maxLength: 30,
        nullable: false);

    migrationBuilder.CreateIndex(
        name: "ux_support_tickets_ticket_number",
        table: "support_tickets",
        column: "ticket_number",
        unique: true);

    // ۴) ضمیمه فایل/تصویر پیام‌ها — کاملاً اختیاری.
    migrationBuilder.AddColumn<string>(
        name: "attachment_url",
        table: "support_messages",
        type: "character varying(500)",
        maxLength: 500,
        nullable: true);

    migrationBuilder.AddColumn<string>(
        name: "attachment_file_name",
        table: "support_messages",
        type: "character varying(255)",
        maxLength: 255,
        nullable: true);
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropColumn(name: "attachment_file_name", table: "support_messages");
    migrationBuilder.DropColumn(name: "attachment_url", table: "support_messages");

    migrationBuilder.DropIndex(name: "ux_support_tickets_ticket_number", table: "support_tickets");
    migrationBuilder.DropColumn(name: "ticket_number", table: "support_tickets");
    migrationBuilder.DropColumn(name: "priority", table: "support_tickets");
    migrationBuilder.DropColumn(name: "department", table: "support_tickets");

    migrationBuilder.Sql("UPDATE support_tickets SET status = 'Open' WHERE status = 'PendingResponse' OR status = 'Reopened';");
    migrationBuilder.Sql("UPDATE support_tickets SET status = 'Resolved' WHERE status = 'Answered';");
}

    }
}
