using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingYearColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_msap_billings_number_company",
                table: "msap_billings");

            migrationBuilder.AddColumn<int>(
                name: "billing_year",
                table: "msap_billings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE msap_billings SET billing_year = EXTRACT(YEAR FROM \"DATE\")");

            migrationBuilder.CreateIndex(
                name: "ix_msap_billings_billing_year_number_company",
                table: "msap_billings",
                columns: new[] { "billing_year", "NUMBER", "company" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_msap_billings_billing_year_number_company",
                table: "msap_billings");

            migrationBuilder.DropColumn(
                name: "billing_year",
                table: "msap_billings");

            migrationBuilder.CreateIndex(
                name: "ix_msap_billings_number_company",
                table: "msap_billings",
                columns: new[] { "NUMBER", "company" },
                unique: true);
        }
    }
}
