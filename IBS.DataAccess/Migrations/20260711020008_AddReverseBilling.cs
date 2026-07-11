using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddReverseBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "can_reverse_billing",
                table: "msap_user_accesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "unpost_remarks",
                table: "msap_billings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "unposted_by",
                table: "msap_billings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "unposted_date",
                table: "msap_billings",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "can_reverse_billing",
                table: "msap_user_accesses");

            migrationBuilder.DropColumn(
                name: "unpost_remarks",
                table: "msap_billings");

            migrationBuilder.DropColumn(
                name: "unposted_by",
                table: "msap_billings");

            migrationBuilder.DropColumn(
                name: "unposted_date",
                table: "msap_billings");
        }
    }
}
