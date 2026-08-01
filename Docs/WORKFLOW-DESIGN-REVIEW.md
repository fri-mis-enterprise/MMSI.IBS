# Dispatch / Service Request Workflow — Design Review

> Status: **proposal only — no code changed.** This file captures the gap between
> what the domain model allows and what the UI actually exposes, plus a proposed
> design to simplify the workflow. Docs were updated to match the UI; code is TODO.

---

## 1. The Problem

`DispatchTicket` currently carries **10 statuses** (plus `ServiceRequestDeleted`) in a single
`DispatchTicketStatus` enum, and Service Requests reuse the same `DispatchTicket` entity:

```
Draft, Requested, Pending, For Tariff, For Approval, Disapproved,
For Billing, Billed, Cancelled, Deleted, Service Request Deleted
```

Two different lifecycles (Service Request vs. Dispatch Ticket) are mixed into one state set,
and several states are not reachable from the UI.

## 2. What the UI Actually Allows (audited 2026-08-01)

| Surface | Actions exposed |
|---------|-----------------|
| **DispatchTicket Index** | Set Tariff (For Tariff), Edit Tariff (For Approval), Edit Ticket (Pending/ForTariff/ForApproval), Approve (For Approval), Delete (Pending/ForTariff/Disapproved), Restore (Deleted) |
| **DispatchTicket Preview** | Approve / Disapprove (For Approval), Revoke Approval (For Billing) |
| **JobOrder Details** | Post Request (Requested), Set Tariff, Approve/Disapprove, Edit Tariff, Edit Ticket, Delete, Restore |
| **ServiceRequest Index** | Edit, Delete (Draft/Requested), Restore — **no Post, no Cancel** |

**Unreachable from UI:**
- `Cancelled` — no view sets it. `CancelSelected` (batch cancel) exists in
  `ServiceRequestController` but no view calls it.
- `PostSelected` (batch post) — controller action exists, no view calls it.
- `ChangeStatus` generic endpoint — only used by Preview's "Revoke Approval"
  (`ForBilling → ForApproval`); otherwise it accepts arbitrary transitions the UI never offers.

## 3. Proposed Design (TODO)

### 3.1 Split Service Request into its own status enum

- New `ServiceRequestStatus`: `Draft`, `Requested`, `ServiceRequestDeleted`.
- New `DispatchTicketStatus` (slimmed): `Pending`, `ForTariff`, `ForApproval`,
  `Disapproved`, `ForBilling`, `Billed`, `Deleted`.
- Service Request no longer reuses `DispatchTicketStatus`.

### 3.2 Remove or repurpose `Cancelled`

Options (pick one when implementing):
- **Remove** the constant and `ChangeStatus`/`CancelSelected` paths that write it, OR
- Keep it only for **legacy data** — map existing `Cancelled` rows to `Deleted` in a migration.

### 3.3 Remove orphaned endpoints

- `ServiceRequestController.PostSelected` — no UI calls it.
- `ServiceRequestController.CancelSelected` — no UI calls it.
- Tighten `DispatchTicketController.ChangeStatus` to a whitelisted transition map
  (or remove it; Preview's revoke is the only consumer).

### 3.4 Tighten state guards

`SaveTariffAsync`, `ApproveTariffAsync`, `DisapproveTariffAsync` currently set a new status
without asserting the current status. Add guard clauses matching the UI matrix in §2.

---

## 4. Reference Files

| Concern | File |
|---------|------|
| Status constants | `IBS.Utility/Constants/SD.cs:101` |
| DT status writes | `IBS.Services/DispatchTicketService.cs` |
| SR status writes / orphaned endpoints | `IBSWeb/Areas/User/Controllers/ServiceRequestController.cs` (PostSelected:665, CancelSelected:730) |
| Generic ChangeStatus | `IBSWeb/Areas/User/Controllers/DispatchTicketController.cs:501` |
| DT list actions | `IBSWeb/Areas/User/Views/DispatchTicket/Index.cshtml` |
| SR list actions | `IBSWeb/Areas/User/Views/ServiceRequest/Index.cshtml` |
