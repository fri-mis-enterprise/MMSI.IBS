using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddJobOrderUserAccessColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "can_close_job_order",
                table: "mmsi_user_accesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_create_job_order",
                table: "mmsi_user_accesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_delete_job_order",
                table: "mmsi_user_accesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_edit_job_order",
                table: "mmsi_user_accesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "can_close_job_order",
                table: "mmsi_user_accesses");

            migrationBuilder.DropColumn(
                name: "can_create_job_order",
                table: "mmsi_user_accesses");

            migrationBuilder.DropColumn(
                name: "can_delete_job_order",
                table: "mmsi_user_accesses");

            migrationBuilder.DropColumn(
                name: "can_edit_job_order",
                table: "mmsi_user_accesses");
        }
    }
}
