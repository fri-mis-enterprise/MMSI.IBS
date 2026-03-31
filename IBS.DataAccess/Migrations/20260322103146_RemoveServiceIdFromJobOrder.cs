using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveServiceIdFromJobOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_job_orders_mmsi_services_service_id",
                table: "mmsi_job_orders");

            migrationBuilder.DropIndex(
                name: "ix_mmsi_job_orders_service_id",
                table: "mmsi_job_orders");

            migrationBuilder.DropColumn(
                name: "service_id",
                table: "mmsi_job_orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "service_id",
                table: "mmsi_job_orders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_job_orders_service_id",
                table: "mmsi_job_orders",
                column: "service_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_job_orders_mmsi_services_service_id",
                table: "mmsi_job_orders",
                column: "service_id",
                principalTable: "mmsi_services",
                principalColumn: "service_id");
        }
    }
}
