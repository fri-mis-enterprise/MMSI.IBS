using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStaleModulesv2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gl_period_balances");

            migrationBuilder.DropTable(
                name: "gl_sub_account_balances");

            migrationBuilder.DropTable(
                name: "posted_periods");

            migrationBuilder.DropColumn(
                name: "cluster_code",
                table: "customers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "cluster_code",
                table: "customers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "gl_period_balances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    adjusted_ending_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    adjustment_credit_total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    adjustment_debit_total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    beginning_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    closed_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    company = table.Column<string>(type: "varchar(50)", nullable: false),
                    credit_total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    debit_total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ending_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    fiscal_period = table.Column<int>(type: "integer", nullable: false),
                    fiscal_year = table.Column<int>(type: "integer", nullable: false),
                    is_closed = table.Column<bool>(type: "boolean", nullable: false),
                    period_end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    period_start_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gl_period_balances", x => x.id);
                    table.ForeignKey(
                        name: "fk_gl_period_balances_chart_of_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "chart_of_accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "gl_sub_account_balances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    beginning_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    company = table.Column<string>(type: "varchar(50)", nullable: false),
                    credit_total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    debit_total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ending_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    fiscal_period = table.Column<int>(type: "integer", nullable: false),
                    fiscal_year = table.Column<int>(type: "integer", nullable: false),
                    is_closed = table.Column<bool>(type: "boolean", nullable: false),
                    period_end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    period_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    sub_account_id = table.Column<int>(type: "integer", nullable: false),
                    sub_account_name = table.Column<string>(type: "varchar(200)", nullable: false),
                    sub_account_type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gl_sub_account_balances", x => x.id);
                    table.ForeignKey(
                        name: "fk_gl_sub_account_balances_chart_of_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "chart_of_accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "posted_periods",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_posted = table.Column<bool>(type: "boolean", nullable: false),
                    module = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    month = table.Column<int>(type: "integer", nullable: false),
                    posted_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    posted_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_posted_periods", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_gl_period_balances_account_id",
                table: "gl_period_balances",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_gl_sub_account_balances_account_id",
                table: "gl_sub_account_balances",
                column: "account_id");
        }
    }
}
