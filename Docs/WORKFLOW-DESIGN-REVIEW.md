# Dispatch / Service Request Workflow — Design Review

> Status: **mostly implemented (2026-08-01).** `Cancelled` removed, orphaned
> `PostSelected`/`CancelSelected` endpoints deleted, `ChangeStatus` tightened,
> Service Request split into its own `ServiceRequestStatus` enum, and `Pending`
> merged into `ForTariff`. Remaining TODO: tighten service-level state guards (§3.4).

---

## 1. The Problem

`DispatchTicket` previously carried **10 statuses** (plus `ServiceRequestDeleted`) in a single
`DispatchTicketStatus` enum, and Service Requests reused the same `DispatchTicket` entity:

```
Draft, Requested, Pending, For Tariff, For Approval, Disapproved,
For Billing, Billed, Cancelled, Deleted, Service Request Deleted
```

Two different lifecycles (Service Request vs. Dispatch Ticket) were mixed into one state set,
and several states were not reachable from the UI.

## 2. What the UI Actually Allows (audited 2026-08-01)

| Surface | Actions exposed |
|---------|-----------------|
| **DispatchTicket Index** | Set Tariff (For Tariff), Edit Tariff (For Approval), Edit Ticket (ForTariff/ForApproval), Approve (For Approval), Delete (ForTariff/Disapproved), Restore (Deleted) |
| **DispatchTicket Preview** | Approve / Disapprove (For Approval), Revoke Approval (For Billing) |
| **JobOrder Details** | Post Request (Requested), Set Tariff, Approve/Disapprove, Edit Tariff, Edit Ticket, Delete, Restore |
| **ServiceRequest Index** | Edit, Delete (Draft/Requested), Restore — **no Post, no Cancel** |

**Unreachable from UI (now removed 2026-08-01):**
- `Cancelled` — removed from `DispatchTicketStatus`, all writers, dashboard counts, SR filter, and badges. No data migration needed (0 rows in DB).
- `PostSelected` (batch post) — controller action deleted.
- `CancelSelected` (batch cancel) — controller action deleted.
- `ChangeStatus` generic endpoint — kept (Preview's "Revoke Approval" uses it) but `Cancelled` removed from valid targets and terminal-state guard.

## 3. Remaining Design (TODO)

### 3.1 Split Service Request into its own status enum

**DONE (2026-08-01):** `ServiceRequestStatus` (`Draft`, `Requested`, `ServiceRequestDeleted`)
created in `SD.cs`; `ServiceRequestController`, SR view, dashboard SR count, and all
SR-exclusion filters now reference it. `DispatchTicketStatus` slimmed to `ForTariff`,
`ForApproval`, `Disapproved`, `ForBilling`, `Billed`, `Deleted`. `Pending` merged into
`ForTariff` (create/restore always set `ForTariff`; SetTariff, delete, and batch flows
dropped the `Pending` branch). No data migration needed — no `Draft`/`Pending`/
`ServiceRequestDeleted` rows existed.

### 3.2 Remove or repurpose `Cancelled`

**DONE (2026-08-01):** constant removed from `DispatchTicketStatus`, along with all
writer paths, dashboard counts, SR filter button/JS, and badge maps. DB had zero
`Cancelled` rows, so no migration was needed.

### 3.3 Remove orphaned endpoints

**DONE (2026-08-01):** `PostSelected` and `CancelSelected` deleted from
`ServiceRequestController`. `ChangeStatus` retained but with `Cancelled` dropped
from its valid-target set and terminal-state guard.

### 3.4 Tighten state guards

`SaveTariffAsync`, `ApproveTariffAsync`, `DisapproveTariffAsync` currently set a new status
without asserting the current status. Add guard clauses matching the UI matrix in §2.

---

## 4. Reference Files

| Concern | File |
|---------|------|
| Status constants | `IBS.Utility/Constants/SD.cs:101` |
| DT status writes | `IBS.Services/DispatchTicketService.cs` |
| SR status writes | `IBSWeb/Areas/User/Controllers/ServiceRequestController.cs` |
| Generic ChangeStatus | `IBSWeb/Areas/User/Controllers/DispatchTicketController.cs:500` |
| DT list actions | `IBSWeb/Areas/User/Views/DispatchTicket/Index.cshtml` |
| SR list actions | `IBSWeb/Areas/User/Views/ServiceRequest/Index.cshtml` |
