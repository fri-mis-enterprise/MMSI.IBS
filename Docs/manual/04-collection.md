# 4. Collection & Payment

**Collections** records payments received against outstanding billings. Supports check and cash payments with full accounting integration.

## Workflow State

Billing moves to **Collected / Paid** when fully collected.

## Pages

### Collections List (Index)

- **Path:** MSAP > Collections
- **Filters:** Date range
- **Table columns:** Date, Collection #, Check Date, Date Deposited, Amount, Customer, Actions

### Create Collection

- **Form layout:** Two-column grid with AJAX submission
- **Left column (col-8):**
  - Customer selection (auto-loads uncollected billings)
  - Billing selection table — shows outstanding billings with checkboxes
  - Payment allocation per billing
- **Right column (col-4):**
  - Payment details: Check #, Check Date, Deposit Date
  - Bank Account selection
  - Total Amount
- **Toggle:** Undocumented vessel badge

### Edit Collection

- Can modify payment details and re-allocate to billings
- Reverts old allocations and applies new ones

### Preview

- Read-only view of collection and all paid billings
- Shows check details and bank account

## Key Actions

| Action | Description |
|--------|-------------|
| Create | Record a new payment against billings |
| Edit | Modify payment details |
| Preview | View collection details |

## Payment Methods

- **Check:** Requires check number, check date, bank account
- **Cash:** Recorded as direct payment

## Tips

- Select the **Customer** first — uncollected billings load automatically
- Partial payments are supported (pay a portion of a billing)
- The bank account dropdown is populated from **Bank Account** master file
- Check if a customer is VATable — it affects the allocation
