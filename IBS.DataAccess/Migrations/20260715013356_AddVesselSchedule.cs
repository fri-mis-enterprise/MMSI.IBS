using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddVesselSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "msap_vessel_schedules",
                columns: table => new
                {
                    vessel_schedule_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    vessel_id = table.Column<int>(type: "integer", nullable: false),
                    port_id = table.Column<int>(type: "integer", nullable: false),
                    terminal_id = table.Column<int>(type: "integer", nullable: false),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    service_id = table.Column<int>(type: "integer", nullable: false),
                    planned_start = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    planned_end = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    required_tug_count = table.Column<int>(type: "integer", nullable: false),
                    assigned_tugboat_ids = table.Column<string>(type: "text", nullable: true),
                    voyage_number = table.Column<string>(type: "varchar(50)", nullable: true),
                    vessel_type = table.Column<string>(type: "varchar(20)", nullable: true),
                    status = table.Column<string>(type: "varchar(20)", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    job_order_id = table.Column<int>(type: "integer", nullable: true),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_msap_vessel_schedules", x => x.vessel_schedule_id);
                    table.ForeignKey(
                        name: "fk_msap_vessel_schedules_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_msap_vessel_schedules_msap_job_orders_job_order_id",
                        column: x => x.job_order_id,
                        principalTable: "msap_job_orders",
                        principalColumn: "job_order_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_msap_vessel_schedules_msap_ports_port_id",
                        column: x => x.port_id,
                        principalTable: "msap_ports",
                        principalColumn: "port_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_msap_vessel_schedules_msap_services_service_id",
                        column: x => x.service_id,
                        principalTable: "msap_services",
                        principalColumn: "service_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_msap_vessel_schedules_msap_terminals_terminal_id",
                        column: x => x.terminal_id,
                        principalTable: "msap_terminals",
                        principalColumn: "terminal_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_msap_vessel_schedules_msap_vessels_vessel_id",
                        column: x => x.vessel_id,
                        principalTable: "msap_vessels",
                        principalColumn: "vessel_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_msap_vessel_schedules_customer_id",
                table: "msap_vessel_schedules",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_msap_vessel_schedules_job_order_id",
                table: "msap_vessel_schedules",
                column: "job_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_msap_vessel_schedules_planned_start",
                table: "msap_vessel_schedules",
                column: "planned_start");

            migrationBuilder.CreateIndex(
                name: "ix_msap_vessel_schedules_port_id_terminal_id",
                table: "msap_vessel_schedules",
                columns: new[] { "port_id", "terminal_id" });

            migrationBuilder.CreateIndex(
                name: "ix_msap_vessel_schedules_service_id",
                table: "msap_vessel_schedules",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_msap_vessel_schedules_status",
                table: "msap_vessel_schedules",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_msap_vessel_schedules_terminal_id",
                table: "msap_vessel_schedules",
                column: "terminal_id");

            migrationBuilder.CreateIndex(
                name: "ix_msap_vessel_schedules_vessel_id",
                table: "msap_vessel_schedules",
                column: "vessel_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "msap_vessel_schedules");
        }
    }
}
