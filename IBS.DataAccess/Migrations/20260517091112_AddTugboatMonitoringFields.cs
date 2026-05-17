using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddTugboatMonitoringFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "planned_end_time",
                table: "mmsi_job_orders",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "planned_start_time",
                table: "mmsi_job_orders",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "preferred_tugboat_id",
                table: "mmsi_job_orders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_job_orders_preferred_tugboat_id",
                table: "mmsi_job_orders",
                column: "preferred_tugboat_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_job_orders_mmsi_tugboats_preferred_tugboat_id",
                table: "mmsi_job_orders",
                column: "preferred_tugboat_id",
                principalTable: "mmsi_tugboats",
                principalColumn: "tugboat_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_job_orders_mmsi_tugboats_preferred_tugboat_id",
                table: "mmsi_job_orders");

            migrationBuilder.DropIndex(
                name: "ix_mmsi_job_orders_preferred_tugboat_id",
                table: "mmsi_job_orders");

            migrationBuilder.DropColumn(
                name: "planned_end_time",
                table: "mmsi_job_orders");

            migrationBuilder.DropColumn(
                name: "planned_start_time",
                table: "mmsi_job_orders");

            migrationBuilder.DropColumn(
                name: "preferred_tugboat_id",
                table: "mmsi_job_orders");
        }
    }
}
