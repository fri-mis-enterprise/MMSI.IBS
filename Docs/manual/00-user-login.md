# User Login & Navigation

How to access MSAP and navigate the interface.

## Login

![Login page](/docs-images/user-login/login-page.png)

1. Open the app in your browser
2. Enter your **Username** and **Password**
3. Click **Login**

### First-Time Login

- Use the credentials provided by your administrator
- You may need to change your password on first login
- Sessions expire after **30 minutes of inactivity** — you'll be redirected to login automatically

## Dashboard

![Dashboard overview](/docs-images/user-login/dashboard.png)

After login, you land on the **Dashboard** showing:

- **Tasks Overview** — quick counts of pending work items:
  - Service Requests for Posting
  - Dispatch Tickets for Tariff / Approval / Disapproved
  - Billings for Collection
- Quick links to each module

## Navigation

![Top navigation bar](/docs-images/user-login/navigation.png)

The top navigation bar has these sections:

| Nav Item | Description |
|----------|-------------|
| **Home** | Dashboard |
| **MSAP** | Core workflow: Job Orders, Dispatch Tickets, Billing, Collection, Import, Reports |
| **Service Requests** | Create and manage service requests |
| **Master File** | (Admin only) Users, Roles, Employees, User Access, Chart of Accounts, Payment Terms, Bank Accounts |
| **MSAP References** | Master data: Activities, Ports, Principals, Tariff Rates, Terminals, Tugboats, Vessels, Customers, Suppliers |
| **Notifications** | Bell icon top-right — shows unread alerts |
| **Manual** | This user manual |
| **User menu** | Top-right — shows your name, logout option |

### MSAP Dropdown

The main workflow modules:

```
MSAP
├── Job Orders       → Plan and track service operations
├── Dispatch Tickets → Record service delivery
├── Billing          → Generate invoices
├── Collection       → Record payments
├── MSAP Import      → (restricted) CSV data import
└── MMSI Reports     → (restricted) Operational reports
```

### Navigation Tips

- **Dropdown menus** expand on click
- Items with a padlock icon require special permissions — if you don't see them, you don't have access
- Use the browser **back** button to return to previous screens
- Most list screens show the breadcrumb path at the top (e.g., `MSAP > Job Orders`)

## Logout

Click your username top-right, then click **Logout**.

## Common Interface Patterns

| Element | Description |
|---------|-------------|
| **DataTables** | All list screens use searchable, sortable, paginated tables |
| **Modern Cards** | Forms and details are wrapped in cards with headers and icons |
| **Select2** | Dropdowns with search (type to filter options) |
| **SweetAlert2** | Confirmation dialogs for deletes and state changes |
| **Toastr** | Success/error messages appear as toast notifications top-right |
| **Filter Buttons** | Status filter buttons above tables (e.g., ALL, FOR TARIFF, FOR BILLING) |

## Tips

- Bookmark the app URL for quick access
- Use the **Manual** link in the nav bar anytime you need help
- Report issues to your system administrator
