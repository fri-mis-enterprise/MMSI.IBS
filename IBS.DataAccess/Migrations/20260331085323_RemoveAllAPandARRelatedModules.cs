using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAllAPandARRelatedModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_cos_appointed_suppliers_purchase_orders_purchase_order_id",
                table: "cos_appointed_suppliers");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_order_slips_purchase_orders_purchase_order_id",
                table: "customer_order_slips");

            migrationBuilder.DropForeignKey(
                name: "fk_delivery_receipts_purchase_orders_purchase_order_id",
                table: "delivery_receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_inventories_purchase_orders_po_id",
                table: "inventories");

            migrationBuilder.DropForeignKey(
                name: "fk_po_actual_prices_purchase_orders_purchase_order_id",
                table: "po_actual_prices");

            migrationBuilder.DropForeignKey(
                name: "fk_purchase_locked_records_queues_receiving_reports_receiving_",
                table: "purchase_locked_records_queues");

            migrationBuilder.DropTable(
                name: "check_voucher_details");

            migrationBuilder.DropTable(
                name: "collection_receipt_details");

            migrationBuilder.DropTable(
                name: "credit_memos");

            migrationBuilder.DropTable(
                name: "cv_trade_payments");

            migrationBuilder.DropTable(
                name: "debit_memos");

            migrationBuilder.DropTable(
                name: "journal_voucher_details");

            migrationBuilder.DropTable(
                name: "jv_amortization_settings");

            migrationBuilder.DropTable(
                name: "multiple_check_voucher_payments");

            migrationBuilder.DropTable(
                name: "offsettings");

            migrationBuilder.DropTable(
                name: "receiving_reports");

            migrationBuilder.DropTable(
                name: "collection_receipts");

            migrationBuilder.DropTable(
                name: "journal_voucher_headers");

            migrationBuilder.DropTable(
                name: "sales_invoices");

            migrationBuilder.DropTable(
                name: "service_invoices");

            migrationBuilder.DropTable(
                name: "check_voucher_headers");

            migrationBuilder.DropTable(
                name: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "ix_purchase_locked_records_queues_receiving_report_id",
                table: "purchase_locked_records_queues");

            migrationBuilder.DropIndex(
                name: "ix_po_actual_prices_purchase_order_id",
                table: "po_actual_prices");

            migrationBuilder.DropIndex(
                name: "ix_inventories_po_id",
                table: "inventories");

            migrationBuilder.DropIndex(
                name: "ix_delivery_receipts_purchase_order_id",
                table: "delivery_receipts");

            migrationBuilder.DropIndex(
                name: "ix_customer_order_slips_purchase_order_id",
                table: "customer_order_slips");

            migrationBuilder.DropIndex(
                name: "ix_cos_appointed_suppliers_purchase_order_id",
                table: "cos_appointed_suppliers");

            migrationBuilder.DropColumn(
                name: "can_access_payable",
                table: "mmsi_user_accesses");

            migrationBuilder.DropColumn(
                name: "can_access_receivable",
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
                name: "can_view_accounts_payable_report",
                table: "mmsi_user_accesses");

            migrationBuilder.DropColumn(
                name: "can_view_accounts_receivable_report",
                table: "mmsi_user_accesses");

            migrationBuilder.CreateIndex(
                name: "ix_po_actual_prices_purchase_order_id_triggered_date",
                table: "po_actual_prices",
                columns: new[] { "purchase_order_id", "triggered_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_po_actual_prices_purchase_order_id_triggered_date",
                table: "po_actual_prices");

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

            migrationBuilder.CreateTable(
                name: "check_voucher_headers",
                columns: table => new
                {
                    check_voucher_header_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bank_id = table.Column<int>(type: "integer", nullable: true),
                    employee_id = table.Column<int>(type: "integer", nullable: true),
                    supplier_id = table.Column<int>(type: "integer", nullable: true),
                    address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    amount = table.Column<decimal[]>(type: "numeric(18,4)[]", nullable: true),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    approved_by = table.Column<string>(type: "text", nullable: true),
                    approved_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    bank_account_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    bank_account_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    cancellation_remarks = table.Column<string>(type: "varchar(255)", nullable: true),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    check_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    check_date = table.Column<DateOnly>(type: "date", nullable: true),
                    check_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    check_voucher_header_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    cv_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    dcp_date = table.Column<DateOnly>(type: "date", nullable: true),
                    dcr_date = table.Column<DateOnly>(type: "date", nullable: true),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    invoice_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_advances = table.Column<bool>(type: "boolean", nullable: false),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false),
                    is_payroll = table.Column<bool>(type: "boolean", nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    liquidation_date = table.Column<DateOnly>(type: "date", nullable: true),
                    old_cv_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    po_no = table.Column<string[]>(type: "varchar[]", nullable: true),
                    particulars = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    payee = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    rr_no = table.Column<string[]>(type: "varchar[]", nullable: true),
                    reference = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    si_no = table.Column<string[]>(type: "varchar[]", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    supplier_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    supporting_file_saved_file_name = table.Column<string>(type: "text", nullable: true),
                    supporting_file_saved_url = table.Column<string>(type: "text", nullable: true),
                    tax_percent = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    tax_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    type = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    vat_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_check_voucher_headers", x => x.check_voucher_header_id);
                    table.ForeignKey(
                        name: "fk_check_voucher_headers_bank_accounts_bank_id",
                        column: x => x.bank_id,
                        principalTable: "bank_accounts",
                        principalColumn: "bank_account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_check_voucher_headers_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id");
                    table.ForeignKey(
                        name: "fk_check_voucher_headers_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "supplier_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "offsettings",
                columns: table => new
                {
                    off_setting_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_no = table.Column<string>(type: "text", nullable: false),
                    account_title = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    company = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    is_removed = table.Column<bool>(type: "boolean", nullable: false),
                    reference = table.Column<string>(type: "text", nullable: true),
                    source = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_offsettings", x => x.off_setting_id);
                });

            migrationBuilder.CreateTable(
                name: "purchase_orders",
                columns: table => new
                {
                    purchase_order_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_id = table.Column<int>(type: "integer", nullable: true),
                    pick_up_point_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    supplier_id = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    cancellation_remarks = table.Column<string>(type: "varchar(255)", nullable: true),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    final_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_closed = table.Column<bool>(type: "boolean", nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    is_received = table.Column<bool>(type: "boolean", nullable: false),
                    is_sub_po = table.Column<bool>(type: "boolean", nullable: false),
                    old_po_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    product_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    purchase_order_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    quantity_received = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    received_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sub_po_series = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    supplier_address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    supplier_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    supplier_sales_order_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    supplier_tin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tax_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    terms = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    trigger_date = table.Column<DateOnly>(type: "date", nullable: false),
                    type = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    type_of_purchase = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    un_triggered_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    vat_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_orders", x => x.purchase_order_id);
                    table.ForeignKey(
                        name: "fk_purchase_orders_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_orders_pick_up_points_pick_up_point_id",
                        column: x => x.pick_up_point_id,
                        principalTable: "pick_up_points",
                        principalColumn: "pick_up_point_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_orders_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_orders_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "supplier_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_invoices",
                columns: table => new
                {
                    service_invoice_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    delivery_receipt_id = table.Column<int>(type: "integer", nullable: true),
                    service_id = table.Column<int>(type: "integer", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    cancellation_remarks = table.Column<string>(type: "varchar(255)", nullable: true),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    current_and_previous_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    customer_address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    customer_business_style = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    customer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    customer_tin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    discount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    has_ewt = table.Column<bool>(type: "boolean", nullable: false),
                    has_wvat = table.Column<bool>(type: "boolean", nullable: false),
                    instructions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    payment_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    period = table.Column<DateOnly>(type: "date", nullable: false),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    service_invoice_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    service_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    service_percent = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    type = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    unearned_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    vat_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_invoices", x => x.service_invoice_id);
                    table.ForeignKey(
                        name: "fk_service_invoices_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_invoices_delivery_receipts_delivery_receipt_id",
                        column: x => x.delivery_receipt_id,
                        principalTable: "delivery_receipts",
                        principalColumn: "delivery_receipt_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_invoices_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "service_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "check_voucher_details",
                columns: table => new
                {
                    check_voucher_detail_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    check_voucher_header_id = table.Column<int>(type: "integer", nullable: false),
                    account_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    account_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    credit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    debit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ewt_percent = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_display_entry = table.Column<bool>(type: "boolean", nullable: false),
                    is_user_selected = table.Column<bool>(type: "boolean", nullable: false),
                    is_vatable = table.Column<bool>(type: "boolean", nullable: false),
                    sub_account_id = table.Column<int>(type: "integer", nullable: true),
                    sub_account_name = table.Column<string>(type: "varchar(200)", nullable: true),
                    sub_account_type = table.Column<int>(type: "integer", nullable: true),
                    transaction_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_check_voucher_details", x => x.check_voucher_detail_id);
                    table.ForeignKey(
                        name: "fk_check_voucher_details_check_voucher_headers_check_voucher_h",
                        column: x => x.check_voucher_header_id,
                        principalTable: "check_voucher_headers",
                        principalColumn: "check_voucher_header_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cv_trade_payments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    check_voucher_id = table.Column<int>(type: "integer", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    document_id = table.Column<int>(type: "integer", nullable: false),
                    document_type = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cv_trade_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_cv_trade_payments_check_voucher_headers_check_voucher_id",
                        column: x => x.check_voucher_id,
                        principalTable: "check_voucher_headers",
                        principalColumn: "check_voucher_header_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "journal_voucher_headers",
                columns: table => new
                {
                    journal_voucher_header_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cv_id = table.Column<int>(type: "integer", nullable: true),
                    cr_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    cancellation_remarks = table.Column<string>(type: "varchar(255)", nullable: true),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    jv_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    journal_voucher_header_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    jv_type = table.Column<string>(type: "text", nullable: false),
                    particulars = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    references = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_journal_voucher_headers", x => x.journal_voucher_header_id);
                    table.ForeignKey(
                        name: "fk_journal_voucher_headers_check_voucher_headers_cv_id",
                        column: x => x.cv_id,
                        principalTable: "check_voucher_headers",
                        principalColumn: "check_voucher_header_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "multiple_check_voucher_payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    check_voucher_header_invoice_id = table.Column<int>(type: "integer", nullable: false),
                    check_voucher_header_payment_id = table.Column<int>(type: "integer", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_multiple_check_voucher_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_multiple_check_voucher_payments_check_voucher_headers_check",
                        column: x => x.check_voucher_header_invoice_id,
                        principalTable: "check_voucher_headers",
                        principalColumn: "check_voucher_header_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_multiple_check_voucher_payments_check_voucher_headers_check1",
                        column: x => x.check_voucher_header_payment_id,
                        principalTable: "check_voucher_headers",
                        principalColumn: "check_voucher_header_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "receiving_reports",
                columns: table => new
                {
                    receiving_report_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    delivery_receipt_id = table.Column<int>(type: "integer", nullable: true),
                    po_id = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    authority_to_load_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    canceled_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    cancellation_remarks = table.Column<string>(type: "varchar(255)", nullable: true),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cost_based_on_soa = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    gain_or_loss = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_cost_updated = table.Column<bool>(type: "boolean", nullable: false),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    old_rr_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    po_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    paid_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    quantity_delivered = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    quantity_received = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    received_date = table.Column<DateOnly>(type: "date", nullable: true),
                    receiving_report_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    supplier_dr_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    supplier_invoice_date = table.Column<DateOnly>(type: "date", nullable: true),
                    supplier_invoice_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    tax_percentage = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    truck_or_vessels = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    type = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    withdrawal_certificate = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_receiving_reports", x => x.receiving_report_id);
                    table.ForeignKey(
                        name: "fk_receiving_reports_delivery_receipts_delivery_receipt_id",
                        column: x => x.delivery_receipt_id,
                        principalTable: "delivery_receipts",
                        principalColumn: "delivery_receipt_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_receiving_reports_purchase_orders_po_id",
                        column: x => x.po_id,
                        principalTable: "purchase_orders",
                        principalColumn: "purchase_order_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_invoices",
                columns: table => new
                {
                    sales_invoice_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    customer_order_slip_id = table.Column<int>(type: "integer", nullable: true),
                    delivery_receipt_id = table.Column<int>(type: "integer", nullable: true),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    purchase_order_id = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    cancellation_remarks = table.Column<string>(type: "varchar(255)", nullable: true),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    customer_address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    customer_tin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    discount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    is_tax_and_vat_paid = table.Column<bool>(type: "boolean", nullable: false),
                    other_ref_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payment_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    receiving_report_id = table.Column<int>(type: "integer", nullable: false),
                    remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    sales_invoice_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    terms = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    type = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_invoices", x => x.sales_invoice_id);
                    table.ForeignKey(
                        name: "fk_sales_invoices_customer_order_slips_customer_order_slip_id",
                        column: x => x.customer_order_slip_id,
                        principalTable: "customer_order_slips",
                        principalColumn: "customer_order_slip_id");
                    table.ForeignKey(
                        name: "fk_sales_invoices_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sales_invoices_delivery_receipts_delivery_receipt_id",
                        column: x => x.delivery_receipt_id,
                        principalTable: "delivery_receipts",
                        principalColumn: "delivery_receipt_id");
                    table.ForeignKey(
                        name: "fk_sales_invoices_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sales_invoices_purchase_orders_purchase_order_id",
                        column: x => x.purchase_order_id,
                        principalTable: "purchase_orders",
                        principalColumn: "purchase_order_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "journal_voucher_details",
                columns: table => new
                {
                    journal_voucher_detail_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    journal_voucher_header_id = table.Column<int>(type: "integer", nullable: false),
                    account_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    account_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    credit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    debit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    sub_account_id = table.Column<int>(type: "integer", nullable: true),
                    sub_account_name = table.Column<string>(type: "varchar(200)", nullable: true),
                    sub_account_type = table.Column<int>(type: "integer", nullable: true),
                    transaction_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_journal_voucher_details", x => x.journal_voucher_detail_id);
                    table.ForeignKey(
                        name: "fk_journal_voucher_details_journal_voucher_headers_journal_vou",
                        column: x => x.journal_voucher_header_id,
                        principalTable: "journal_voucher_headers",
                        principalColumn: "journal_voucher_header_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "jv_amortization_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    jv_id = table.Column<int>(type: "integer", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    expense_account = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    jv_frequency = table.Column<int>(type: "integer", nullable: false),
                    last_run_date = table.Column<DateOnly>(type: "date", nullable: true),
                    next_run_date = table.Column<DateOnly>(type: "date", nullable: true),
                    occurrence_remaining = table.Column<int>(type: "integer", nullable: false),
                    occurrence_total = table.Column<int>(type: "integer", nullable: false),
                    prepaid_account = table.Column<string>(type: "text", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_jv_amortization_settings", x => x.id);
                    table.ForeignKey(
                        name: "fk_jv_amortization_settings_journal_voucher_headers_jv_id",
                        column: x => x.jv_id,
                        principalTable: "journal_voucher_headers",
                        principalColumn: "journal_voucher_header_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "collection_receipts",
                columns: table => new
                {
                    collection_receipt_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bank_id = table.Column<int>(type: "integer", nullable: true),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    sales_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    service_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    bank_account_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    bank_account_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    batch_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    cancellation_remarks = table.Column<string>(type: "varchar(255)", nullable: true),
                    cash_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    check_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    check_bank = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    check_branch = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    check_date = table.Column<DateOnly>(type: "date", nullable: true),
                    check_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    cleared_date = table.Column<DateOnly>(type: "date", nullable: true),
                    collection_receipt_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    deposited_date = table.Column<DateOnly>(type: "date", nullable: true),
                    ewt = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    f2306file_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    f2306file_path = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    f2307file_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    f2307file_path = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_certificate_upload = table.Column<bool>(type: "boolean", nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    managers_check_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    managers_check_bank = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    managers_check_branch = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    managers_check_date = table.Column<DateOnly>(type: "date", nullable: true),
                    managers_check_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    multiple_si = table.Column<string[]>(type: "text[]", nullable: true),
                    multiple_si_id = table.Column<int[]>(type: "integer[]", nullable: true),
                    multiple_transaction_date = table.Column<DateOnly[]>(type: "date[]", nullable: true),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    reference_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    remarks = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    si_multiple_amount = table.Column<decimal[]>(type: "numeric[]", nullable: true),
                    si_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    sv_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    type = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    wvat = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_collection_receipts", x => x.collection_receipt_id);
                    table.ForeignKey(
                        name: "fk_collection_receipts_bank_accounts_bank_id",
                        column: x => x.bank_id,
                        principalTable: "bank_accounts",
                        principalColumn: "bank_account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_collection_receipts_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_collection_receipts_sales_invoices_sales_invoice_id",
                        column: x => x.sales_invoice_id,
                        principalTable: "sales_invoices",
                        principalColumn: "sales_invoice_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_collection_receipts_service_invoices_service_invoice_id",
                        column: x => x.service_invoice_id,
                        principalTable: "service_invoices",
                        principalColumn: "service_invoice_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "credit_memos",
                columns: table => new
                {
                    credit_memo_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sales_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    service_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    adjusted_price = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    cancellation_remarks = table.Column<string>(type: "varchar(255)", nullable: true),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    credit_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    credit_memo_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    current_and_previous_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    period = table.Column<DateOnly>(type: "date", nullable: false),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    type = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    unearned_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_credit_memos", x => x.credit_memo_id);
                    table.ForeignKey(
                        name: "fk_credit_memos_sales_invoices_sales_invoice_id",
                        column: x => x.sales_invoice_id,
                        principalTable: "sales_invoices",
                        principalColumn: "sales_invoice_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_credit_memos_service_invoices_service_invoice_id",
                        column: x => x.service_invoice_id,
                        principalTable: "service_invoices",
                        principalColumn: "service_invoice_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "debit_memos",
                columns: table => new
                {
                    debit_memo_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sales_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    service_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    adjusted_price = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    cancellation_remarks = table.Column<string>(type: "varchar(255)", nullable: true),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    current_and_previous_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    debit_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    debit_memo_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    period = table.Column<DateOnly>(type: "date", nullable: false),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    type = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    unearned_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_debit_memos", x => x.debit_memo_id);
                    table.ForeignKey(
                        name: "fk_debit_memos_sales_invoices_sales_invoice_id",
                        column: x => x.sales_invoice_id,
                        principalTable: "sales_invoices",
                        principalColumn: "sales_invoice_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_debit_memos_service_invoices_service_invoice_id",
                        column: x => x.service_invoice_id,
                        principalTable: "service_invoices",
                        principalColumn: "service_invoice_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "collection_receipt_details",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    collection_receipt_id = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    collection_receipt_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    invoice_date = table.Column<DateOnly>(type: "date", nullable: false),
                    invoice_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_collection_receipt_details", x => x.id);
                    table.ForeignKey(
                        name: "fk_collection_receipt_details_collection_receipts_collection_r",
                        column: x => x.collection_receipt_id,
                        principalTable: "collection_receipts",
                        principalColumn: "collection_receipt_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_locked_records_queues_receiving_report_id",
                table: "purchase_locked_records_queues",
                column: "receiving_report_id");

            migrationBuilder.CreateIndex(
                name: "ix_po_actual_prices_purchase_order_id",
                table: "po_actual_prices",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventories_po_id",
                table: "inventories",
                column: "po_id");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_receipts_purchase_order_id",
                table: "delivery_receipts",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_order_slips_purchase_order_id",
                table: "customer_order_slips",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_cos_appointed_suppliers_purchase_order_id",
                table: "cos_appointed_suppliers",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_check_voucher_details_check_voucher_header_id",
                table: "check_voucher_details",
                column: "check_voucher_header_id");

            migrationBuilder.CreateIndex(
                name: "ix_check_voucher_headers_bank_id",
                table: "check_voucher_headers",
                column: "bank_id");

            migrationBuilder.CreateIndex(
                name: "ix_check_voucher_headers_check_voucher_header_no_company",
                table: "check_voucher_headers",
                columns: new[] { "check_voucher_header_no", "company" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_check_voucher_headers_employee_id",
                table: "check_voucher_headers",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_check_voucher_headers_supplier_id",
                table: "check_voucher_headers",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_collection_receipt_details_collection_receipt_id",
                table: "collection_receipt_details",
                column: "collection_receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_collection_receipt_details_collection_receipt_no",
                table: "collection_receipt_details",
                column: "collection_receipt_no");

            migrationBuilder.CreateIndex(
                name: "ix_collection_receipt_details_invoice_no",
                table: "collection_receipt_details",
                column: "invoice_no");

            migrationBuilder.CreateIndex(
                name: "ix_collection_receipts_bank_id",
                table: "collection_receipts",
                column: "bank_id");

            migrationBuilder.CreateIndex(
                name: "ix_collection_receipts_collection_receipt_no_company",
                table: "collection_receipts",
                columns: new[] { "collection_receipt_no", "company" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_collection_receipts_customer_id",
                table: "collection_receipts",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_collection_receipts_sales_invoice_id",
                table: "collection_receipts",
                column: "sales_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_collection_receipts_service_invoice_id",
                table: "collection_receipts",
                column: "service_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_credit_memos_credit_memo_no_company",
                table: "credit_memos",
                columns: new[] { "credit_memo_no", "company" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_credit_memos_sales_invoice_id",
                table: "credit_memos",
                column: "sales_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_credit_memos_service_invoice_id",
                table: "credit_memos",
                column: "service_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_cv_trade_payments_check_voucher_id",
                table: "cv_trade_payments",
                column: "check_voucher_id");

            migrationBuilder.CreateIndex(
                name: "ix_debit_memos_debit_memo_no_company",
                table: "debit_memos",
                columns: new[] { "debit_memo_no", "company" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_debit_memos_sales_invoice_id",
                table: "debit_memos",
                column: "sales_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_debit_memos_service_invoice_id",
                table: "debit_memos",
                column: "service_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_journal_voucher_details_journal_voucher_header_id",
                table: "journal_voucher_details",
                column: "journal_voucher_header_id");

            migrationBuilder.CreateIndex(
                name: "ix_journal_voucher_headers_cv_id",
                table: "journal_voucher_headers",
                column: "cv_id");

            migrationBuilder.CreateIndex(
                name: "ix_journal_voucher_headers_journal_voucher_header_no_company",
                table: "journal_voucher_headers",
                columns: new[] { "journal_voucher_header_no", "company" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_jv_amortization_settings_jv_id",
                table: "jv_amortization_settings",
                column: "jv_id");

            migrationBuilder.CreateIndex(
                name: "ix_multiple_check_voucher_payments_check_voucher_header_invoic",
                table: "multiple_check_voucher_payments",
                column: "check_voucher_header_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_multiple_check_voucher_payments_check_voucher_header_paymen",
                table: "multiple_check_voucher_payments",
                column: "check_voucher_header_payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_customer_id",
                table: "purchase_orders",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_pick_up_point_id",
                table: "purchase_orders",
                column: "pick_up_point_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_product_id",
                table: "purchase_orders",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_purchase_order_no_company",
                table: "purchase_orders",
                columns: new[] { "purchase_order_no", "company" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_supplier_id",
                table: "purchase_orders",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_receiving_reports_delivery_receipt_id",
                table: "receiving_reports",
                column: "delivery_receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_receiving_reports_po_id",
                table: "receiving_reports",
                column: "po_id");

            migrationBuilder.CreateIndex(
                name: "ix_receiving_reports_receiving_report_no_company",
                table: "receiving_reports",
                columns: new[] { "receiving_report_no", "company" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sales_invoices_customer_id",
                table: "sales_invoices",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_invoices_customer_order_slip_id",
                table: "sales_invoices",
                column: "customer_order_slip_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_invoices_delivery_receipt_id",
                table: "sales_invoices",
                column: "delivery_receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_invoices_product_id",
                table: "sales_invoices",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_invoices_purchase_order_id",
                table: "sales_invoices",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_invoices_sales_invoice_no_company",
                table: "sales_invoices",
                columns: new[] { "sales_invoice_no", "company" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_invoices_customer_id",
                table: "service_invoices",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_invoices_delivery_receipt_id",
                table: "service_invoices",
                column: "delivery_receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_invoices_service_id",
                table: "service_invoices",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_invoices_service_invoice_no_company",
                table: "service_invoices",
                columns: new[] { "service_invoice_no", "company" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_cos_appointed_suppliers_purchase_orders_purchase_order_id",
                table: "cos_appointed_suppliers",
                column: "purchase_order_id",
                principalTable: "purchase_orders",
                principalColumn: "purchase_order_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_order_slips_purchase_orders_purchase_order_id",
                table: "customer_order_slips",
                column: "purchase_order_id",
                principalTable: "purchase_orders",
                principalColumn: "purchase_order_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_delivery_receipts_purchase_orders_purchase_order_id",
                table: "delivery_receipts",
                column: "purchase_order_id",
                principalTable: "purchase_orders",
                principalColumn: "purchase_order_id");

            migrationBuilder.AddForeignKey(
                name: "fk_inventories_purchase_orders_po_id",
                table: "inventories",
                column: "po_id",
                principalTable: "purchase_orders",
                principalColumn: "purchase_order_id");

            migrationBuilder.AddForeignKey(
                name: "fk_po_actual_prices_purchase_orders_purchase_order_id",
                table: "po_actual_prices",
                column: "purchase_order_id",
                principalTable: "purchase_orders",
                principalColumn: "purchase_order_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_purchase_locked_records_queues_receiving_reports_receiving_",
                table: "purchase_locked_records_queues",
                column: "receiving_report_id",
                principalTable: "receiving_reports",
                principalColumn: "receiving_report_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
