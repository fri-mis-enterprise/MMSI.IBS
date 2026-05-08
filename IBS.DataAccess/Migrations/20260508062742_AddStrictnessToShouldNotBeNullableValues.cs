using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddStrictnessToShouldNotBeNullableValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_billings_customers_customer_id",
                table: "billings");

            migrationBuilder.DropForeignKey(
                name: "fk_billings_mmsi_ports_port_id",
                table: "billings");

            migrationBuilder.DropForeignKey(
                name: "fk_billings_mmsi_terminals_terminal_id",
                table: "billings");

            migrationBuilder.DropForeignKey(
                name: "fk_billings_mmsi_vessels_vessel_id",
                table: "billings");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_dispatch_tickets_customers_customer_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_ports_port_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_services_service_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_terminals_terminal_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_tugboats_tug_boat_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_vessels_vessel_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_job_orders_mmsi_ports_port_id",
                table: "mmsi_job_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_job_orders_mmsi_terminals_terminal_id",
                table: "mmsi_job_orders");

            migrationBuilder.AlterColumn<int>(
                name: "terminal_id",
                table: "mmsi_job_orders",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "port_id",
                table: "mmsi_job_orders",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "vessel_id",
                table: "mmsi_dispatch_tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "tug_boat_id",
                table: "mmsi_dispatch_tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "terminal_id",
                table: "mmsi_dispatch_tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "service_id",
                table: "mmsi_dispatch_tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "port_id",
                table: "mmsi_dispatch_tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "customer_id",
                table: "mmsi_dispatch_tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "vessel_id",
                table: "billings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "terminal_id",
                table: "billings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "port_id",
                table: "billings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "customer_id",
                table: "billings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_billings_customers_customer_id",
                table: "billings",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_billings_mmsi_ports_port_id",
                table: "billings",
                column: "port_id",
                principalTable: "mmsi_ports",
                principalColumn: "port_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_billings_mmsi_terminals_terminal_id",
                table: "billings",
                column: "terminal_id",
                principalTable: "mmsi_terminals",
                principalColumn: "terminal_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_billings_mmsi_vessels_vessel_id",
                table: "billings",
                column: "vessel_id",
                principalTable: "mmsi_vessels",
                principalColumn: "vessel_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_dispatch_tickets_customers_customer_id",
                table: "mmsi_dispatch_tickets",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_ports_port_id",
                table: "mmsi_dispatch_tickets",
                column: "port_id",
                principalTable: "mmsi_ports",
                principalColumn: "port_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_services_service_id",
                table: "mmsi_dispatch_tickets",
                column: "service_id",
                principalTable: "mmsi_services",
                principalColumn: "service_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_terminals_terminal_id",
                table: "mmsi_dispatch_tickets",
                column: "terminal_id",
                principalTable: "mmsi_terminals",
                principalColumn: "terminal_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_tugboats_tug_boat_id",
                table: "mmsi_dispatch_tickets",
                column: "tug_boat_id",
                principalTable: "mmsi_tugboats",
                principalColumn: "tugboat_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_vessels_vessel_id",
                table: "mmsi_dispatch_tickets",
                column: "vessel_id",
                principalTable: "mmsi_vessels",
                principalColumn: "vessel_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_job_orders_mmsi_ports_port_id",
                table: "mmsi_job_orders",
                column: "port_id",
                principalTable: "mmsi_ports",
                principalColumn: "port_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_job_orders_mmsi_terminals_terminal_id",
                table: "mmsi_job_orders",
                column: "terminal_id",
                principalTable: "mmsi_terminals",
                principalColumn: "terminal_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_billings_customers_customer_id",
                table: "billings");

            migrationBuilder.DropForeignKey(
                name: "fk_billings_mmsi_ports_port_id",
                table: "billings");

            migrationBuilder.DropForeignKey(
                name: "fk_billings_mmsi_terminals_terminal_id",
                table: "billings");

            migrationBuilder.DropForeignKey(
                name: "fk_billings_mmsi_vessels_vessel_id",
                table: "billings");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_dispatch_tickets_customers_customer_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_ports_port_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_services_service_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_terminals_terminal_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_tugboats_tug_boat_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_vessels_vessel_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_job_orders_mmsi_ports_port_id",
                table: "mmsi_job_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_job_orders_mmsi_terminals_terminal_id",
                table: "mmsi_job_orders");

            migrationBuilder.AlterColumn<int>(
                name: "terminal_id",
                table: "mmsi_job_orders",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "port_id",
                table: "mmsi_job_orders",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "vessel_id",
                table: "mmsi_dispatch_tickets",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "tug_boat_id",
                table: "mmsi_dispatch_tickets",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "terminal_id",
                table: "mmsi_dispatch_tickets",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "service_id",
                table: "mmsi_dispatch_tickets",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "port_id",
                table: "mmsi_dispatch_tickets",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "customer_id",
                table: "mmsi_dispatch_tickets",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "vessel_id",
                table: "billings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "terminal_id",
                table: "billings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "port_id",
                table: "billings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "customer_id",
                table: "billings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "fk_billings_customers_customer_id",
                table: "billings",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "customer_id");

            migrationBuilder.AddForeignKey(
                name: "fk_billings_mmsi_ports_port_id",
                table: "billings",
                column: "port_id",
                principalTable: "mmsi_ports",
                principalColumn: "port_id");

            migrationBuilder.AddForeignKey(
                name: "fk_billings_mmsi_terminals_terminal_id",
                table: "billings",
                column: "terminal_id",
                principalTable: "mmsi_terminals",
                principalColumn: "terminal_id");

            migrationBuilder.AddForeignKey(
                name: "fk_billings_mmsi_vessels_vessel_id",
                table: "billings",
                column: "vessel_id",
                principalTable: "mmsi_vessels",
                principalColumn: "vessel_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_dispatch_tickets_customers_customer_id",
                table: "mmsi_dispatch_tickets",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "customer_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_ports_port_id",
                table: "mmsi_dispatch_tickets",
                column: "port_id",
                principalTable: "mmsi_ports",
                principalColumn: "port_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_services_service_id",
                table: "mmsi_dispatch_tickets",
                column: "service_id",
                principalTable: "mmsi_services",
                principalColumn: "service_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_terminals_terminal_id",
                table: "mmsi_dispatch_tickets",
                column: "terminal_id",
                principalTable: "mmsi_terminals",
                principalColumn: "terminal_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_tugboats_tug_boat_id",
                table: "mmsi_dispatch_tickets",
                column: "tug_boat_id",
                principalTable: "mmsi_tugboats",
                principalColumn: "tugboat_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_vessels_vessel_id",
                table: "mmsi_dispatch_tickets",
                column: "vessel_id",
                principalTable: "mmsi_vessels",
                principalColumn: "vessel_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_job_orders_mmsi_ports_port_id",
                table: "mmsi_job_orders",
                column: "port_id",
                principalTable: "mmsi_ports",
                principalColumn: "port_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_job_orders_mmsi_terminals_terminal_id",
                table: "mmsi_job_orders",
                column: "terminal_id",
                principalTable: "mmsi_terminals",
                principalColumn: "terminal_id");
        }
    }
}
