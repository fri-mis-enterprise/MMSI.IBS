using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddIndicesToAuditTrail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_audit_trails_date",
                table: "audit_trails",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_audit_trails_document_type",
                table: "audit_trails",
                column: "document_type");

            migrationBuilder.CreateIndex(
                name: "ix_audit_trails_record_id",
                table: "audit_trails",
                column: "record_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_trails_reference_number",
                table: "audit_trails",
                column: "reference_number");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_audit_trails_date",
                table: "audit_trails");

            migrationBuilder.DropIndex(
                name: "ix_audit_trails_document_type",
                table: "audit_trails");

            migrationBuilder.DropIndex(
                name: "ix_audit_trails_record_id",
                table: "audit_trails");

            migrationBuilder.DropIndex(
                name: "ix_audit_trails_reference_number",
                table: "audit_trails");
        }
    }
}
