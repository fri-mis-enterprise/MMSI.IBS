using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBillingNumberOnDispatchTicket2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_msap_dispatch_tickets_msap_job_orders_job_order_id1",
                table: "msap_dispatch_tickets");

            migrationBuilder.DropIndex(
                name: "ix_msap_dispatch_tickets_job_order_id1",
                table: "msap_dispatch_tickets");

            migrationBuilder.DropColumn(
                name: "job_order_id1",
                table: "msap_dispatch_tickets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "job_order_id1",
                table: "msap_dispatch_tickets",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_msap_dispatch_tickets_job_order_id1",
                table: "msap_dispatch_tickets",
                column: "job_order_id1");

            migrationBuilder.AddForeignKey(
                name: "fk_msap_dispatch_tickets_msap_job_orders_job_order_id1",
                table: "msap_dispatch_tickets",
                column: "job_order_id1",
                principalTable: "msap_job_orders",
                principalColumn: "job_order_id");
        }
    }
}
