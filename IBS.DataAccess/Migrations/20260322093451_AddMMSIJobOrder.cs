using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddMMSIJobOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "job_order_id",
                table: "mmsi_dispatch_tickets",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "mmsi_job_orders",
                columns: table => new
                {
                    job_order_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    job_order_number = table.Column<string>(type: "varchar(20)", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "varchar(20)", nullable: false),
                    cos_number = table.Column<string>(type: "varchar(20)", nullable: true),
                    voyage_number = table.Column<string>(type: "varchar(100)", nullable: true),
                    remarks = table.Column<string>(type: "text", nullable: true),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    vessel_id = table.Column<int>(type: "integer", nullable: false),
                    port_id = table.Column<int>(type: "integer", nullable: true),
                    terminal_id = table.Column<int>(type: "integer", nullable: true),
                    service_id = table.Column<int>(type: "integer", nullable: true),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    cancellation_remarks = table.Column<string>(type: "varchar(255)", nullable: true),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_job_orders", x => x.job_order_id);
                    table.ForeignKey(
                        name: "fk_mmsi_job_orders_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mmsi_job_orders_mmsi_ports_port_id",
                        column: x => x.port_id,
                        principalTable: "mmsi_ports",
                        principalColumn: "port_id");
                    table.ForeignKey(
                        name: "fk_mmsi_job_orders_mmsi_services_service_id",
                        column: x => x.service_id,
                        principalTable: "mmsi_services",
                        principalColumn: "service_id");
                    table.ForeignKey(
                        name: "fk_mmsi_job_orders_mmsi_terminals_terminal_id",
                        column: x => x.terminal_id,
                        principalTable: "mmsi_terminals",
                        principalColumn: "terminal_id");
                    table.ForeignKey(
                        name: "fk_mmsi_job_orders_mmsi_vessels_vessel_id",
                        column: x => x.vessel_id,
                        principalTable: "mmsi_vessels",
                        principalColumn: "vessel_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_dispatch_tickets_job_order_id",
                table: "mmsi_dispatch_tickets",
                column: "job_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_job_orders_customer_id",
                table: "mmsi_job_orders",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_job_orders_port_id",
                table: "mmsi_job_orders",
                column: "port_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_job_orders_service_id",
                table: "mmsi_job_orders",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_job_orders_terminal_id",
                table: "mmsi_job_orders",
                column: "terminal_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_job_orders_vessel_id",
                table: "mmsi_job_orders",
                column: "vessel_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_job_orders_job_order_id",
                table: "mmsi_dispatch_tickets",
                column: "job_order_id",
                principalTable: "mmsi_job_orders",
                principalColumn: "job_order_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_dispatch_tickets_mmsi_job_orders_job_order_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropTable(
                name: "mmsi_job_orders");

            migrationBuilder.DropIndex(
                name: "ix_mmsi_dispatch_tickets_job_order_id",
                table: "mmsi_dispatch_tickets");

            migrationBuilder.DropColumn(
                name: "job_order_id",
                table: "mmsi_dispatch_tickets");
        }
    }
}
