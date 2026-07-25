using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AuditCodeCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "log_messages");

            migrationBuilder.DropTable(
                name: "msap_modules");

            migrationBuilder.DropTable(
                name: "msap_rates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "log_messages",
                columns: table => new
                {
                    log_id = table.Column<Guid>(type: "uuid", nullable: false),
                    log_level = table.Column<string>(type: "text", nullable: false),
                    logger_name = table.Column<string>(type: "text", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    time_stamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_log_messages", x => x.log_id);
                });

            migrationBuilder.CreateTable(
                name: "msap_modules",
                columns: table => new
                {
                    module_number = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "varchar(100)", nullable: false),
                    module_name = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_msap_modules", x => x.module_number);
                });

            migrationBuilder.CreateTable(
                name: "msap_rates",
                columns: table => new
                {
                    rate_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    as_of = table.Column<DateOnly>(type: "date", nullable: false),
                    type = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_msap_rates", x => x.rate_id);
                });
        }
    }
}
