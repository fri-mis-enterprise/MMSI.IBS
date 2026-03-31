using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddMMSIUserAccessPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "can_print_report",
                table: "mmsi_user_accesses",
                newName: "can_view_maritime_report");

            migrationBuilder.AddColumn<bool>(
                name: "can_access_payable",
                table: "mmsi_user_accesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_access_receivable",
                table: "mmsi_user_accesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_access_treasury",
                table: "mmsi_user_accesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_create_authority_to_load",
                table: "mmsi_user_accesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_create_check_voucher_non_trade_invoice",
                table: "mmsi_user_accesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_create_check_voucher_non_trade_payment",
                table: "mmsi_user_accesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_create_check_voucher_trade",
                table: "mmsi_user_accesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_create_collection_receipt",
                table: "mmsi_user_accesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_create_credit_memo",
                table: "mmsi_user_accesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_create_customer_order_slip",
                table: "mmsi_user_accesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_create_debit_memo",
                table: "mmsi_user_accesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_create_delivery_receipt",
                table: "mmsi_user_accesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_create_disbursement",
                table: "mmsi_user_accesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_create_journal_voucher",
                table: "mmsi_user_accesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_create_purchase_order",
                table: "mmsi_user_accesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_create_receiving_report",
                table: "mmsi_user_accesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_create_sales_invoice",
                table: "mmsi_user_accesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_create_service_invoice",
                table: "mmsi_user_accesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_manage_msap_import",
                table: "mmsi_user_accesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_view_accounts_payable_report",
                table: "mmsi_user_accesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_view_accounts_receivable_report",
                table: "mmsi_user_accesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_view_general_ledger",
                table: "mmsi_user_accesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_view_inventory_report",
                table: "mmsi_user_accesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "can_access_payable",
                table: "mmsi_user_accesses");

            migrationBuilder.DropColumn(
                name: "can_access_receivable",
                table: "mmsi_user_accesses");

            migrationBuilder.DropColumn(
                name: "can_access_treasury",
                table: "mmsi_user_accesses");

            migrationBuilder.DropColumn(
                name: "can_create_authority_to_load",
                table: "mmsi_user_accesses");

            migrationBuilder.DropColumn(
                name: "can_create_check_voucher_non_trade_invoice",
                table: "mmsi_user_accesses");

            migrationBuilder.DropColumn(
                name: "can_create_check_voucher_non_trade_payment",
                table: "mmsi_user_accesses");

            migrationBuilder.DropColumn(
                name: "can_create_check_voucher_trade",
                table: "mmsi_user_accesses");

            migrationBuilder.DropColumn(
                name: "can_create_collection_receipt",
                table: "mmsi_user_accesses");

            migrationBuilder.DropColumn(
                name: "can_create_credit_memo",
                table: "mmsi_user_accesses");

            migrationBuilder.DropColumn(
                name: "can_create_customer_order_slip",
                table: "mmsi_user_accesses");

            migrationBuilder.DropColumn(
                name: "can_create_debit_memo",
                table: "mmsi_user_accesses");

            migrationBuilder.DropColumn(
                name: "can_create_delivery_receipt",
                table: "mmsi_user_accesses");

            migrationBuilder.DropColumn(
                name: "can_create_disbursement",
                table: "mmsi_user_accesses");

            migrationBuilder.DropColumn(
                name: "can_create_journal_voucher",
                table: "mmsi_user_accesses");

            migrationBuilder.DropColumn(
                name: "can_create_purchase_order",
                table: "mmsi_user_accesses");

            migrationBuilder.DropColumn(
                name: "can_create_receiving_report",
                table: "mmsi_user_accesses");

            migrationBuilder.DropColumn(
                name: "can_create_sales_invoice",
                table: "mmsi_user_accesses");

            migrationBuilder.DropColumn(
                name: "can_create_service_invoice",
                table: "mmsi_user_accesses");

            migrationBuilder.DropColumn(
                name: "can_manage_msap_import",
                table: "mmsi_user_accesses");

            migrationBuilder.DropColumn(
                name: "can_view_accounts_payable_report",
                table: "mmsi_user_accesses");

            migrationBuilder.DropColumn(
                name: "can_view_accounts_receivable_report",
                table: "mmsi_user_accesses");

            migrationBuilder.DropColumn(
                name: "can_view_general_ledger",
                table: "mmsi_user_accesses");

            migrationBuilder.DropColumn(
                name: "can_view_inventory_report",
                table: "mmsi_user_accesses");

            migrationBuilder.RenameColumn(
                name: "can_view_maritime_report",
                table: "mmsi_user_accesses",
                newName: "can_print_report");
        }
    }
}
