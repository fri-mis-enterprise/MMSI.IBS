using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFilprideRelatedModulesOnFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "book_atl_details");

            migrationBuilder.DropTable(
                name: "customer_branches");

            migrationBuilder.DropTable(
                name: "freights");

            migrationBuilder.DropTable(
                name: "inventories");

            migrationBuilder.DropTable(
                name: "monthly_nibits");

            migrationBuilder.DropTable(
                name: "po_actual_prices");

            migrationBuilder.DropTable(
                name: "purchase_locked_records_queues");

            migrationBuilder.DropTable(
                name: "sales_locked_records_queues");

            migrationBuilder.DropTable(
                name: "services");

            migrationBuilder.DropTable(
                name: "cos_appointed_suppliers");

            migrationBuilder.DropTable(
                name: "delivery_receipts");

            migrationBuilder.DropTable(
                name: "authority_to_loads");

            migrationBuilder.DropTable(
                name: "customer_order_slips");

            migrationBuilder.DropTable(
                name: "pick_up_points");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropColumn(
                name: "has_branch",
                table: "customers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "has_branch",
                table: "customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "customer_branches",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    branch_address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    branch_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    branch_tin = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_branches", x => x.id);
                    table.ForeignKey(
                        name: "fk_customer_branches_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "monthly_nibits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    beginning_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    company = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ending_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    month = table.Column<int>(type: "integer", nullable: false),
                    net_income = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    prior_period_adjustment = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_monthly_nibits", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pick_up_points",
                columns: table => new
                {
                    pick_up_point_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    supplier_id = table.Column<int>(type: "integer", nullable: false),
                    company = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    depot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pick_up_points", x => x.pick_up_point_id);
                    table.ForeignKey(
                        name: "fk_pick_up_points_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "supplier_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "po_actual_prices",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    applied_volume = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    approved_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    approved_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    is_approved = table.Column<bool>(type: "boolean", nullable: false),
                    purchase_order_id = table.Column<int>(type: "integer", nullable: false),
                    triggered_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    triggered_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    triggered_volume = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_po_actual_prices", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    product_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    product_code = table.Column<string>(type: "varchar(10)", nullable: false),
                    product_name = table.Column<string>(type: "varchar(50)", nullable: false),
                    product_unit = table.Column<string>(type: "varchar(2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.product_id);
                });

            migrationBuilder.CreateTable(
                name: "purchase_locked_records_queues",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    locked_date = table.Column<DateOnly>(type: "date", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    receiving_report_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_locked_records_queues", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "services",
                columns: table => new
                {
                    service_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    current_and_previous_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    current_and_previous_title = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    percent = table.Column<int>(type: "integer", nullable: false),
                    service_no = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    unearned_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    unearned_title = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_services", x => x.service_id);
                });

            migrationBuilder.CreateTable(
                name: "freights",
                columns: table => new
                {
                    freight_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pick_up_point_id = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    cluster_code = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_freights", x => x.freight_id);
                    table.ForeignKey(
                        name: "fk_freights_pick_up_points_pick_up_point_id",
                        column: x => x.pick_up_point_id,
                        principalTable: "pick_up_points",
                        principalColumn: "pick_up_point_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "customer_order_slips",
                columns: table => new
                {
                    customer_order_slip_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    commissionee_id = table.Column<int>(type: "integer", nullable: true),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    hauler_id = table.Column<int>(type: "integer", nullable: true),
                    pick_up_point_id = table.Column<int>(type: "integer", nullable: true),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    supplier_id = table.Column<int>(type: "integer", nullable: true),
                    account_specialist = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    authority_to_load_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    available_credit_limit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    balance_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    branch = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    business_style = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    cnc_approved_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    cnc_approved_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    commission_rate = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    commissionee_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    commissionee_tax_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    commissionee_vat_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    customer_address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    customer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    customer_order_slip_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    customer_po_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    customer_tin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    customer_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    delivered_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    delivered_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    delivery_option = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    depot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    disapproved_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    disapproved_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    driver = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    edited_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    expiration_date = table.Column<DateOnly>(type: "date", nullable: true),
                    finance_instruction = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    fm_approved_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    fm_approved_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    freight = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    has_commission = table.Column<bool>(type: "boolean", nullable: false),
                    has_ewt = table.Column<bool>(type: "boolean", nullable: false),
                    has_multiple_po = table.Column<bool>(type: "boolean", nullable: false),
                    has_wvat = table.Column<bool>(type: "boolean", nullable: false),
                    is_cos_atl_finalized = table.Column<bool>(type: "boolean", nullable: false),
                    is_delivered = table.Column<bool>(type: "boolean", nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    om_reason = table.Column<string>(type: "text", nullable: true),
                    old_cos_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    old_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    om_approved_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    om_approved_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    plate_no = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    price_reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    product_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    purchase_order_id = table.Column<int>(type: "integer", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sub_po_remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    terms = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    total_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    uploaded_files = table.Column<string[]>(type: "varchar[]", nullable: true),
                    vat_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_order_slips", x => x.customer_order_slip_id);
                    table.ForeignKey(
                        name: "fk_customer_order_slips_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_customer_order_slips_pick_up_points_pick_up_point_id",
                        column: x => x.pick_up_point_id,
                        principalTable: "pick_up_points",
                        principalColumn: "pick_up_point_id");
                    table.ForeignKey(
                        name: "fk_customer_order_slips_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_customer_order_slips_suppliers_commissionee_id",
                        column: x => x.commissionee_id,
                        principalTable: "suppliers",
                        principalColumn: "supplier_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_customer_order_slips_suppliers_hauler_id",
                        column: x => x.hauler_id,
                        principalTable: "suppliers",
                        principalColumn: "supplier_id");
                    table.ForeignKey(
                        name: "fk_customer_order_slips_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "supplier_id");
                });

            migrationBuilder.CreateTable(
                name: "inventories",
                columns: table => new
                {
                    inventory_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    average_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    company = table.Column<string>(type: "text", nullable: false),
                    cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    inventory_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_validated = table.Column<bool>(type: "boolean", nullable: false),
                    po_id = table.Column<int>(type: "integer", nullable: true),
                    particular = table.Column<string>(type: "varchar(200)", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    reference = table.Column<string>(type: "varchar(12)", nullable: true),
                    total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unit = table.Column<string>(type: "varchar(2)", nullable: false),
                    validated_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    validated_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventories", x => x.inventory_id);
                    table.ForeignKey(
                        name: "fk_inventories_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "authority_to_loads",
                columns: table => new
                {
                    authority_to_load_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_order_slip_id = table.Column<int>(type: "integer", nullable: true),
                    supplier_id = table.Column<int>(type: "integer", nullable: false),
                    authority_to_load_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    date_booked = table.Column<DateOnly>(type: "date", nullable: false),
                    depot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    driver = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    freight = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    hauler_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    load_port_id = table.Column<int>(type: "integer", nullable: false),
                    plate_no = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    remarks = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    supplier_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    uppi_atl_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    valid_until = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_authority_to_loads", x => x.authority_to_load_id);
                    table.ForeignKey(
                        name: "fk_authority_to_loads_customer_order_slips_customer_order_slip",
                        column: x => x.customer_order_slip_id,
                        principalTable: "customer_order_slips",
                        principalColumn: "customer_order_slip_id");
                    table.ForeignKey(
                        name: "fk_authority_to_loads_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "supplier_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cos_appointed_suppliers",
                columns: table => new
                {
                    sequence_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_order_slip_id = table.Column<int>(type: "integer", nullable: false),
                    supplier_id = table.Column<int>(type: "integer", nullable: false),
                    atl_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_assigned_to_dr = table.Column<bool>(type: "boolean", nullable: false),
                    purchase_order_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unreserved_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unserved_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cos_appointed_suppliers", x => x.sequence_id);
                    table.ForeignKey(
                        name: "fk_cos_appointed_suppliers_customer_order_slips_customer_order",
                        column: x => x.customer_order_slip_id,
                        principalTable: "customer_order_slips",
                        principalColumn: "customer_order_slip_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cos_appointed_suppliers_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "supplier_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "delivery_receipts",
                columns: table => new
                {
                    delivery_receipt_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    authority_to_load_id = table.Column<int>(type: "integer", nullable: false),
                    commissionee_id = table.Column<int>(type: "integer", nullable: true),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    customer_order_slip_id = table.Column<int>(type: "integer", nullable: false),
                    hauler_id = table.Column<int>(type: "integer", nullable: true),
                    authority_to_load_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    cancellation_remarks = table.Column<string>(type: "varchar(255)", nullable: true),
                    commission_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    commission_amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    commission_rate = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    customer_address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    customer_tin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    delivered_date = table.Column<DateOnly>(type: "date", nullable: true),
                    delivery_receipt_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    driver = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ecc = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    freight = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    freight_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    freight_amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    has_already_invoiced = table.Column<bool>(type: "boolean", nullable: false),
                    has_receiving_report = table.Column<bool>(type: "boolean", nullable: false),
                    hauler_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    hauler_tax_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    hauler_vat_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_commission_paid = table.Column<bool>(type: "boolean", nullable: false),
                    is_freight_paid = table.Column<bool>(type: "boolean", nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    manual_dr_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    plate_no = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    purchase_order_id = table.Column<int>(type: "integer", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    type = table.Column<string>(type: "varchar(15)", nullable: false),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delivery_receipts", x => x.delivery_receipt_id);
                    table.ForeignKey(
                        name: "fk_delivery_receipts_authority_to_loads_authority_to_load_id",
                        column: x => x.authority_to_load_id,
                        principalTable: "authority_to_loads",
                        principalColumn: "authority_to_load_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_delivery_receipts_customer_order_slips_customer_order_slip_",
                        column: x => x.customer_order_slip_id,
                        principalTable: "customer_order_slips",
                        principalColumn: "customer_order_slip_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_delivery_receipts_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_delivery_receipts_suppliers_commissionee_id",
                        column: x => x.commissionee_id,
                        principalTable: "suppliers",
                        principalColumn: "supplier_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_delivery_receipts_suppliers_hauler_id",
                        column: x => x.hauler_id,
                        principalTable: "suppliers",
                        principalColumn: "supplier_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "book_atl_details",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    appointed_id = table.Column<int>(type: "integer", nullable: true),
                    authority_to_load_id = table.Column<int>(type: "integer", nullable: false),
                    customer_order_slip_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unserved_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_book_atl_details", x => x.id);
                    table.ForeignKey(
                        name: "fk_book_atl_details_authority_to_loads_authority_to_load_id",
                        column: x => x.authority_to_load_id,
                        principalTable: "authority_to_loads",
                        principalColumn: "authority_to_load_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_book_atl_details_cos_appointed_suppliers_appointed_id",
                        column: x => x.appointed_id,
                        principalTable: "cos_appointed_suppliers",
                        principalColumn: "sequence_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_book_atl_details_customer_order_slips_customer_order_slip_id",
                        column: x => x.customer_order_slip_id,
                        principalTable: "customer_order_slips",
                        principalColumn: "customer_order_slip_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_locked_records_queues",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    delivery_receipt_id = table.Column<int>(type: "integer", nullable: false),
                    locked_date = table.Column<DateOnly>(type: "date", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_locked_records_queues", x => x.id);
                    table.ForeignKey(
                        name: "fk_sales_locked_records_queues_delivery_receipts_delivery_rece",
                        column: x => x.delivery_receipt_id,
                        principalTable: "delivery_receipts",
                        principalColumn: "delivery_receipt_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_authority_to_loads_authority_to_load_no_company",
                table: "authority_to_loads",
                columns: new[] { "authority_to_load_no", "company" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_authority_to_loads_customer_order_slip_id",
                table: "authority_to_loads",
                column: "customer_order_slip_id");

            migrationBuilder.CreateIndex(
                name: "ix_authority_to_loads_supplier_id",
                table: "authority_to_loads",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_book_atl_details_appointed_id",
                table: "book_atl_details",
                column: "appointed_id");

            migrationBuilder.CreateIndex(
                name: "ix_book_atl_details_authority_to_load_id",
                table: "book_atl_details",
                column: "authority_to_load_id");

            migrationBuilder.CreateIndex(
                name: "ix_book_atl_details_customer_order_slip_id",
                table: "book_atl_details",
                column: "customer_order_slip_id");

            migrationBuilder.CreateIndex(
                name: "ix_cos_appointed_suppliers_customer_order_slip_id",
                table: "cos_appointed_suppliers",
                column: "customer_order_slip_id");

            migrationBuilder.CreateIndex(
                name: "ix_cos_appointed_suppliers_supplier_id",
                table: "cos_appointed_suppliers",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_branches_customer_id",
                table: "customer_branches",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_order_slips_commissionee_id",
                table: "customer_order_slips",
                column: "commissionee_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_order_slips_customer_id",
                table: "customer_order_slips",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_order_slips_customer_order_slip_no_company",
                table: "customer_order_slips",
                columns: new[] { "customer_order_slip_no", "company" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_customer_order_slips_date",
                table: "customer_order_slips",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_customer_order_slips_hauler_id",
                table: "customer_order_slips",
                column: "hauler_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_order_slips_pick_up_point_id",
                table: "customer_order_slips",
                column: "pick_up_point_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_order_slips_product_id",
                table: "customer_order_slips",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_order_slips_supplier_id",
                table: "customer_order_slips",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_receipts_authority_to_load_id",
                table: "delivery_receipts",
                column: "authority_to_load_id");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_receipts_commissionee_id",
                table: "delivery_receipts",
                column: "commissionee_id");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_receipts_customer_id",
                table: "delivery_receipts",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_receipts_customer_order_slip_id",
                table: "delivery_receipts",
                column: "customer_order_slip_id");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_receipts_date",
                table: "delivery_receipts",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_receipts_delivery_receipt_no_company",
                table: "delivery_receipts",
                columns: new[] { "delivery_receipt_no", "company" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_delivery_receipts_hauler_id",
                table: "delivery_receipts",
                column: "hauler_id");

            migrationBuilder.CreateIndex(
                name: "ix_freights_pick_up_point_id",
                table: "freights",
                column: "pick_up_point_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventories_product_id",
                table: "inventories",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_monthly_nibits_company",
                table: "monthly_nibits",
                column: "company");

            migrationBuilder.CreateIndex(
                name: "ix_monthly_nibits_month",
                table: "monthly_nibits",
                column: "month");

            migrationBuilder.CreateIndex(
                name: "ix_monthly_nibits_year",
                table: "monthly_nibits",
                column: "year");

            migrationBuilder.CreateIndex(
                name: "ix_pick_up_points_company",
                table: "pick_up_points",
                column: "company");

            migrationBuilder.CreateIndex(
                name: "ix_pick_up_points_supplier_id",
                table: "pick_up_points",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_po_actual_prices_purchase_order_id_triggered_date",
                table: "po_actual_prices",
                columns: new[] { "purchase_order_id", "triggered_date" });

            migrationBuilder.CreateIndex(
                name: "ix_products_product_code",
                table: "products",
                column: "product_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_products_product_name",
                table: "products",
                column: "product_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_locked_records_queues_locked_date",
                table: "purchase_locked_records_queues",
                column: "locked_date");

            migrationBuilder.CreateIndex(
                name: "ix_sales_locked_records_queues_delivery_receipt_id",
                table: "sales_locked_records_queues",
                column: "delivery_receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_locked_records_queues_locked_date",
                table: "sales_locked_records_queues",
                column: "locked_date");
        }
    }
}
