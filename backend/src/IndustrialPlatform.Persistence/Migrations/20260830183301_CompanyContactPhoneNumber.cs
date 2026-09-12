using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndustrialPlatform.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompanyContactPhoneNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "contact_phone_number",
                table: "companies",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "contact_phone_number",
                table: "companies");
        }
    }
}
