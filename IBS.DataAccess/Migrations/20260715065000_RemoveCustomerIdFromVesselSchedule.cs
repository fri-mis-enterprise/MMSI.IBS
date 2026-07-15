using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCustomerIdFromVesselSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_msap_vessel_schedules_customers_customer_id",
                table: "msap_vessel_schedules");

            migrationBuilder.DropIndex(
                name: "ix_msap_vessel_schedules_customer_id",
                table: "msap_vessel_schedules");

            migrationBuilder.DropColumn(
                name: "customer_id",
                table: "msap_vessel_schedules");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "customer_id",
                table: "msap_vessel_schedules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_msap_vessel_schedules_customer_id",
                table: "msap_vessel_schedules",
                column: "customer_id");

            migrationBuilder.AddForeignKey(
                name: "fk_msap_vessel_schedules_customers_customer_id",
                table: "msap_vessel_schedules",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
