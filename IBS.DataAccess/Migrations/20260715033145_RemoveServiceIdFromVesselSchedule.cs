using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveServiceIdFromVesselSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_msap_vessel_schedules_msap_services_service_id",
                table: "msap_vessel_schedules");

            migrationBuilder.DropIndex(
                name: "ix_msap_vessel_schedules_service_id",
                table: "msap_vessel_schedules");

            migrationBuilder.DropColumn(
                name: "service_id",
                table: "msap_vessel_schedules");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "service_id",
                table: "msap_vessel_schedules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_msap_vessel_schedules_service_id",
                table: "msap_vessel_schedules",
                column: "service_id");

            migrationBuilder.AddForeignKey(
                name: "fk_msap_vessel_schedules_msap_services_service_id",
                table: "msap_vessel_schedules",
                column: "service_id",
                principalTable: "msap_services",
                principalColumn: "service_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
