# 10. Notifications & Audit Trail

## Notifications

**Path:** MSAP > Notifications

![Notification Center](/docs-images/notifications-audit/notification-center.png)

The Notification Center displays workflow alerts and action items for the current user.

### Features
- **Unread count:** Badge on the notification icon in the top navigation
- **List view:** Each notification shows icon, message, timestamp, read/unread state
- **Actionable notifications:** Some require a response (e.g., tariff approval requests)

### Actions

| Action | Description |
|--------|-------------|
| Mark as Read | Mark a single notification as read |
| Mark All as Read | Mark all notifications as read |
| Archive | Remove a notification from the list |
| Archive All | Remove all notifications |
| Respond | Respond to actionable notifications (e.g., approve/deny) |

### Notification Triggers
- Workflow state transitions (e.g., ticket needs tariff approval)
- Batch operations (e.g., Post Selected, Cancel Selected)
- System-generated alerts

## Audit Trail

**Path:** MSAP > Audit Trail

![Audit Trail](/docs-images/notifications-audit/audit-trail.png)

Every create, edit, delete, and state transition is logged with full details.

### Features

| Section | Description |
|---------|-------------|
| **Workflow Trace** | Select a Job Order to see a timeline of all related events |
| **Audit Trail Table** | Full searchable log of all system activity |

### Audit Log Columns
| Column | Description |
|--------|-------------|
| Date/Time | When the action occurred (Philippine Time) |
| User | Who performed the action |
| Action | What was done (Create, Edit, Delete, Approve, Disapprove, Post, Collect, etc.) |
| Entity | Which entity type (JobOrder, DispatchTicket, Billing, Collection, etc.) |
| Key | The record ID |
| Details | Description of what changed |
| IP Address | Client IP at time of action |

### Workflow Trace
1. Select a **Job Order** from the dropdown
2. Click the search/trace button
3. View a chronological timeline of all events related to that Job Order

## Tips

- Notifications are **user-specific** — you only see your own
- Use **Mark All as Read** after reviewing your notifications
- The Audit Trail is append-only — records cannot be deleted
- Use **Workflow Trace** for auditing a specific Job Order's lifecycle
- Audit trails include IP addresses for security auditing
