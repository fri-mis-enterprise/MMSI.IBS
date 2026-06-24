# MSAP Workflow Analysis: Job Order → Dispatch Ticket → Billing → Collection
> Comprehensive text breakdown for flowchart crafting — traced from Views → Controllers → Services → Repository

---

## STATUS REFERENCE TABLE

| Entity | Statuses |
|---|---|
| **Job Order** | `Open` → `Closed` |
| **Dispatch Ticket** | `Pending` → `For Tariff` → `For Approval` → `Disapproved` (terminal) / `For Billing` → `Billed` (terminal) |
| **Billing** | `For Posting` → `For Collection` → `Collected` |

---

## STAGE 1: JOB ORDER
**Controller:** `JobOrderController.cs` | **Service:** `JobOrderService.cs`

### 1.1 — Index / List View
- **Route:** `GET /User/JobOrder/Index`
- **Access:** Requires any of: `CreateJobOrder`, `EditJobOrder`, `DeleteJobOrder`, `CloseJobOrder`
- **What happens:** Returns the view shell; actual data is loaded via AJAX (DataTables server-side).
- **AJAX Endpoint:** `POST /User/JobOrder/GetJobOrderList` — calls `jobOrderService.GetPagedJobOrdersAsync()` → `unitOfWork.JobOrder.GetPagedJobOrdersAsync()`, returns paged+filtered JSON.

---

### 1.2 — Create Job Order
**Entry Point:** User clicks "Create Job Order"

#### Step A: GET /User/JobOrder/Create
- Controller calls `jobOrderService.PopulateJobOrderViewModelAsync(null)`.
- Service fetches from DB: Customers, Vessels, Ports, Terminals (filtered by Port), Tugboats.
- View rendered with `JobOrderViewModel` (empty form).
- **Fields required:** Date, Customer, Vessel, Port, Terminal, COS Number, Voyage Number, Planned Start Time, Planned End Time, Preferred Tugboat, Required Tug Count, Is Confirmed, Remarks.

#### Step B: POST /User/JobOrder/Create (form submission)
1. **Controller:** Validates `ModelState`. If invalid → re-populate & return same view.
2. **Controller:** Maps view model → `JobOrder` entity.
3. **Service `CreateJobOrderAsync()`:**
   - Validates: `PlannedEndTime > PlannedStartTime` (if both provided). Failure → returns error.
   - Sets `Status = "Open"`.
   - Generates `JobOrderNumber` → `unitOfWork.JobOrder.GenerateJobOrderNumber()`.
   - Sets `CreatedBy`, `CreatedDate`.
   - Saves via `unitOfWork.JobOrder.AddAsync()` + `unitOfWork.SaveAsync()`.
   - Records AuditTrail: `"Created Job Order #XXXX"`.
   - **Notification:** Notifies users with `CreateDispatchTicket` permission → `"A new Job Order #XXXX for [VesselName] has been created and is ready for Dispatch."` (links to Job Order Details).
4. **Controller (on success):**
   - Fires **SignalR** → `TugboatHub.TimelineChanged` (updates planning timelines).
   - Redirects → `Job Order Details` page.

---

### 1.3 — Job Order Details View
- **Route:** `GET /User/JobOrder/Details/{id}`
- Controller calls `jobOrderService.GetJobOrderByIdAsync(id)` — fetches full entity with related data.
- Also calls `dispatchTicketService.PopulateServiceRequestViewModelAsync(null, jobOrderId)` — pre-populates the "Add Dispatch Ticket" panel.
- View shows: Job Order header info + list of associated Dispatch Tickets (with statuses) + action buttons.
- **Actions available from this view:** Edit Job Order, Close Job Order, Add Dispatch Ticket, Assign/Unassign Tugboat.

---

### 1.4 — Edit Job Order
#### GET /User/JobOrder/Edit/{id}
- Fetches Job Order by ID.
- **Guard:** If `Status == "Closed"` → redirect back to Details with error (cannot edit a closed order).
- Populates `JobOrderViewModel` with current data + select lists.
- View also checks if there are any linked `DispatchTickets` (`ViewData["HasTickets"]`).

#### POST /User/JobOrder/Edit
1. **Service `UpdateJobOrderAsync()`:**
   - Fetches existing record. Fails if not found or if `Status == "Closed"`.
   - Validates planned times.
   - Updates all fields: Date, Customer, Vessel, Port, Terminal, COS, Voyage, Times, Tugboat preferences.
   - **Cascade Sync (`SyncRelatedRecordsAsync`):** Very important — when Job Order is edited:
     - All linked `DispatchTickets` that are **NOT `Billed`** are updated with new Customer, Vessel, Voyage, COS, Port, Terminal, Date.
     - All linked `Billings` with status `"For Posting"` or `"For Collection"` are updated with same fields.
   - Records AuditTrail: `"Edited Job Order #XXXX"`.
   - Saves.
2. **Controller (on success):** Fires SignalR `TimelineChanged`. Redirects to Details.

---

### 1.5 — Assign / Unassign Tugboat
**POST /User/JobOrder/AssignTugboat**
- **Service `AssignTugboatAsync()`:**
  - Checks if tugboat already assigned (as PreferredTugboat or in any DispatchTicket).
  - If `PreferredTugboatId == null` → sets it as Preferred Tugboat.
  - If a preferred is already set → creates a new `DispatchTicket` in `Pending` status for this tugboat (auto-assigned dispatch).
  - Records audit. Saves.
- **Controller:** Fires SignalR `TimelineChanged` + `PlanningHub.OnPlanUpdated` (port-specific).

**POST /User/JobOrder/UnassignTugboat**
- **Service `UnassignTugboatAsync()`:**
  - Guard: Cannot unassign if ticket is not `Pending` or `ForTariff`.
  - Clears `PreferredTugboatId` or removes the DispatchTicket (if auto-created).
  - Records audit. Saves.

---

### 1.6 — Close Job Order
**POST /User/JobOrder/Close**

1. **Controller:** Checks TempData for "pending confirm" ID (force-close flow).
2. **Service `CloseJobOrderAsync()`:**
   - Fetches Job Order with all DispatchTickets.
   - Fails if already `Closed`.
   - **Critical Validation:** Checks for "non-terminal" tickets — any ticket NOT in `Billed` or `Disapproved` status will block closure.
     - If blocking tickets exist → returns error: `"Cannot close Job Order. X ticket(s) are in non-terminal states (...)."`.
   - If all tickets are in terminal states → sets `Status = "Closed"`.
   - Records AuditTrail: `"Closed Job Order #XXXX"`. Saves.
3. **Controller (on success):** Fires SignalR `TimelineChanged`. Redirects to Details.

> **Note:** Job Order can also be **auto-closed** by `BillingService.PostBillingAsync()` — see Stage 3.

---

## STAGE 2: DISPATCH TICKET
**Controller:** `DispatchTicketController.cs` | **Service:** `DispatchTicketService.cs`

### 2.1 — Index / List View
- **Route:** `GET /User/DispatchTicket/Index?filterType=...`
- Filter types typically: `All`, `Pending`, `For Tariff`, `For Approval`, `For Billing`, `Billed`, `Cancelled`.
- **AJAX Endpoint:** `POST /User/DispatchTicket/GetDispatchTicketLists` with `filterType` parameter → `dispatchTicketService.GetPagedDispatchTicketsAsync()`. Also fetches signed media URLs (image/video) from Cloud Storage.

---

### 2.2 — Create Dispatch Ticket
**Can be accessed from:** Job Order Details page (pre-fills Job Order context) OR standalone from Dispatch Ticket Index.

#### GET /User/DispatchTicket/Create?jobOrderId={id}
- Service `PopulateServiceRequestViewModelAsync()`:
  - If `jobOrderId` provided → auto-fills Customer, Vessel, Port, Terminal, Voyage, COS, Date from the parent Job Order.
  - Loads select lists: Customers, Tugboats, Tug Masters, Vessels, Ports, Terminals, Services.

#### POST /User/DispatchTicket/Create
**Fields required:** Dispatch Number, Tugboat, Tug Master, Service, Date Left + Time Left (departure), Date Arrived + Time Arrived (arrival), Customer, Vessel, Terminal, Remarks. Optional: image/video upload.

**Service `CreateDispatchTicketAsync()`:**
1. **Guard:** If linked to a Job Order, checks `IsJobOrderEditableAsync()` — job order must NOT be Closed or Cancelled.
2. Maps view model → `DispatchTicket` entity.
3. **Media upload (if provided):** Uploads image and/or video to Cloud Storage (GCS), stores file name + URL.
4. **Inherits from Job Order:** If `JobOrderId` is set, syncs Customer, Vessel, Port, Terminal, Voyage, COS, Date from parent.
5. **Status determination (key logic):**
   - If **Date/Time Left AND Date/Time Arrived are provided** (completed trip):
     - Validates `Arrival > Departure`.
     - Calculates `TotalHours = MAX(actual hours, 0.5)` rounded to 4 decimals.
     - Sets `Status = "For Tariff"`.
   - If **Departure/Arrival not provided** (trip not yet started):
     - Sets `Status = "Pending"`.
6. Records AuditTrail: `"Create dispatch ticket #XXXX"`.
7. Saves.
8. **Notification (if "For Tariff"):** Notifies users with `SetTariff` permission → `"Dispatch Ticket #XXXX for [VesselName] has been completed and is ready for Tariff application."`.
9. **Controller:** Fires SignalR `TimelineChanged`. Redirects → Job Order Details (if from JO) or Dispatch Index.

---

### 2.3 — Dispatch Ticket Preview
- **Route:** `GET /User/DispatchTicket/Preview/{id}`
- Fetches ticket with all details. Generates **signed URLs** for image/video from cloud storage (time-limited access URLs).
- Read-only view of the dispatch ticket.

---

### 2.4 — Edit Dispatch Ticket
#### GET /User/DispatchTicket/EditTicket/{id}
- **Guard:** Checks `IsJobOrderEditableAsync()` — parent Job Order must be open.
- Fetches ticket, maps to `ServiceRequestViewModel`.
- Fetches signed URLs for media.
- Loads select lists.

#### POST /User/DispatchTicket/EditTicket
**Service `UpdateDispatchTicketAsync()`:**
1. **Guard:** Parent Job Order must be editable.
2. Fetches existing ticket.
3. **Media update:** If new image/video uploaded → deletes old from cloud, uploads new.
4. **Date/Time validation:** If departure/arrival provided, validates arrival > departure, recalculates TotalHours.
5. **Detailed change tracking:** Tracks all changed fields (Date, DispatchNumber, COSNumber, VoyageNumber, CustomerId, DateLeft, TimeLeft, DateArrived, TimeArrived, TerminalId, PortId, ServiceId, TugBoatId, TugMasterId, VesselId, Remarks).
6. **Smart Tariff Reset (critical):** If any of these fields changed — `ServiceId`, `TugBoatId`, `DateLeft`, `TimeLeft`, `DateArrived`, `TimeArrived`, `TotalHours` — ALL tariff data is reset to zero and status reverts to `"For Tariff"`. This forces re-tariffing.
7. Records detailed AuditTrail with all changes listed.
8. Saves.
9. **Controller:** Fires SignalR `TimelineChanged`.

---

### 2.5 — Set Tariff (First time)
**Triggered when ticket is in "For Tariff" status.**

#### GET /User/DispatchTicket/SetTariff/{id}
- **Access:** Requires `SetTariff` permission.
- Loads full ticket details. Generates signed media URLs.
- Builds `TariffViewModel` with: Dispatch info, Tugboat info (Owner name, FixedRate, IsCompanyOwned), Customer, all current rates.
- Loads Customer list for dropdown.

#### POST /User/DispatchTicket/SetTariff
**Fields:** `DispatchRate`, `DispatchDiscount`, `DispatchBillingAmount`, `DispatchNetRevenue`, `BAFRate`, `BAFDiscount`, `BAFBillingAmount`, `BAFNetRevenue`, `TotalBilling`, `TotalNetRevenue`, `ApOtherTugs`. Also `chargeType` (Dispatch: "Per hour" or "Flat") and `chargeType2` (BAF: "Per hour" or "Flat").

**Service `SaveTariffAsync(isEdit: false)`:**
1. **Guard:** Parent Job Order must be editable.
2. **Server-side recalculation (override client values for integrity):**
   - Dispatch: `Rate × Hours` (if "Per hour") or `Rate` (if "Flat"). Net = `(Rate - DiscountAmount) × Hours`.
   - BAF: Same logic with BAFRate.
   - `TotalBilling = DispatchBilling + BAFBilling`.
   - `TotalNetRevenue = DispatchRevenue + BAFRevenue`.
3. Sets `Status = "For Approval"`.
4. Records `TariffBy = username`, `TariffDate = now`.
5. Records AuditTrail: `"Set Tariff #XXXX"`.
6. Saves.
7. **Notification:** Notifies users with `ApproveTariff` permission → `"Tariff has been set for Dispatch Ticket #XXXX. Pending your approval."`.

---

### 2.6 — Edit Tariff (Re-tariffing)
Same as Set Tariff but `isEdit: true`. Records `TariffEditedBy` + `TariffEditedDate`. Audit message includes field-level changes.

---

### 2.7 — Approve Tariff
**POST /User/DispatchTicket/ApproveTariff**

**Service `ApproveTariffAsync()`:**
1. Guard: Parent Job Order must be editable.
2. Sets `Status = "For Billing"`.
3. Records `EditedBy`, `EditedDate`.
4. Records AuditTrail: `"Approved tariff for dispatch ticket #XXXX"`.
5. Saves.
6. **Notification:** Notifies users with `CreateBilling` permission → `"Tariff for Dispatch Ticket #XXXX has been approved. Ready for Billing."`.

---

### 2.8 — Disapprove Tariff
**POST /User/DispatchTicket/DisapproveTariff** (requires reason ≥ 10 chars)

**Service `DisapproveTariffAsync()`:**
1. Guard: Parent Job Order must be editable.
2. Validates reason length.
3. Sets `Status = "Disapproved"`.
4. Appends reason to `Remarks`.
5. Records AuditTrail with reason.
6. Saves.
7. **Notification:** Notifies users with `SetTariff` permission → `"Tariff for Dispatch Ticket #XXXX was disapproved. Reason: [reason]."`.

> **Effect on Job Order closure:** A `Disapproved` ticket is a terminal state — the Job Order CAN be closed even if some tickets are Disapproved (as long as all others are Billed or Disapproved).

---

### 2.9 — Tariff Rate Lookup (AJAX)
**GET /User/DispatchTicket/CheckForTariffRate?customerId=X&dispatchTicketId=Y**

**Service `CheckForTariffRateAsync()`:**
- Looks up `TariffTable` with cascading priority:
  1. Match by `CustomerId` + `TerminalId` + `ServiceId` + `AsOfDate <= DateLeft`.
  2. Match by `CustomerId` + `TerminalId` + `AsOfDate <= DateLeft`.
  3. Match by `CustomerId` + `AsOfDate <= DateLeft`.
- Returns `{Dispatch, BAF, DispatchDiscount, BAFDiscount, Exists}` — auto-fills tariff form.

---

### 2.10 — Generic Status Change
**POST /User/DispatchTicket/ChangeStatus** — generic endpoint used for bulk/simple status updates without full service logic. Directly sets `model.Status`, logs audit trail, saves.

---

## STAGE 3: BILLING
**Controller:** `BillingController.cs` | **Service:** `BillingService.cs`

### 3.1 — Index / List View
- **Route:** `GET /User/Billing/Index?filterType=...`
- **AJAX Endpoint:** `POST /User/Billing/GetBillingList` → `billingService.GetPagedBillingsAsync()`.

---

### 3.2 — Create Billing
**Prerequisites:** At least one Dispatch Ticket in `"For Billing"` status must exist.

#### GET /User/Billing/Create
- Calls `billingService.PopulateBillingSelectListsAsync()` → loads Vessels, Ports, Customers (with billable tickets), Terminals.

#### POST /User/Billing/Create
**Fields:** Customer, Principal (optional), Billing Number (or auto-generate if Undocumented), Date, Billed To (LOCAL/FOREIGN), Voyage Number, COS Number, Vessel, Port, Terminal, Job Order (optional), list of selected Dispatch Ticket IDs.

**Service `CreateBillingAsync()`:**
1. If `JobOrderId` provided → inherits Customer, Vessel, Port, Terminal, Voyage, COS from Job Order (if not already set).
2. Validates Customer exists.
3. Sets `IsVatable` from Customer's `VatType`.
4. Sets `Status = "For Posting"`.
5. Determines `Terms` (from Principal if set, otherwise from Customer). Defaults to `"COD"`.
6. Calculates `DueDate` from terms.
7. Billing Number: if `IsUndocumented` → auto-generate. Otherwise → must be manually provided.
8. Validates at least 1 ticket selected.
9. **For each selected Dispatch Ticket:**
   - Validates ticket exists + belongs to the Job Order (if JO-linked).
   - Accumulates: `TotalNetRevenue`, `DispatchNetRevenue`, `BAFNetRevenue`.
   - Sets `ticket.Status = "Billed"`.
   - Links ticket to this billing (`BillingId`, `BillingNumber`).
10. Calculates `Amount = Balance`:
    - If Vatable AND not VAT-inclusive: `TotalNetRevenue × 1.12`.
    - Otherwise: `TotalNetRevenue` as-is.
11. Sets `DispatchAmount`, `BAFAmount`, `IsPaid = false`.
12. Saves Billing + all ticket updates.
13. Records AuditTrail: `"Created Billing #XXXX"`.
14. **Notification:** Notifies users with `ViewGeneralLedger` permission → `"New Billing #XXXX for [Customer] has been created. Ready for Posting."`.
15. **Controller:** Returns JSON `{success, redirectUrl}` (AJAX-based form).

---

### 3.3 — Edit Billing
#### GET /User/Billing/Edit/{id}
- Loads billing. Loads select lists. Loads `UnbilledDispatchTickets` for the customer (for adding more tickets). Loads `ToBillDispatchTickets` (currently linked). Loads Principals. Shows customer info in ViewData.

#### POST /User/Billing/Edit
**Service `UpdateBillingAsync()`:**
1. Fetches existing billing. Validates Customer.
2. **Reverts old ticket allocations:** All tickets currently linked to this billing are set back to `"For Billing"` with `BillingId = null`, `BillingNumber = null`.
3. Updates Billing fields: Customer, Principal, Voyage, COS, Date, Port, Terminal, Vessel, BilledTo, JobOrderId, IsVatInclusive, PrintWht.
4. **Re-applies new ticket selections** (same loop as Create).
5. Recalculates Amount.
6. Records AuditTrail: `"Edit billing #XXXX"`. Saves.

---

### 3.4 — Delete Billing
**Service `DeleteBillingAsync()`:**
- Reverts all linked tickets to `"For Billing"` (so they can be re-billed).
- Removes billing record. Saves.

---

### 3.5 — Post Billing to Books ⭐ (Critical Step)
**Route:** `GET /User/Billing/Post/{id}` (requires `CreateBilling` access)

**Service `PostBillingAsync()`** — runs inside a **database transaction**:
1. Fetches billing. Validates `Status == "For Posting"` (must not be already posted).
2. Fetches Customer, Principal, Vessel.
3. **Creates `SalesBook` entry:**
   - `TransactionDate`, `SerialNo` (= Billing Number), `SoldTo`, `TinNo`, `Address`, `Description` (Vessel name), `Amount` (net of discount).
   - If Vatable: computes `VatableSales = Amount / 1.12`, `VatAmount = VatableSales × 0.12`, `NetSales`.
   - If Non-Vatable (Zero-rated): `ZeroRated = Amount`, `NetSales`.
   - Sets `DueDate`, `DocumentId`, `Company`.
4. **Creates `GeneralLedgerBook` entries:**
   - **Debit: AR Trade** (Account `101020100`) — full amount (gross).
   - **Credit: Maritime Service Revenue** (Account: MSAP revenue account) — net of VAT.
   - **Credit: Output VAT** (Account: MSAP output VAT account) — VAT amount (if Vatable).
   - Validates journal is **balanced** (total debits = total credits). Throws if not.
5. Sets `Billing.Status = "For Collection"`.
6. Records AuditTrail: `"Posted Billing #XXXX"`. Saves.
7. **Notification:** Notifies users with `CreateCollection` permission → `"Billing #XXXX for [Customer] has been posted. Ready for Collection."`.
8. **Auto-close Job Order (if linked):** Calls `jobOrderService.CloseJobOrderAsync()` — if all tickets are Billed/Disapproved, the Job Order is automatically Closed. Failure is logged as warning (does not block billing post).

---

### 3.6 — Preview & Print Billing
#### Preview (GET /User/Billing/Preview/{id})
- Loads billing with: `ToBillDispatchTickets`, `PaidDispatchTickets`, `UniqueTugboats`.
- Calls `unitOfWork.Billing.ProcessAddress()` for address formatting.
- Read-only rendered view.

#### Print (GET /User/Billing/Print/{id})
- **Service `GenerateExcelForPrintingAsync()`:** Generates dot-matrix-style Excel file with:
  - Customer info, Date, Voyage Number, Vessel Name, Port.
  - Per-Tugboat breakdown: Service name, departure/arrival times, dispatch rate, dispatch billing amount.
  - BAF section (if applicable).
  - Subtotal, 12% VAT (if vatable), LESS 2% WHT (if applicable), LESS 5% WVAT (if applicable), Net/Total Amount Due.
- Returns as `.xlsx` file download.

---

## STAGE 4: COLLECTION
**Controller:** `CollectionController.cs` | **Service:** `CollectionService.cs`

### 4.1 — Index / List View
- **Route:** `GET /User/Collection/Index`
- **AJAX Endpoint:** `POST /User/Collection/GetCollectionList` → `collectionService.GetPagedCollectionsAsync()`.

---

### 4.2 — Create Collection
**Prerequisites:** At least one Billing in `"For Collection"` status must exist for a customer.

#### GET /User/Collection/Create
- `collectionService.PopulateCreateViewModelAsync()`:
  - Loads `Customers` with collectible billings → `unitOfWork.Collection.GetMsapCustomersWithCollectiblesSelectList()`.
  - Loads `BankAccounts` (for check deposits).

#### POST /User/Collection/Create
**Fields:** Customer, Collection Number (or auto-generate if Undocumented), Date, Amount, Cash Amount, Check Amount (Check Number, Date, Bank, Branch), Bank Account for deposit, Deposit Date, Reference No, Remarks, EWT (Expanded Withholding Tax), WVAT (Withholding VAT), and list of `BillingPayments` (each = `{BillingId, AmountToPay}`).

**Service `CreateCollectionAsync()`** — runs inside a **database transaction**:
1. **Maps ViewModel → `Collection` entity:**
   - Sets `Date`, `CustomerId`, `Amount`, `EWT`, `WVAT`.
   - `Total = Amount + EWT + WVAT` (Gross amount).
   - Sets cash/check details, bank account info.
   - `Company = "MMSI"`.
2. Generates `CollectionNumber` (if Undocumented) or uses manual entry.
3. **Amount Validation:** `Sum(BillingPayments.AmountToPay) must equal Amount` (unless Undocumented — loose validation).
4. Saves Collection record first.
5. **Allocates payment to each selected Billing:**
   - Sets `billing.Status = "Collected"`.
   - Sets `billing.CollectionId`, `billing.CollectionNumber`.
   - Calls `unitOfWork.Collection.UpdateBillingPayment()` to record the partial or full payment amount.
6. **Posts to Books (`unitOfWork.Collection.PostAsync()`):**
   - Creates cash receipts journal / general ledger entries for the collection.
   - EWT and WVAT handled as separate deductions.
7. Final save.
8. Records AuditTrail: `"Create collection #XXXX for billings #X, #Y, ..."`.
9. **Notification:** Notifies users with `ViewGeneralLedger` permission → `"New Collection #XXXX for [Customer] has been created."`.

---

### 4.3 — Edit Collection
#### GET /User/Collection/Edit/{id}
- `collectionService.PopulateEditViewModelAsync()`: Loads collection, maps to ViewModel. Loads existing billing allocations. Loads uncollected billings for customer. Loads bank accounts.

#### POST /User/Collection/Edit
**Service `UpdateCollectionAsync()`** — runs inside a **database transaction**:
1. Fetches existing collection.
2. **Reverts old billing allocations:**
   - All billings previously linked → set back to `"For Collection"`, `CollectionId = 0`, `CollectionNumber = null`.
   - Calls `unitOfWork.Collection.RemoveBillingPayment()` to undo the payment record.
3. **Validates new amount vs. allocated total** (unless Undocumented).
4. **Re-applies new billing allocations:** For each new payment → `billing.Status = "Collected"`, links CollectionId/CollectionNumber, calls `UpdateBillingPayment()`.
5. **Updates collection fields:** Date, Customer, Collection Number, Reference, Remarks, Cash/Check amounts, Bank info, EWT, WVAT, Total.
6. Records AuditTrail: `"Edit collection #XXXX"`. Saves.

---

### 4.4 — Preview Collection
- **Route:** `GET /User/Collection/Preview/{id}`
- `collectionService.GetCollectionByIdAsync()`: Fetches collection + all linked `PaidBills`.
- Read-only view showing the collection receipt with all paid billings.

---

### 4.5 — AJAX Endpoints (Collection)
| Endpoint | Purpose |
|---|---|
| `POST GetSelectedBillings(billingIds)` | Fetches billing details for selected IDs (used in form preview) |
| `GET IsCustomerVatable(customerId)` | Returns boolean — drives EWT/WVAT display |
| `GET GetBankAccountDetails(bankId)` | Returns Bank, AccountNo, AccountName |
| `GET GetUncollectedBillingsForTable(customerId, collectionId?)` | Returns all "For Collection" billings for customer with computed EWT/WVAT/Net. If editing, includes already-associated billings. |
| `GET SearchCustomers(term)` | Typeahead search for customers |

---

## CROSS-CUTTING CONCERNS

### Audit Trail
Every state-changing operation records an `AuditTrail` entry with:
- `Username` (who performed the action)
- `Activity` (description: "Created", "Edited", "Closed", etc.)
- `DocumentType` (e.g., "Job Order", "Dispatch Ticket", "Billing", "Collection")
- `RecordId` / `ReferenceNumber` (for traceability)

### Notifications (SignalR + In-App)
| Trigger Event | Notified Role | Message |
|---|---|---|
| Job Order Created | `CreateDispatchTicket` | "JO #XXXX created, ready for Dispatch" |
| Dispatch Ticket ForTariff | `SetTariff` | "Ticket #XXXX ready for Tariff" |
| Tariff Set (ForApproval) | `ApproveTariff` | "Tariff set, pending approval" |
| Tariff Disapproved | `SetTariff` | "Tariff disapproved, reason: ..." |
| Tariff Approved (ForBilling) | `CreateBilling` | "Ticket #XXXX approved, ready for Billing" |
| Billing Created (ForPosting) | `ViewGeneralLedger` | "Billing #XXXX ready for Posting" |
| Billing Posted (ForCollection) | `CreateCollection` | "Billing #XXXX posted, ready for Collection" |
| Collection Created | `ViewGeneralLedger` | "Collection #XXXX created" |

### Real-Time Sync (SignalR Hubs)
- **`TugboatHub` (`TimelineChanged`):** Fired on Job Order create/edit, Dispatch Ticket create/edit/assign. Updates gantt/timeline views.
- **`PlanningHub` (`OnPlanUpdated`):** Fired on tugboat assign/unassign per port. Updates vessel planning board.

---

## COMPLETE END-TO-END STATUS FLOW

```
[Job Order Created]
  Status: OPEN
      |
      v
[Dispatch Ticket Created]
  If complete trip info → Status: FOR TARIFF
  If trip not yet started → Status: PENDING
      |
      | (when trip completed, edit ticket with departure/arrival)
      v
  Status: FOR TARIFF
      |
      | (SetTariff action: enter rates + compute amounts)
      v
  Status: FOR APPROVAL
      |
      |──[Disapprove]──→ Status: DISAPPROVED (terminal, no billing)
      |
      | (ApproveTariff action)
      v
  Status: FOR BILLING
      |
      | (Included in a Billing record)
      v
  Status: BILLED
  [Billing Created] → Status: FOR POSTING
      |
      | (Post to Books: SalesBook + GeneralLedger)
      v
  [Billing] Status: FOR COLLECTION
  [Job Order auto-close attempt: if all tickets Billed/Disapproved → Status: CLOSED]
      |
      | (Collection created, payment allocated)
      v
  [Billing] Status: COLLECTED
  [Collection] Status: (no explicit status, collection is always "active")
  [Accounting Entries: Cash In Bank DR, AR Trade CR, EWT/WVAT as applicable]
```

---

## KEY BUSINESS RULES SUMMARY

1. **Job Order cannot be Closed** if any linked Dispatch Ticket is still in `Pending`, `ForTariff`, `ForApproval`, or `ForBilling` state.
2. **Dispatch Tickets cannot be edited or tariffed** if parent Job Order is `Closed`.
3. **Editing critical ticket fields** (Service, Tugboat, Times, Hours) **auto-resets tariff** and sends status back to `ForTariff`.
4. **Billing requires at least one `ForBilling` ticket** to be selected.
5. **Posting Billing creates immutable accounting entries** (SalesBook + GL). Only after posting does billing status move to `ForCollection`.
6. **Collection payment total must equal** the sum of individual billing payment allocations (strict validation, unless Undocumented).
7. **Cascade on Job Order Edit:** Changing JO header data propagates to all un-billed Dispatch Tickets and un-posted Billings automatically.
8. **Tariff rate auto-lookup** checks Customer + Terminal + Service + AsOfDate with fallback to less specific matches.
9. **VAT:** Applied if Customer is `"Vatable"`. Computation: `NetRevenue × 1.12` for billing amount. GL splits into revenue + output VAT.
10. **EWT/WVAT at Collection:** Computed based on `WithHoldingTax` and `WithHoldingVat` flags on Customer. Deducted from net collectible amount.
