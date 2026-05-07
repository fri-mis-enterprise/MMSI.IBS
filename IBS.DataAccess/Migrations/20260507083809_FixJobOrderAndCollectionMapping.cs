using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class FixJobOrderAndCollectionMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_billings_job_order_id",
                table: "billings",
                column: "job_order_id");

            migrationBuilder.AddForeignKey(
                name: "fk_billings_mmsi_job_orders_job_order_id",
                table: "billings",
                column: "job_order_id",
                principalTable: "mmsi_job_orders",
                principalColumn: "job_order_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_billings_mmsi_job_orders_job_order_id",
                table: "billings");

            migrationBuilder.DropIndex(
                name: "ix_billings_job_order_id",
                table: "billings");
        }
    }
}
