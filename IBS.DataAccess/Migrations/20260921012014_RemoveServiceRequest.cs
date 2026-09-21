using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveServiceRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE msap_dispatch_tickets
                SET status = CASE status
                    WHEN 'Draft' THEN 'For Tariff'
                    WHEN 'Requested' THEN 'For Tariff'
                    WHEN 'Service Request Deleted' THEN 'Deleted'
                    ELSE status
                END
                WHERE status IN ('Draft', 'Requested', 'Service Request Deleted');
                """);

            migrationBuilder.DropColumn(
                name: "can_create_service_request",
                table: "msap_user_accesses");

            migrationBuilder.DropColumn(
                name: "can_post_service_request",
                table: "msap_user_accesses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "can_create_service_request",
                table: "msap_user_accesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_post_service_request",
                table: "msap_user_accesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
