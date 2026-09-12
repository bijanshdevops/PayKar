using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndustrialPlatform.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ResumeCreationFee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "company_id",
                table: "payment_transactions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "candidate_id",
                table: "payment_transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_fee_paid",
                table: "candidates",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_transactions_candidate_id",
                table: "payment_transactions",
                column: "candidate_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_payment_transactions_candidate_id",
                table: "payment_transactions");

            migrationBuilder.DropColumn(
                name: "candidate_id",
                table: "payment_transactions");

            migrationBuilder.DropColumn(
                name: "is_fee_paid",
                table: "candidates");

            migrationBuilder.AlterColumn<Guid>(
                name: "company_id",
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
