using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndustrialPlatform.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportTicketRequesterMobileNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "requester_mobile_number",
                table: "support_tickets",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "requester_mobile_number",
                table: "support_tickets");
        }
    }
}
