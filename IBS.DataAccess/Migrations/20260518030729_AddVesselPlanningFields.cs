using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddVesselPlanningFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_confirmed",
                table: "mmsi_job_orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "required_tug_count",
                table: "mmsi_job_orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "service_type",
                table: "mmsi_job_orders",
                type: "varchar(50)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_confirmed",
                table: "mmsi_job_orders");

            migrationBuilder.DropColumn(
                name: "required_tug_count",
                table: "mmsi_job_orders");

            migrationBuilder.DropColumn(
                name: "service_type",
                table: "mmsi_job_orders");
        }
    }
}
