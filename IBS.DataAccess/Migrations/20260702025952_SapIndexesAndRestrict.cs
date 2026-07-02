using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IBS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SapIndexesAndRestrict : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_msap_bill_adjustments_msap_billings_billing_id",
                table: "msap_bill_adjustments");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_bill_adjustments_msap_dispatch_tickets_dispatch_ticket",
                table: "msap_bill_adjustments");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_bill_dispatches_msap_billings_billing_id",
                table: "msap_bill_dispatches");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_bill_dispatches_msap_dispatch_tickets_dispatch_ticket_",
                table: "msap_bill_dispatches");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_billings_customers_custno_fk",
                table: "msap_billings");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_billings_msap_job_orders_job_order_id",
                table: "msap_billings");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_billings_msap_ports_portnum",
                table: "msap_billings");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_billings_msap_principals_principal_id",
                table: "msap_billings");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_billings_msap_terminals_terminal",
                table: "msap_billings");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_billings_msap_vessels_vesselnum",
                table: "msap_billings");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_collection_bills_customers_customer_id",
                table: "msap_collection_bills");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_collection_bills_msap_billings_billing_id",
                table: "msap_collection_bills");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_collection_bills_msap_collections_collection_id",
                table: "msap_collection_bills");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_collections_bank_accounts_bankacctco",
                table: "msap_collections");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_collections_customers_custno",
                table: "msap_collections");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_dispatch_tickets_customers_custno",
                table: "msap_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_dispatch_tickets_msap_billings_billnum",
                table: "msap_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_dispatch_tickets_msap_job_orders_job_order_id",
                table: "msap_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_dispatch_tickets_msap_ports_portnum",
                table: "msap_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_dispatch_tickets_msap_services_service_id",
                table: "msap_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_dispatch_tickets_msap_terminals_terminal",
                table: "msap_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_dispatch_tickets_msap_tug_masters_masterno",
                table: "msap_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_dispatch_tickets_msap_tugboats_tugnum",
                table: "msap_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_dispatch_tickets_msap_vessels_vesselnum",
                table: "msap_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_job_orders_customers_customer_id",
                table: "msap_job_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_job_orders_msap_ports_port_id",
                table: "msap_job_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_job_orders_msap_terminals_terminal_id",
                table: "msap_job_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_job_orders_msap_vessels_vessel_id",
                table: "msap_job_orders");

            migrationBuilder.AddColumn<int>(
                name: "job_order_id1",
                table: "msap_dispatch_tickets",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_msap_job_orders_job_order_number",
                table: "msap_job_orders",
                column: "job_order_number");

            migrationBuilder.CreateIndex(
                name: "ix_msap_job_orders_status",
                table: "msap_job_orders",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_msap_dispatch_tickets_job_order_id1",
                table: "msap_dispatch_tickets",
                column: "job_order_id1");

            migrationBuilder.CreateIndex(
                name: "ix_msap_dispatch_tickets_status",
                table: "msap_dispatch_tickets",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_msap_billings_status",
                table: "msap_billings",
                column: "status");

            migrationBuilder.AddForeignKey(
                name: "fk_msap_bill_adjustments_msap_billings_billing_id",
                table: "msap_bill_adjustments",
                column: "billing_id",
                principalTable: "msap_billings",
                principalColumn: "RECID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_bill_adjustments_msap_dispatch_tickets_dispatch_ticket",
                table: "msap_bill_adjustments",
                column: "dispatch_ticket_id",
                principalTable: "msap_dispatch_tickets",
                principalColumn: "RECID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_bill_dispatches_msap_billings_billing_id",
                table: "msap_bill_dispatches",
                column: "billing_id",
                principalTable: "msap_billings",
                principalColumn: "RECID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_bill_dispatches_msap_dispatch_tickets_dispatch_ticket_",
                table: "msap_bill_dispatches",
                column: "dispatch_ticket_id",
                principalTable: "msap_dispatch_tickets",
                principalColumn: "RECID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_billings_customers_custno_fk",
                table: "msap_billings",
                column: "CUSTNO_FK",
                principalTable: "customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_billings_msap_job_orders_job_order_id",
                table: "msap_billings",
                column: "job_order_id",
                principalTable: "msap_job_orders",
                principalColumn: "job_order_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_billings_msap_ports_portnum",
                table: "msap_billings",
                column: "PORTNUM",
                principalTable: "msap_ports",
                principalColumn: "port_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_billings_msap_principals_principal_id",
                table: "msap_billings",
                column: "principal_id",
                principalTable: "msap_principals",
                principalColumn: "principal_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_billings_msap_terminals_terminal",
                table: "msap_billings",
                column: "TERMINAL",
                principalTable: "msap_terminals",
                principalColumn: "terminal_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_billings_msap_vessels_vesselnum",
                table: "msap_billings",
                column: "VESSELNUM",
                principalTable: "msap_vessels",
                principalColumn: "vessel_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_collection_bills_customers_customer_id",
                table: "msap_collection_bills",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_collection_bills_msap_billings_billing_id",
                table: "msap_collection_bills",
                column: "billing_id",
                principalTable: "msap_billings",
                principalColumn: "RECID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_collection_bills_msap_collections_collection_id",
                table: "msap_collection_bills",
                column: "collection_id",
                principalTable: "msap_collections",
                principalColumn: "RECID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_collections_bank_accounts_bankacctco",
                table: "msap_collections",
                column: "BANKACCTCO",
                principalTable: "bank_accounts",
                principalColumn: "bank_account_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_collections_customers_custno",
                table: "msap_collections",
                column: "CUSTNO",
                principalTable: "customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_dispatch_tickets_customers_custno",
                table: "msap_dispatch_tickets",
                column: "CUSTNO",
                principalTable: "customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_dispatch_tickets_msap_billings_billnum",
                table: "msap_dispatch_tickets",
                column: "BILLNUM",
                principalTable: "msap_billings",
                principalColumn: "RECID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_dispatch_tickets_msap_job_orders_job_order_id",
                table: "msap_dispatch_tickets",
                column: "job_order_id",
                principalTable: "msap_job_orders",
                principalColumn: "job_order_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_dispatch_tickets_msap_job_orders_job_order_id1",
                table: "msap_dispatch_tickets",
                column: "job_order_id1",
                principalTable: "msap_job_orders",
                principalColumn: "job_order_id");

            migrationBuilder.AddForeignKey(
                name: "fk_msap_dispatch_tickets_msap_ports_portnum",
                table: "msap_dispatch_tickets",
                column: "PORTNUM",
                principalTable: "msap_ports",
                principalColumn: "port_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_dispatch_tickets_msap_services_service_id",
                table: "msap_dispatch_tickets",
                column: "service_id",
                principalTable: "msap_services",
                principalColumn: "service_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_dispatch_tickets_msap_terminals_terminal",
                table: "msap_dispatch_tickets",
                column: "TERMINAL",
                principalTable: "msap_terminals",
                principalColumn: "terminal_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_dispatch_tickets_msap_tug_masters_masterno",
                table: "msap_dispatch_tickets",
                column: "MASTERNO",
                principalTable: "msap_tug_masters",
                principalColumn: "tug_master_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_dispatch_tickets_msap_tugboats_tugnum",
                table: "msap_dispatch_tickets",
                column: "TUGNUM",
                principalTable: "msap_tugboats",
                principalColumn: "tugboat_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_dispatch_tickets_msap_vessels_vesselnum",
                table: "msap_dispatch_tickets",
                column: "VESSELNUM",
                principalTable: "msap_vessels",
                principalColumn: "vessel_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_job_orders_customers_customer_id",
                table: "msap_job_orders",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_job_orders_msap_ports_port_id",
                table: "msap_job_orders",
                column: "port_id",
                principalTable: "msap_ports",
                principalColumn: "port_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_job_orders_msap_terminals_terminal_id",
                table: "msap_job_orders",
                column: "terminal_id",
                principalTable: "msap_terminals",
                principalColumn: "terminal_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_job_orders_msap_vessels_vessel_id",
                table: "msap_job_orders",
                column: "vessel_id",
                principalTable: "msap_vessels",
                principalColumn: "vessel_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_msap_bill_adjustments_msap_billings_billing_id",
                table: "msap_bill_adjustments");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_bill_adjustments_msap_dispatch_tickets_dispatch_ticket",
                table: "msap_bill_adjustments");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_bill_dispatches_msap_billings_billing_id",
                table: "msap_bill_dispatches");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_bill_dispatches_msap_dispatch_tickets_dispatch_ticket_",
                table: "msap_bill_dispatches");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_billings_customers_custno_fk",
                table: "msap_billings");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_billings_msap_job_orders_job_order_id",
                table: "msap_billings");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_billings_msap_ports_portnum",
                table: "msap_billings");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_billings_msap_principals_principal_id",
                table: "msap_billings");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_billings_msap_terminals_terminal",
                table: "msap_billings");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_billings_msap_vessels_vesselnum",
                table: "msap_billings");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_collection_bills_customers_customer_id",
                table: "msap_collection_bills");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_collection_bills_msap_billings_billing_id",
                table: "msap_collection_bills");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_collection_bills_msap_collections_collection_id",
                table: "msap_collection_bills");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_collections_bank_accounts_bankacctco",
                table: "msap_collections");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_collections_customers_custno",
                table: "msap_collections");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_dispatch_tickets_customers_custno",
                table: "msap_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_dispatch_tickets_msap_billings_billnum",
                table: "msap_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_dispatch_tickets_msap_job_orders_job_order_id",
                table: "msap_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_dispatch_tickets_msap_job_orders_job_order_id1",
                table: "msap_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_dispatch_tickets_msap_ports_portnum",
                table: "msap_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_dispatch_tickets_msap_services_service_id",
                table: "msap_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_dispatch_tickets_msap_terminals_terminal",
                table: "msap_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_dispatch_tickets_msap_tug_masters_masterno",
                table: "msap_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_dispatch_tickets_msap_tugboats_tugnum",
                table: "msap_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_dispatch_tickets_msap_vessels_vesselnum",
                table: "msap_dispatch_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_job_orders_customers_customer_id",
                table: "msap_job_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_job_orders_msap_ports_port_id",
                table: "msap_job_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_job_orders_msap_terminals_terminal_id",
                table: "msap_job_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_msap_job_orders_msap_vessels_vessel_id",
                table: "msap_job_orders");

            migrationBuilder.DropIndex(
                name: "ix_msap_job_orders_job_order_number",
                table: "msap_job_orders");

            migrationBuilder.DropIndex(
                name: "ix_msap_job_orders_status",
                table: "msap_job_orders");

            migrationBuilder.DropIndex(
                name: "ix_msap_dispatch_tickets_job_order_id1",
                table: "msap_dispatch_tickets");

            migrationBuilder.DropIndex(
                name: "ix_msap_dispatch_tickets_status",
                table: "msap_dispatch_tickets");

            migrationBuilder.DropIndex(
                name: "ix_msap_billings_status",
                table: "msap_billings");

            migrationBuilder.DropColumn(
                name: "job_order_id1",
                table: "msap_dispatch_tickets");

            migrationBuilder.AddForeignKey(
                name: "fk_msap_bill_adjustments_msap_billings_billing_id",
                table: "msap_bill_adjustments",
                column: "billing_id",
                principalTable: "msap_billings",
                principalColumn: "RECID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_bill_adjustments_msap_dispatch_tickets_dispatch_ticket",
                table: "msap_bill_adjustments",
                column: "dispatch_ticket_id",
                principalTable: "msap_dispatch_tickets",
                principalColumn: "RECID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_bill_dispatches_msap_billings_billing_id",
                table: "msap_bill_dispatches",
                column: "billing_id",
                principalTable: "msap_billings",
                principalColumn: "RECID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_bill_dispatches_msap_dispatch_tickets_dispatch_ticket_",
                table: "msap_bill_dispatches",
                column: "dispatch_ticket_id",
                principalTable: "msap_dispatch_tickets",
                principalColumn: "RECID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_billings_customers_custno_fk",
                table: "msap_billings",
                column: "CUSTNO_FK",
                principalTable: "customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_billings_msap_job_orders_job_order_id",
                table: "msap_billings",
                column: "job_order_id",
                principalTable: "msap_job_orders",
                principalColumn: "job_order_id");

            migrationBuilder.AddForeignKey(
                name: "fk_msap_billings_msap_ports_portnum",
                table: "msap_billings",
                column: "PORTNUM",
                principalTable: "msap_ports",
                principalColumn: "port_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_billings_msap_principals_principal_id",
                table: "msap_billings",
                column: "principal_id",
                principalTable: "msap_principals",
                principalColumn: "principal_id");

            migrationBuilder.AddForeignKey(
                name: "fk_msap_billings_msap_terminals_terminal",
                table: "msap_billings",
                column: "TERMINAL",
                principalTable: "msap_terminals",
                principalColumn: "terminal_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_billings_msap_vessels_vesselnum",
                table: "msap_billings",
                column: "VESSELNUM",
                principalTable: "msap_vessels",
                principalColumn: "vessel_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_collection_bills_customers_customer_id",
                table: "msap_collection_bills",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_collection_bills_msap_billings_billing_id",
                table: "msap_collection_bills",
                column: "billing_id",
                principalTable: "msap_billings",
                principalColumn: "RECID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_collection_bills_msap_collections_collection_id",
                table: "msap_collection_bills",
                column: "collection_id",
                principalTable: "msap_collections",
                principalColumn: "RECID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_collections_bank_accounts_bankacctco",
                table: "msap_collections",
                column: "BANKACCTCO",
                principalTable: "bank_accounts",
                principalColumn: "bank_account_id");

            migrationBuilder.AddForeignKey(
                name: "fk_msap_collections_customers_custno",
                table: "msap_collections",
                column: "CUSTNO",
                principalTable: "customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_dispatch_tickets_customers_custno",
                table: "msap_dispatch_tickets",
                column: "CUSTNO",
                principalTable: "customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_dispatch_tickets_msap_billings_billnum",
                table: "msap_dispatch_tickets",
                column: "BILLNUM",
                principalTable: "msap_billings",
                principalColumn: "RECID");

            migrationBuilder.AddForeignKey(
                name: "fk_msap_dispatch_tickets_msap_job_orders_job_order_id",
                table: "msap_dispatch_tickets",
                column: "job_order_id",
                principalTable: "msap_job_orders",
                principalColumn: "job_order_id");

            migrationBuilder.AddForeignKey(
                name: "fk_msap_dispatch_tickets_msap_ports_portnum",
                table: "msap_dispatch_tickets",
                column: "PORTNUM",
                principalTable: "msap_ports",
                principalColumn: "port_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_dispatch_tickets_msap_services_service_id",
                table: "msap_dispatch_tickets",
                column: "service_id",
                principalTable: "msap_services",
                principalColumn: "service_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_dispatch_tickets_msap_terminals_terminal",
                table: "msap_dispatch_tickets",
                column: "TERMINAL",
                principalTable: "msap_terminals",
                principalColumn: "terminal_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_dispatch_tickets_msap_tug_masters_masterno",
                table: "msap_dispatch_tickets",
                column: "MASTERNO",
                principalTable: "msap_tug_masters",
                principalColumn: "tug_master_id");

            migrationBuilder.AddForeignKey(
                name: "fk_msap_dispatch_tickets_msap_tugboats_tugnum",
                table: "msap_dispatch_tickets",
                column: "TUGNUM",
                principalTable: "msap_tugboats",
                principalColumn: "tugboat_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_dispatch_tickets_msap_vessels_vesselnum",
                table: "msap_dispatch_tickets",
                column: "VESSELNUM",
                principalTable: "msap_vessels",
                principalColumn: "vessel_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_job_orders_customers_customer_id",
                table: "msap_job_orders",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_job_orders_msap_ports_port_id",
                table: "msap_job_orders",
                column: "port_id",
                principalTable: "msap_ports",
                principalColumn: "port_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_job_orders_msap_terminals_terminal_id",
                table: "msap_job_orders",
                column: "terminal_id",
                principalTable: "msap_terminals",
                principalColumn: "terminal_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_msap_job_orders_msap_vessels_vessel_id",
                table: "msap_job_orders",
                column: "vessel_id",
                principalTable: "msap_vessels",
                principalColumn: "vessel_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
