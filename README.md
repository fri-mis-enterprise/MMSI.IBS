# MSAP — MMSI Sales and Accounting Program

MSAP is a specialized, standalone Sales and Accounting system dedicated to the maritime service industry. Developed with ASP.NET Core 10.0, it provides a focused and efficient workflow for managing maritime service requests, from initial job ordering to final collection.

---

## 🏗️ Core MSAP Workflow

The system is architected around a streamlined, four-stage operational lifecycle:

### 1. Job Order Management
The entry point for all service requests.
- **Job Orders**: Centralized tracking of service requests including customer details, vessel info, and planned schedules.
- **Vessel Planning**: Real-time synchronization and scheduling of vessel activities.

### 2. Dispatch Ticket Operations
Field operations management and service fulfillment.
- **Dispatch Tickets**: Recording of actual service delivery, including tugboat assignments, service times, and tug master details.
- **Tugboat Monitoring**: Live tracking and status updates of the tugboat fleet via SignalR.

### 3. Billing & Invoicing
Financial processing of fulfilled services.
- **Billing**: Automated generation of service invoices based on dispatched tickets.
- **Tariff Rates**: Dynamic rate calculation based on customer-specific or port-specific tariff agreements.
- **Adjustments**: Flexible handling of bill adjustments and miscellaneous charges.

### 4. Collection & Treasury
Final settlement and financial tracking.
- **Collections**: Recording of payments (checks/cash) and linking them to outstanding billings.
- **Collection Reports**: Automated generation of official receipts and collection summaries.

---

## 🔎 Key Features

- **Specialized 4-Tier Architecture**: Lean design focusing exclusively on maritime operations and their financial counterparts.
- **Modern UI Design System**: A unified interface using custom `ModernTable`, `ModernSelect`, and `ModernAlert` components for maximum efficiency.
- **Live Operation Dashboards**: Real-time updates for Job Orders and Tugboat assignments powered by SignalR.
- **Comprehensive Audit Trail**: Automatic logging of every critical step in the MSAP lifecycle (Create, Edit, Disapprove, Bill, Collect).
- **Advanced Export Features**: Direct data export capabilities for operational and financial auditing.

---

## 🛠️ Tech Stack

- **Backend**: ASP.NET Core 10.0 (C#)
- **Data Access**: Entity Framework Core with Npgsql (PostgreSQL)
- **Architecture**: N-Tier / Layered (DataAccess, Models, DTOs, Services, Web UI)
- **Frontend**: Razor Pages / MVC, JavaScript (ES6+), jQuery, DataTables.net
- **Messaging**: SignalR for real-time operational hubs
- **Reports**: QuestPDF for high-fidelity maritime documents and invoices
- **Hosting/Deployment**: Dockerized environment optimized for Google Cloud Platform (GCP)

---

For full version history, see the [CHANGELOG](CHANGELOG.md).
