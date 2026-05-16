using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddRecIdToAllMasterFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "msap_recid",
                table: "mmsi_vessels",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "msap_recid",
                table: "mmsi_tugboats",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "msap_recid",
                table: "mmsi_tugboat_owners",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "tug_master_name",
                table: "mmsi_tug_masters",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "msap_recid",
                table: "mmsi_tug_masters",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "msap_recid",
                table: "mmsi_services",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "msap_recid",
                table: "mmsi_principals",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "msap_recid",
                table: "customers",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "msap_recid",
                table: "mmsi_vessels");

            migrationBuilder.DropColumn(
                name: "msap_recid",
                table: "mmsi_tugboats");

            migrationBuilder.DropColumn(
                name: "msap_recid",
                table: "mmsi_tugboat_owners");

            migrationBuilder.DropColumn(
                name: "msap_recid",
                table: "mmsi_tug_masters");

            migrationBuilder.DropColumn(
                name: "msap_recid",
                table: "mmsi_services");

            migrationBuilder.DropColumn(
                name: "msap_recid",
                table: "mmsi_principals");

            migrationBuilder.DropColumn(
                name: "msap_recid",
                table: "customers");

            migrationBuilder.AlterColumn<string>(
                name: "tug_master_name",
                table: "mmsi_tug_masters",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50);
        }
    }
}
