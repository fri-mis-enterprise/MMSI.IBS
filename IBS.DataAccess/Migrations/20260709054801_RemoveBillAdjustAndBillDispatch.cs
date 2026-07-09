using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBillAdjustAndBillDispatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "msap_bill_adjustments");

            migrationBuilder.DropTable(
                name: "msap_bill_dispatches");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "msap_bill_adjustments",
                columns: table => new
                {
                    bill_adjust_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    billing_id = table.Column<int>(type: "integer", nullable: false),
                    dispatch_ticket_id = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    billing_number = table.Column<string>(type: "varchar(10)", nullable: false),
                    dispatch_number = table.Column<string>(type: "varchar(20)", nullable: false),
                    rate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_msap_bill_adjustments", x => x.bill_adjust_id);
                    table.ForeignKey(
                        name: "fk_msap_bill_adjustments_msap_billings_billing_id",
                        column: x => x.billing_id,
                        principalTable: "msap_billings",
                        principalColumn: "RECID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_msap_bill_adjustments_msap_dispatch_tickets_dispatch_ticket",
                        column: x => x.dispatch_ticket_id,
                        principalTable: "msap_dispatch_tickets",
                        principalColumn: "RECID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "msap_bill_dispatches",
                columns: table => new
                {
                    bill_dispatch_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    billing_id = table.Column<int>(type: "integer", nullable: false),
                    dispatch_ticket_id = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ap_other_tug = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    billing_number = table.Column<string>(type: "varchar(10)", nullable: false),
                    dispatch_number = table.Column<string>(type: "varchar(20)", nullable: false),
                    rate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_msap_bill_dispatches", x => x.bill_dispatch_id);
                    table.ForeignKey(
                        name: "fk_msap_bill_dispatches_msap_billings_billing_id",
                        column: x => x.billing_id,
                        principalTable: "msap_billings",
                        principalColumn: "RECID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_msap_bill_dispatches_msap_dispatch_tickets_dispatch_ticket_",
                        column: x => x.dispatch_ticket_id,
                        principalTable: "msap_dispatch_tickets",
                        principalColumn: "RECID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_msap_bill_adjustments_billing_id",
                table: "msap_bill_adjustments",
                column: "billing_id");

            migrationBuilder.CreateIndex(
                name: "ix_msap_bill_adjustments_dispatch_ticket_id",
                table: "msap_bill_adjustments",
                column: "dispatch_ticket_id");

            migrationBuilder.CreateIndex(
                name: "ix_msap_bill_dispatches_billing_id",
                table: "msap_bill_dispatches",
                column: "billing_id");

            migrationBuilder.CreateIndex(
                name: "ix_msap_bill_dispatches_dispatch_ticket_id",
                table: "msap_bill_dispatches",
                column: "dispatch_ticket_id");
        }
    }
}
