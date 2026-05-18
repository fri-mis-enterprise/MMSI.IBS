using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPortIdToTugboat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "port_id",
                table: "mmsi_tugboats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_tugboats_port_id",
                table: "mmsi_tugboats",
                column: "port_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mmsi_tugboats_mmsi_ports_port_id",
                table: "mmsi_tugboats",
                column: "port_id",
                principalTable: "mmsi_ports",
                principalColumn: "port_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mmsi_tugboats_mmsi_ports_port_id",
                table: "mmsi_tugboats");

            migrationBuilder.DropIndex(
                name: "ix_mmsi_tugboats_port_id",
                table: "mmsi_tugboats");

            migrationBuilder.DropColumn(
                name: "port_id",
                table: "mmsi_tugboats");
        }
    }
}
