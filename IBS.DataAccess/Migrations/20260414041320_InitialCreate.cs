using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app_settings",
                columns: table => new
                {
                    setting_key = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_settings", x => x.setting_key);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    department = table.Column<string>(type: "text", nullable: false),
                    station_access = table.Column<string>(type: "text", nullable: true),
                    position = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp", nullable: false),
                    modified_date = table.Column<DateTime>(type: "timestamp", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_trails",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "text", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    machine_name = table.Column<string>(type: "text", nullable: false),
                    activity = table.Column<string>(type: "text", nullable: false),
                    document_type = table.Column<string>(type: "text", nullable: false),
                    company = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_trails", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bank_accounts",
                columns: table => new
                {
                    bank_account_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bank = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    branch = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    account_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    account_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bank_accounts", x => x.bank_account_id);
                });

            migrationBuilder.CreateTable(
                name: "cash_receipt_books",
                columns: table => new
                {
                    cash_receipt_book_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    ref_no = table.Column<string>(type: "text", nullable: false),
                    customer_name = table.Column<string>(type: "text", nullable: false),
                    bank = table.Column<string>(type: "text", nullable: true),
                    check_no = table.Column<string>(type: "text", nullable: true),
                    coa = table.Column<string>(type: "text", nullable: false),
                    particulars = table.Column<string>(type: "text", nullable: false),
                    debit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    credit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    company = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cash_receipt_books", x => x.cash_receipt_book_id);
                });

            migrationBuilder.CreateTable(
                name: "chart_of_accounts",
                columns: table => new
                {
                    account_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    is_main = table.Column<bool>(type: "boolean", nullable: false),
                    account_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    account_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    account_type = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    normal_balance = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    level = table.Column<int>(type: "integer", nullable: false),
                    parent_account_id = table.Column<int>(type: "integer", nullable: true),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    edited_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    has_children = table.Column<bool>(type: "boolean", nullable: false),
                    financial_statement_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chart_of_accounts", x => x.account_id);
                    table.ForeignKey(
                        name: "fk_chart_of_accounts_chart_of_accounts_parent_account_id",
                        column: x => x.parent_account_id,
                        principalTable: "chart_of_accounts",
                        principalColumn: "account_id");
                });

            migrationBuilder.CreateTable(
                name: "companies",
                columns: table => new
                {
                    company_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_code = table.Column<string>(type: "varchar(3)", nullable: true),
                    company_name = table.Column<string>(type: "varchar(50)", nullable: false),
                    company_address = table.Column<string>(type: "varchar(200)", nullable: false),
                    company_tin = table.Column<string>(type: "varchar(20)", nullable: false),
                    business_style = table.Column<string>(type: "varchar(20)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_companies", x => x.company_id);
                });

            migrationBuilder.CreateTable(
                name: "disbursement_books",
                columns: table => new
                {
                    disbursement_book_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    cv_no = table.Column<string>(type: "text", nullable: false),
                    payee = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    particulars = table.Column<string>(type: "text", nullable: false),
                    bank = table.Column<string>(type: "text", nullable: false),
                    check_no = table.Column<string>(type: "text", nullable: false),
                    check_date = table.Column<string>(type: "text", nullable: false),
                    chart_of_account = table.Column<string>(type: "text", nullable: false),
                    debit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    credit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    company = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_disbursement_books", x => x.disbursement_book_id);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    employee_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_number = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    initial = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    middle_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    suffix = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: true),
                    tel_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    sss_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    tin_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    philhealth_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    pagibig_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    department = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    date_hired = table.Column<DateOnly>(type: "date", nullable: false),
                    date_resigned = table.Column<DateOnly>(type: "date", nullable: true),
                    position = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_managerial = table.Column<bool>(type: "boolean", nullable: false),
                    supervisor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    paygrade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    salary = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employees", x => x.employee_id);
                });

            migrationBuilder.CreateTable(
                name: "hub_connections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    connection_id = table.Column<string>(type: "text", nullable: false),
                    user_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hub_connections", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "journal_books",
                columns: table => new
                {
                    journal_book_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    reference = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    account_title = table.Column<string>(type: "text", nullable: false),
                    debit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    credit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    company = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_journal_books", x => x.journal_book_id);
                });

            migrationBuilder.CreateTable(
                name: "log_messages",
                columns: table => new
                {
                    log_id = table.Column<Guid>(type: "uuid", nullable: false),
                    time_stamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    log_level = table.Column<string>(type: "text", nullable: false),
                    logger_name = table.Column<string>(type: "text", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_log_messages", x => x.log_id);
                });

            migrationBuilder.CreateTable(
                name: "mmsi_ports",
                columns: table => new
                {
                    port_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    port_number = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: true),
                    port_name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    has_sbma = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_ports", x => x.port_id);
                });

            migrationBuilder.CreateTable(
                name: "mmsi_services",
                columns: table => new
                {
                    service_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    service_number = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false),
                    service_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_services", x => x.service_id);
                });

            migrationBuilder.CreateTable(
                name: "mmsi_tug_masters",
                columns: table => new
                {
                    tug_master_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tug_master_number = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: false),
                    tug_master_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_tug_masters", x => x.tug_master_id);
                });

            migrationBuilder.CreateTable(
                name: "mmsi_tugboat_owners",
                columns: table => new
                {
                    tugboat_owner_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tugboat_owner_number = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false),
                    tugboat_owner_name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    fixed_rate = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_tugboat_owners", x => x.tugboat_owner_id);
                });

            migrationBuilder.CreateTable(
                name: "mmsi_user_accesses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    user_name = table.Column<string>(type: "varchar(100)", nullable: true),
                    can_create_service_request = table.Column<bool>(type: "boolean", nullable: false),
                    can_post_service_request = table.Column<bool>(type: "boolean", nullable: false),
                    can_create_dispatch_ticket = table.Column<bool>(type: "boolean", nullable: false),
                    can_edit_dispatch_ticket = table.Column<bool>(type: "boolean", nullable: false),
                    can_cancel_dispatch_ticket = table.Column<bool>(type: "boolean", nullable: false),
                    can_set_tariff = table.Column<bool>(type: "boolean", nullable: false),
                    can_approve_tariff = table.Column<bool>(type: "boolean", nullable: false),
                    can_create_billing = table.Column<bool>(type: "boolean", nullable: false),
                    can_create_collection = table.Column<bool>(type: "boolean", nullable: false),
                    can_create_job_order = table.Column<bool>(type: "boolean", nullable: false),
                    can_edit_job_order = table.Column<bool>(type: "boolean", nullable: false),
                    can_delete_job_order = table.Column<bool>(type: "boolean", nullable: false),
                    can_close_job_order = table.Column<bool>(type: "boolean", nullable: false),
                    can_access_treasury = table.Column<bool>(type: "boolean", nullable: false),
                    can_create_disbursement = table.Column<bool>(type: "boolean", nullable: false),
                    can_manage_msap_import = table.Column<bool>(type: "boolean", nullable: false),
                    can_view_general_ledger = table.Column<bool>(type: "boolean", nullable: false),
                    can_view_inventory_report = table.Column<bool>(type: "boolean", nullable: false),
                    can_view_maritime_report = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_user_accesses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mmsi_vessels",
                columns: table => new
                {
                    vessel_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    vessel_number = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: false),
                    vessel_name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    vessel_type = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_vessels", x => x.vessel_id);
                });

            migrationBuilder.CreateTable(
                name: "monthly_nibits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    month = table.Column<int>(type: "integer", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    beginning_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    net_income = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    prior_period_adjustment = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ending_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    company = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_monthly_nibits", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    notification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.notification_id);
                });

            migrationBuilder.CreateTable(
                name: "po_actual_prices",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    purchase_order_id = table.Column<int>(type: "integer", nullable: false),
                    triggered_volume = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    applied_volume = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    triggered_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_approved = table.Column<bool>(type: "boolean", nullable: false),
                    approved_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    approved_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    triggered_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_po_actual_prices", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "posted_periods",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    month = table.Column<int>(type: "integer", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    is_posted = table.Column<bool>(type: "boolean", nullable: false),
                    posted_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    posted_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    module = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_posted_periods", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    product_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_code = table.Column<string>(type: "varchar(10)", nullable: false),
                    product_name = table.Column<string>(type: "varchar(50)", nullable: false),
                    product_unit = table.Column<string>(type: "varchar(2)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.product_id);
                });

            migrationBuilder.CreateTable(
                name: "purchase_books",
                columns: table => new
                {
                    purchase_book_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    supplier_name = table.Column<string>(type: "text", nullable: false),
                    supplier_tin = table.Column<string>(type: "text", nullable: false),
                    supplier_address = table.Column<string>(type: "text", nullable: false),
                    document_no = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    discount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    vat_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    wht_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    net_purchases = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    po_no = table.Column<string>(type: "varchar(12)", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    company = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_books", x => x.purchase_book_id);
                });

            migrationBuilder.CreateTable(
                name: "purchase_locked_records_queues",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    locked_date = table.Column<DateOnly>(type: "date", nullable: false),
                    receiving_report_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_locked_records_queues", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sales_books",
                columns: table => new
                {
                    sales_book_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    serial_no = table.Column<string>(type: "text", nullable: false),
                    sold_to = table.Column<string>(type: "text", nullable: false),
                    tin_no = table.Column<string>(type: "text", nullable: false),
                    address = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    vat_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    vatable_sales = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    vat_exempt_sales = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    zero_rated = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    discount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    net_sales = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    document_id = table.Column<int>(type: "integer", nullable: true),
                    company = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_books", x => x.sales_book_id);
                });

            migrationBuilder.CreateTable(
                name: "services",
                columns: table => new
                {
                    service_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    service_no = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    current_and_previous_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    current_and_previous_title = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    unearned_title = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    unearned_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    percent = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_services", x => x.service_id);
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                columns: table => new
                {
                    supplier_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    supplier_code = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    supplier_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    supplier_address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    supplier_tin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    supplier_terms = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    vat_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    tax_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    proof_of_registration_file_path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    proof_of_registration_file_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    proof_of_exemption_file_path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    proof_of_exemption_file_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    edited_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    trade_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    branch = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    default_expense_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    withholding_tax_percent = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    withholding_tax_title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    reason_of_exemption = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    validity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    validity_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    zip_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    requires_price_adjustment = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suppliers", x => x.supplier_id);
                });

            migrationBuilder.CreateTable(
                name: "terms",
                columns: table => new
                {
                    terms_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    number_of_days = table.Column<int>(type: "integer", nullable: false),
                    number_of_months = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    edited_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_terms", x => x.terms_code);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<string>(type: "text", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_role_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_asp_net_role_claims_asp_net_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "AspNetRoles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_asp_net_user_claims_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    provider_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_asp_net_user_logins_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "text", nullable: false),
                    role_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_asp_net_user_roles_asp_net_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "AspNetRoles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_asp_net_user_roles_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "text", nullable: false),
                    login_provider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_tokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_asp_net_user_tokens_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "general_ledger_books",
                columns: table => new
                {
                    general_ledger_book_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    reference = table.Column<string>(type: "varchar(20)", nullable: false),
                    account_no = table.Column<string>(type: "varchar(50)", nullable: false),
                    account_title = table.Column<string>(type: "varchar(200)", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    debit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    credit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    is_posted = table.Column<bool>(type: "boolean", nullable: false),
                    company = table.Column<string>(type: "varchar(50)", nullable: false),
                    module_type = table.Column<string>(type: "varchar(50)", nullable: false),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    sub_account_type = table.Column<int>(type: "integer", nullable: true),
                    sub_account_id = table.Column<int>(type: "integer", nullable: true),
                    sub_account_name = table.Column<string>(type: "varchar(200)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_general_ledger_books", x => x.general_ledger_book_id);
                    table.ForeignKey(
                        name: "fk_general_ledger_books_chart_of_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "chart_of_accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "gl_period_balances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    period_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    fiscal_year = table.Column<int>(type: "integer", nullable: false),
                    fiscal_period = table.Column<int>(type: "integer", nullable: false),
                    beginning_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    debit_total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    credit_total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ending_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    adjustment_debit_total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    adjustment_credit_total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    adjusted_ending_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_closed = table.Column<bool>(type: "boolean", nullable: false),
                    closed_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    company = table.Column<string>(type: "varchar(50)", nullable: false)
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
                    sub_account_type = table.Column<int>(type: "integer", nullable: false),
                    sub_account_id = table.Column<int>(type: "integer", nullable: false),
                    sub_account_name = table.Column<string>(type: "varchar(200)", nullable: false),
                    period_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    fiscal_year = table.Column<int>(type: "integer", nullable: false),
                    fiscal_period = table.Column<int>(type: "integer", nullable: false),
                    beginning_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    debit_total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    credit_total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ending_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_closed = table.Column<bool>(type: "boolean", nullable: false),
                    company = table.Column<string>(type: "varchar(50)", nullable: false)
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
                name: "mmsi_terminals",
                columns: table => new
                {
                    terminal_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    terminal_number = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: true),
                    terminal_name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    port_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_terminals", x => x.terminal_id);
                    table.ForeignKey(
                        name: "fk_mmsi_terminals_mmsi_ports_port_id",
                        column: x => x.port_id,
                        principalTable: "mmsi_ports",
                        principalColumn: "port_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mmsi_tugboats",
                columns: table => new
                {
                    tugboat_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tugboat_number = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false),
                    tugboat_name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    is_company_owned = table.Column<bool>(type: "boolean", nullable: false),
                    tugboat_owner_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_tugboats", x => x.tugboat_id);
                    table.ForeignKey(
                        name: "fk_mmsi_tugboats_mmsi_tugboat_owners_tugboat_owner_id",
                        column: x => x.tugboat_owner_id,
                        principalTable: "mmsi_tugboat_owners",
                        principalColumn: "tugboat_owner_id");
                });

            migrationBuilder.CreateTable(
                name: "user_notifications",
                columns: table => new
                {
                    user_notification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    notification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    requires_response = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_notifications", x => x.user_notification_id);
                    table.ForeignKey(
                        name: "fk_user_notifications_application_user_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_notifications_notifications_notification_id",
                        column: x => x.notification_id,
                        principalTable: "notifications",
                        principalColumn: "notification_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventories",
                columns: table => new
                {
                    inventory_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    particular = table.Column<string>(type: "varchar(200)", nullable: false),
                    reference = table.Column<string>(type: "varchar(12)", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    inventory_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    average_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unit = table.Column<string>(type: "varchar(2)", nullable: false),
                    is_validated = table.Column<bool>(type: "boolean", nullable: false),
                    validated_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    validated_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    po_id = table.Column<int>(type: "integer", nullable: true),
                    company = table.Column<string>(type: "text", nullable: false)
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
                name: "customers",
                columns: table => new
                {
                    customer_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_code = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    customer_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    customer_address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    customer_tin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    business_style = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    customer_terms = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    customer_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    vat_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    with_holding_vat = table.Column<bool>(type: "boolean", nullable: false),
                    with_holding_tax = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    edited_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cluster_code = table.Column<int>(type: "integer", nullable: true),
                    station_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    credit_limit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    credit_limit_as_of_today = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    has_branch = table.Column<bool>(type: "boolean", nullable: false),
                    zip_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    retention_rate = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    has_multiple_terms = table.Column<bool>(type: "boolean", nullable: false),
                    type = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    requires_price_adjustment = table.Column<bool>(type: "boolean", nullable: false),
                    commissionee_id = table.Column<int>(type: "integer", nullable: true),
                    commission_rate = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customers", x => x.customer_id);
                    table.ForeignKey(
                        name: "fk_customers_suppliers_commissionee_id",
                        column: x => x.commissionee_id,
                        principalTable: "suppliers",
                        principalColumn: "supplier_id");
                });

            migrationBuilder.CreateTable(
                name: "pick_up_points",
                columns: table => new
                {
                    pick_up_point_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    depot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    supplier_id = table.Column<int>(type: "integer", nullable: false),
                    company = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
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
                name: "customer_branches",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    branch_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    branch_address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
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
                name: "mmsi_collections",
                columns: table => new
                {
                    mmsi_collection_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    mmsi_collection_number = table.Column<string>(type: "text", nullable: false),
                    reference_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "text", nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    remarks = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    cash_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    check_date = table.Column<DateOnly>(type: "date", nullable: true),
                    check_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    check_bank = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    check_branch = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    check_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    bank_id = table.Column<int>(type: "integer", nullable: true),
                    bank_account_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    bank_account_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ewt = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    wvat = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    is_undocumented = table.Column<bool>(type: "boolean", nullable: false),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    deposit_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    cleared_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    cancellation_remarks = table.Column<string>(type: "varchar(255)", nullable: true),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_collections", x => x.mmsi_collection_id);
                    table.ForeignKey(
                        name: "fk_mmsi_collections_bank_accounts_bank_id",
                        column: x => x.bank_id,
                        principalTable: "bank_accounts",
                        principalColumn: "bank_account_id");
                    table.ForeignKey(
                        name: "fk_mmsi_collections_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mmsi_job_orders",
                columns: table => new
                {
                    job_order_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    job_order_number = table.Column<string>(type: "varchar(20)", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "varchar(20)", nullable: false),
                    cos_number = table.Column<string>(type: "varchar(20)", nullable: true),
                    voyage_number = table.Column<string>(type: "varchar(100)", nullable: true),
                    remarks = table.Column<string>(type: "text", nullable: true),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    vessel_id = table.Column<int>(type: "integer", nullable: false),
                    port_id = table.Column<int>(type: "integer", nullable: true),
                    terminal_id = table.Column<int>(type: "integer", nullable: true),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    cancellation_remarks = table.Column<string>(type: "varchar(255)", nullable: true),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_job_orders", x => x.job_order_id);
                    table.ForeignKey(
                        name: "fk_mmsi_job_orders_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mmsi_job_orders_mmsi_ports_port_id",
                        column: x => x.port_id,
                        principalTable: "mmsi_ports",
                        principalColumn: "port_id");
                    table.ForeignKey(
                        name: "fk_mmsi_job_orders_mmsi_terminals_terminal_id",
                        column: x => x.terminal_id,
                        principalTable: "mmsi_terminals",
                        principalColumn: "terminal_id");
                    table.ForeignKey(
                        name: "fk_mmsi_job_orders_mmsi_vessels_vessel_id",
                        column: x => x.vessel_id,
                        principalTable: "mmsi_vessels",
                        principalColumn: "vessel_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mmsi_principals",
                columns: table => new
                {
                    principal_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    principal_number = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: false),
                    principal_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    address = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    business_type = table.Column<string>(type: "text", nullable: true),
                    terms = table.Column<string>(type: "text", nullable: true),
                    tin = table.Column<string>(type: "text", nullable: true),
                    landline1 = table.Column<string>(type: "text", nullable: true),
                    landline2 = table.Column<string>(type: "text", nullable: true),
                    mobile1 = table.Column<string>(type: "text", nullable: true),
                    mobile2 = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_vatable = table.Column<bool>(type: "boolean", nullable: false),
                    customer_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_principals", x => x.principal_id);
                    table.ForeignKey(
                        name: "fk_mmsi_principals_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mmsi_tariff_rates",
                columns: table => new
                {
                    tariff_rate_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    as_of_date = table.Column<DateOnly>(type: "date", nullable: false),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    terminal_id = table.Column<int>(type: "integer", nullable: false),
                    service_id = table.Column<int>(type: "integer", nullable: false),
                    dispatch = table.Column<decimal>(type: "numeric", nullable: false),
                    baf = table.Column<decimal>(type: "numeric", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    update_by = table.Column<string>(type: "text", nullable: true),
                    update_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    dispatch_discount = table.Column<decimal>(type: "numeric", nullable: false),
                    baf_discount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_tariff_rates", x => x.tariff_rate_id);
                    table.ForeignKey(
                        name: "fk_mmsi_tariff_rates_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mmsi_tariff_rates_mmsi_services_service_id",
                        column: x => x.service_id,
                        principalTable: "mmsi_services",
                        principalColumn: "service_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mmsi_tariff_rates_mmsi_terminals_terminal_id",
                        column: x => x.terminal_id,
                        principalTable: "mmsi_terminals",
                        principalColumn: "terminal_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "customer_order_slips",
                columns: table => new
                {
                    customer_order_slip_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_order_slip_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    customer_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    customer_address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    customer_tin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    customer_po_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    delivered_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    delivered_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    balance_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    has_commission = table.Column<bool>(type: "boolean", nullable: false),
                    commissionee_id = table.Column<int>(type: "integer", nullable: true),
                    commission_rate = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    account_specialist = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    branch = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    purchase_order_id = table.Column<int>(type: "integer", nullable: true),
                    delivery_option = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    freight = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    pick_up_point_id = table.Column<int>(type: "integer", nullable: true),
                    supplier_id = table.Column<int>(type: "integer", nullable: true),
                    sub_po_remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    om_approved_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    om_approved_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    expiration_date = table.Column<DateOnly>(type: "date", nullable: true),
                    om_reason = table.Column<string>(type: "text", nullable: true),
                    cnc_approved_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    cnc_approved_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    fm_approved_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    fm_approved_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    terms = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    finance_instruction = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    hauler_id = table.Column<int>(type: "integer", nullable: true),
                    driver = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    plate_no = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    authority_to_load_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_delivered = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    edited_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    disapproved_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    disapproved_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    old_cos_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    has_multiple_po = table.Column<bool>(type: "boolean", nullable: false),
                    uploaded_files = table.Column<string[]>(type: "varchar[]", nullable: true),
                    old_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    price_reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    customer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    product_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    available_credit_limit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    vat_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    has_ewt = table.Column<bool>(type: "boolean", nullable: false),
                    has_wvat = table.Column<bool>(type: "boolean", nullable: false),
                    depot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    commissionee_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    business_style = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    commissionee_vat_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    commissionee_tax_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_cos_atl_finalized = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "freights",
                columns: table => new
                {
                    freight_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pick_up_point_id = table.Column<int>(type: "integer", nullable: false),
                    cluster_code = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
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
                name: "billings",
                columns: table => new
                {
                    mmsi_billing_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    mmsi_billing_number = table.Column<string>(type: "varchar(10)", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    is_undocumented = table.Column<bool>(type: "boolean", nullable: false),
                    billed_to = table.Column<string>(type: "varchar(10)", nullable: false),
                    voyage_number = table.Column<string>(type: "text", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false),
                    dispatch_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    baf_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    is_principal = table.Column<bool>(type: "boolean", nullable: false),
                    customer_id = table.Column<int>(type: "integer", nullable: true),
                    principal_id = table.Column<int>(type: "integer", nullable: true),
                    vessel_id = table.Column<int>(type: "integer", nullable: true),
                    port_id = table.Column<int>(type: "integer", nullable: true),
                    terminal_id = table.Column<int>(type: "integer", nullable: true),
                    ap_other_tug = table.Column<decimal>(type: "numeric", nullable: false),
                    is_vatable = table.Column<bool>(type: "boolean", nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    discount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    terms = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    collection_id = table.Column<int>(type: "integer", nullable: true),
                    collection_number = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    cancellation_remarks = table.Column<string>(type: "varchar(255)", nullable: true),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_billings", x => x.mmsi_billing_id);
                    table.ForeignKey(
                        name: "fk_billings_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "customer_id");
                    table.ForeignKey(
                        name: "fk_billings_mmsi_collections_collection_id",
                        column: x => x.collection_id,
                        principalTable: "mmsi_collections",
                        principalColumn: "mmsi_collection_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_billings_mmsi_ports_port_id",
                        column: x => x.port_id,
                        principalTable: "mmsi_ports",
                        principalColumn: "port_id");
                    table.ForeignKey(
                        name: "fk_billings_mmsi_principals_principal_id",
                        column: x => x.principal_id,
                        principalTable: "mmsi_principals",
                        principalColumn: "principal_id");
                    table.ForeignKey(
                        name: "fk_billings_mmsi_terminals_terminal_id",
                        column: x => x.terminal_id,
                        principalTable: "mmsi_terminals",
                        principalColumn: "terminal_id");
                    table.ForeignKey(
                        name: "fk_billings_mmsi_vessels_vessel_id",
                        column: x => x.vessel_id,
                        principalTable: "mmsi_vessels",
                        principalColumn: "vessel_id");
                });

            migrationBuilder.CreateTable(
                name: "authority_to_loads",
                columns: table => new
                {
                    authority_to_load_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    authority_to_load_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    customer_order_slip_id = table.Column<int>(type: "integer", nullable: true),
                    date_booked = table.Column<DateOnly>(type: "date", nullable: false),
                    valid_until = table.Column<DateOnly>(type: "date", nullable: false),
                    uppi_atl_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    remarks = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    supplier_id = table.Column<int>(type: "integer", nullable: false),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    hauler_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    driver = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    plate_no = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    supplier_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    depot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    load_port_id = table.Column<int>(type: "integer", nullable: false),
                    freight = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
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
                    purchase_order_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unserved_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_assigned_to_dr = table.Column<bool>(type: "boolean", nullable: false),
                    supplier_id = table.Column<int>(type: "integer", nullable: false),
                    atl_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    unreserved_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
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
                name: "mmsi_dispatch_tickets",
                columns: table => new
                {
                    dispatch_ticket_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateOnly>(type: "date", nullable: true),
                    dispatch_number = table.Column<string>(type: "varchar(20)", nullable: false),
                    cos_number = table.Column<string>(type: "varchar(10)", nullable: true),
                    date_left = table.Column<DateOnly>(type: "date", nullable: true),
                    date_arrived = table.Column<DateOnly>(type: "date", nullable: true),
                    time_left = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    time_arrived = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    remarks = table.Column<string>(type: "varchar(100)", nullable: true),
                    base_or_station = table.Column<string>(type: "varchar(100)", nullable: true),
                    voyage_number = table.Column<string>(type: "varchar(100)", nullable: true),
                    dispatch_charge_type = table.Column<string>(type: "text", nullable: true),
                    baf_charge_type = table.Column<string>(type: "text", nullable: true),
                    total_hours = table.Column<decimal>(type: "numeric", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    edited_by = table.Column<string>(type: "text", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    dispatch_rate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    dispatch_billing_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    dispatch_discount = table.Column<decimal>(type: "numeric", nullable: false),
                    dispatch_net_revenue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    baf_rate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    baf_billing_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    baf_discount = table.Column<decimal>(type: "numeric", nullable: false),
                    baf_net_revenue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_billing = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_net_revenue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ap_other_tugs = table.Column<decimal>(type: "numeric", nullable: false),
                    tariff_by = table.Column<string>(type: "text", nullable: true),
                    tariff_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    tariff_edited_by = table.Column<string>(type: "text", nullable: true),
                    tariff_edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    image_name = table.Column<string>(type: "text", nullable: true),
                    image_saved_url = table.Column<string>(type: "text", nullable: true),
                    image_signed_url = table.Column<string>(type: "text", nullable: true),
                    video_name = table.Column<string>(type: "text", nullable: true),
                    video_saved_url = table.Column<string>(type: "text", nullable: true),
                    video_signed_url = table.Column<string>(type: "text", nullable: true),
                    job_order_id = table.Column<int>(type: "integer", nullable: true),
                    billing_id = table.Column<int>(type: "integer", nullable: true),
                    billing_number = table.Column<string>(type: "text", nullable: true),
                    customer_id = table.Column<int>(type: "integer", nullable: true),
                    tug_boat_id = table.Column<int>(type: "integer", nullable: true),
                    tug_master_id = table.Column<int>(type: "integer", nullable: true),
                    vessel_id = table.Column<int>(type: "integer", nullable: true),
                    terminal_id = table.Column<int>(type: "integer", nullable: true),
                    service_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mmsi_dispatch_tickets", x => x.dispatch_ticket_id);
                    table.ForeignKey(
                        name: "fk_mmsi_dispatch_tickets_billings_billing_id",
                        column: x => x.billing_id,
                        principalTable: "billings",
                        principalColumn: "mmsi_billing_id");
                    table.ForeignKey(
                        name: "fk_mmsi_dispatch_tickets_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "customer_id");
                    table.ForeignKey(
                        name: "fk_mmsi_dispatch_tickets_mmsi_job_orders_job_order_id",
                        column: x => x.job_order_id,
                        principalTable: "mmsi_job_orders",
                        principalColumn: "job_order_id");
                    table.ForeignKey(
                        name: "fk_mmsi_dispatch_tickets_mmsi_services_service_id",
                        column: x => x.service_id,
                        principalTable: "mmsi_services",
                        principalColumn: "service_id");
                    table.ForeignKey(
                        name: "fk_mmsi_dispatch_tickets_mmsi_terminals_terminal_id",
                        column: x => x.terminal_id,
                        principalTable: "mmsi_terminals",
                        principalColumn: "terminal_id");
                    table.ForeignKey(
                        name: "fk_mmsi_dispatch_tickets_mmsi_tug_masters_tug_master_id",
                        column: x => x.tug_master_id,
                        principalTable: "mmsi_tug_masters",
                        principalColumn: "tug_master_id");
                    table.ForeignKey(
                        name: "fk_mmsi_dispatch_tickets_mmsi_tugboats_tug_boat_id",
                        column: x => x.tug_boat_id,
                        principalTable: "mmsi_tugboats",
                        principalColumn: "tugboat_id");
                    table.ForeignKey(
                        name: "fk_mmsi_dispatch_tickets_mmsi_vessels_vessel_id",
                        column: x => x.vessel_id,
                        principalTable: "mmsi_vessels",
                        principalColumn: "vessel_id");
                });

            migrationBuilder.CreateTable(
                name: "delivery_receipts",
                columns: table => new
                {
                    delivery_receipt_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    delivery_receipt_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    delivered_date = table.Column<DateOnly>(type: "date", nullable: true),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    customer_address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    customer_tin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    customer_order_slip_id = table.Column<int>(type: "integer", nullable: false),
                    remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    manual_dr_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    hauler_id = table.Column<int>(type: "integer", nullable: true),
                    driver = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    plate_no = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    freight = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ecc = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    authority_to_load_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    has_already_invoiced = table.Column<bool>(type: "boolean", nullable: false),
                    purchase_order_id = table.Column<int>(type: "integer", nullable: true),
                    freight_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    commissionee_id = table.Column<int>(type: "integer", nullable: true),
                    commission_rate = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    commission_amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    freight_amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_commission_paid = table.Column<bool>(type: "boolean", nullable: false),
                    is_freight_paid = table.Column<bool>(type: "boolean", nullable: false),
                    commission_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    has_receiving_report = table.Column<bool>(type: "boolean", nullable: false),
                    hauler_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    hauler_vat_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    hauler_tax_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    authority_to_load_id = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "varchar(15)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    cancellation_remarks = table.Column<string>(type: "varchar(255)", nullable: true),
                    canceled_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    voided_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    posted_by = table.Column<string>(type: "varchar(50)", nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
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
                    authority_to_load_id = table.Column<int>(type: "integer", nullable: false),
                    customer_order_slip_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unserved_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    appointed_id = table.Column<int>(type: "integer", nullable: true)
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
                    locked_date = table.Column<DateOnly>(type: "date", nullable: false),
                    delivery_receipt_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
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
                name: "ix_app_settings_setting_key",
                table: "app_settings",
                column: "setting_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_role_claims_role_id",
                table: "AspNetRoleClaims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_claims_user_id",
                table: "AspNetUserClaims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_logins_user_id",
                table: "AspNetUserLogins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_roles_role_id",
                table: "AspNetUserRoles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "normalized_user_name",
                unique: true);

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
                name: "ix_billings_collection_id",
                table: "billings",
                column: "collection_id");

            migrationBuilder.CreateIndex(
                name: "ix_billings_customer_id",
                table: "billings",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_billings_date",
                table: "billings",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_billings_mmsi_billing_number_company",
                table: "billings",
                columns: new[] { "mmsi_billing_number", "company" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_billings_port_id",
                table: "billings",
                column: "port_id");

            migrationBuilder.CreateIndex(
                name: "ix_billings_principal_id",
                table: "billings",
                column: "principal_id");

            migrationBuilder.CreateIndex(
                name: "ix_billings_terminal_id",
                table: "billings",
                column: "terminal_id");

            migrationBuilder.CreateIndex(
                name: "ix_billings_vessel_id",
                table: "billings",
                column: "vessel_id");

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
                name: "ix_chart_of_accounts_account_name",
                table: "chart_of_accounts",
                column: "account_name");

            migrationBuilder.CreateIndex(
                name: "ix_chart_of_accounts_account_number",
                table: "chart_of_accounts",
                column: "account_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_chart_of_accounts_parent_account_id",
                table: "chart_of_accounts",
                column: "parent_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_companies_company_code",
                table: "companies",
                column: "company_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_companies_company_name",
                table: "companies",
                column: "company_name",
                unique: true);

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
                name: "ix_customers_commissionee_id",
                table: "customers",
                column: "commissionee_id");

            migrationBuilder.CreateIndex(
                name: "ix_customers_customer_code",
                table: "customers",
                column: "customer_code");

            migrationBuilder.CreateIndex(
                name: "ix_customers_customer_name",
                table: "customers",
                column: "customer_name");

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
                name: "ix_employees_employee_number",
                table: "employees",
                column: "employee_number");

            migrationBuilder.CreateIndex(
                name: "ix_freights_pick_up_point_id",
                table: "freights",
                column: "pick_up_point_id");

            migrationBuilder.CreateIndex(
                name: "ix_general_ledger_books_account_id",
                table: "general_ledger_books",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_gl_period_balances_account_id",
                table: "gl_period_balances",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_gl_sub_account_balances_account_id",
                table: "gl_sub_account_balances",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventories_product_id",
                table: "inventories",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_collections_bank_id",
                table: "mmsi_collections",
                column: "bank_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_collections_customer_id",
                table: "mmsi_collections",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_collections_date",
                table: "mmsi_collections",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_collections_mmsi_collection_number_company",
                table: "mmsi_collections",
                columns: new[] { "mmsi_collection_number", "company" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_dispatch_tickets_billing_id",
                table: "mmsi_dispatch_tickets",
                column: "billing_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_dispatch_tickets_customer_id",
                table: "mmsi_dispatch_tickets",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_dispatch_tickets_job_order_id",
                table: "mmsi_dispatch_tickets",
                column: "job_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_dispatch_tickets_service_id",
                table: "mmsi_dispatch_tickets",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_dispatch_tickets_terminal_id",
                table: "mmsi_dispatch_tickets",
                column: "terminal_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_dispatch_tickets_tug_boat_id",
                table: "mmsi_dispatch_tickets",
                column: "tug_boat_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_dispatch_tickets_tug_master_id",
                table: "mmsi_dispatch_tickets",
                column: "tug_master_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_dispatch_tickets_vessel_id",
                table: "mmsi_dispatch_tickets",
                column: "vessel_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_job_orders_customer_id",
                table: "mmsi_job_orders",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_job_orders_port_id",
                table: "mmsi_job_orders",
                column: "port_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_job_orders_terminal_id",
                table: "mmsi_job_orders",
                column: "terminal_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_job_orders_vessel_id",
                table: "mmsi_job_orders",
                column: "vessel_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_principals_customer_id",
                table: "mmsi_principals",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_tariff_rates_customer_id",
                table: "mmsi_tariff_rates",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_tariff_rates_service_id",
                table: "mmsi_tariff_rates",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_tariff_rates_terminal_id",
                table: "mmsi_tariff_rates",
                column: "terminal_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_terminals_port_id",
                table: "mmsi_terminals",
                column: "port_id");

            migrationBuilder.CreateIndex(
                name: "ix_mmsi_tugboats_tugboat_owner_id",
                table: "mmsi_tugboats",
                column: "tugboat_owner_id");

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

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_supplier_code",
                table: "suppliers",
                column: "supplier_code");

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_supplier_name",
                table: "suppliers",
                column: "supplier_name");

            migrationBuilder.CreateIndex(
                name: "ix_user_notifications_notification_id",
                table: "user_notifications",
                column: "notification_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_notifications_user_id",
                table: "user_notifications",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_settings");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "audit_trails");

            migrationBuilder.DropTable(
                name: "book_atl_details");

            migrationBuilder.DropTable(
                name: "cash_receipt_books");

            migrationBuilder.DropTable(
                name: "companies");

            migrationBuilder.DropTable(
                name: "customer_branches");

            migrationBuilder.DropTable(
                name: "disbursement_books");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "freights");

            migrationBuilder.DropTable(
                name: "general_ledger_books");

            migrationBuilder.DropTable(
                name: "gl_period_balances");

            migrationBuilder.DropTable(
                name: "gl_sub_account_balances");

            migrationBuilder.DropTable(
                name: "hub_connections");

            migrationBuilder.DropTable(
                name: "inventories");

            migrationBuilder.DropTable(
                name: "journal_books");

            migrationBuilder.DropTable(
                name: "log_messages");

            migrationBuilder.DropTable(
                name: "mmsi_dispatch_tickets");

            migrationBuilder.DropTable(
                name: "mmsi_tariff_rates");

            migrationBuilder.DropTable(
                name: "mmsi_user_accesses");

            migrationBuilder.DropTable(
                name: "monthly_nibits");

            migrationBuilder.DropTable(
                name: "po_actual_prices");

            migrationBuilder.DropTable(
                name: "posted_periods");

            migrationBuilder.DropTable(
                name: "purchase_books");

            migrationBuilder.DropTable(
                name: "purchase_locked_records_queues");

            migrationBuilder.DropTable(
                name: "sales_books");

            migrationBuilder.DropTable(
                name: "sales_locked_records_queues");

            migrationBuilder.DropTable(
                name: "services");

            migrationBuilder.DropTable(
                name: "terms");

            migrationBuilder.DropTable(
                name: "user_notifications");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "cos_appointed_suppliers");

            migrationBuilder.DropTable(
                name: "chart_of_accounts");

            migrationBuilder.DropTable(
                name: "billings");

            migrationBuilder.DropTable(
                name: "mmsi_job_orders");

            migrationBuilder.DropTable(
                name: "mmsi_tug_masters");

            migrationBuilder.DropTable(
                name: "mmsi_tugboats");

            migrationBuilder.DropTable(
                name: "mmsi_services");

            migrationBuilder.DropTable(
                name: "delivery_receipts");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "mmsi_collections");

            migrationBuilder.DropTable(
                name: "mmsi_principals");

            migrationBuilder.DropTable(
                name: "mmsi_terminals");

            migrationBuilder.DropTable(
                name: "mmsi_vessels");

            migrationBuilder.DropTable(
                name: "mmsi_tugboat_owners");

            migrationBuilder.DropTable(
                name: "authority_to_loads");

            migrationBuilder.DropTable(
                name: "bank_accounts");

            migrationBuilder.DropTable(
                name: "mmsi_ports");

            migrationBuilder.DropTable(
                name: "customer_order_slips");

            migrationBuilder.DropTable(
                name: "customers");

            migrationBuilder.DropTable(
                name: "pick_up_points");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "suppliers");
        }
    }
}
