# Comprehensive Technical Specification: Tugboat Monitoring & Scheduling

## 1. Project Overview & Rationale
**Objective:** Transform the current reactive tugboat logging system into a proactive, visual, and predictable monitoring control center.

**The "Why":** 
Legacy operations suffer from "invisible conflicts"—situations where a tugboat is double-booked or remains busy longer than expected. Record-based tables (timestamps) are insufficient for human dispatchers to quickly "pattern-match" availability.

---

## 2. Data Layer & Persistence (Phase 1)

### A. Database Extensions (`mmsi_job_orders`)
We will add the following columns via EF Core Migrations:
| Property | Type | DB Column | Description |
| :--- | :--- | :--- | :--- |
| `PlannedStartTime` | `DateTime?` | `planned_start_time` | Expected start of operation. |
| `PlannedEndTime` | `DateTime?` | `planned_end_time` | Expected completion of operation. |
| `PreferredTugboatId` | `int?` | `preferred_tugboat_id` | Foreign Key to `mmsi_tugboats`. |

**EF Core Configuration:**
```csharp
builder.Entity<JobOrder>()
    .HasOne(j => j.PreferredTugboat)
    .WithMany()
    .HasForeignKey(j => j.PreferredTugboatId)
    .OnDelete(DeleteBehavior.SetNull);

builder.Property(j => j.PlannedStartTime).HasColumnType("timestamp without time zone");
builder.Property(j => j.PlannedEndTime).HasColumnType("timestamp without time zone");
```

### B. Logical State Mapping
The system determines a block's state by checking both `JobOrder` and its related `DispatchTicket`:
1.  **Planned (Yellow):** `JobOrder` has planning fields + No linked `DispatchTicket`.
2.  **In-Progress (Green):** `DispatchTicket` exists + `TimeLeft` is set + `TimeArrived` is NULL.
3.  **Completed (Blue):** `DispatchTicket` exists + both `TimeLeft` and `TimeArrived` are set.

---

## 3. The "Brain": Service Logic (`IBS.Services`)

### A. Data Aggregation (`TugboatMonitoringService.cs`)
The service must aggregate two disparate entities into a unified "Timeline Block":
```csharp
public class TimelineBlockDto
{
    public string Id { get; set; } // JO-123 or DT-456
    public string Title { get; set; } // Vessel Name / Service
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Status { get; set; } // Planned, InProgress, Completed
    public bool IsConflict { get; set; }
    public string LinkUrl { get; set; } // Link to JO Details or DT Edit
}
```

### B. Conflict Detection Engine (Granular Logic)
Conflict detection must account for a "Transit Buffer" (e.g., 30 minutes) to allow the tugboat to travel between jobs.
- **Logic:** `Overlap = (RequestedStart < ExistingEnd + Buffer) && (RequestedEnd > ExistingStart - Buffer)`.
- **Scenarios to Flag:**
    - Two Planned jobs on the same boat.
    - An In-Progress job running over into a Planned job's slot.
    - Maintenance windows (if implemented) blocking a Planned job.

---

## 4. The "Eyes": UI/UX Architecture

### A. The Sideways Scrolling Timeline (CSS/HTML)
To achieve a performant, sticky-header timeline without heavy libraries:
- **Container:** `display: grid; grid-template-columns: [Header] 200px [Timeline] 1fr;`
- **Timeline Row:** `display: flex; overflow-x: auto; position: relative;`
- **Time Slots:** Each hour = 100px width.
- **Sticky Headers:** `position: sticky; left: 0; z-index: 10;` for the Tugboat names.

### B. Visual Elements
1.  **The "Now" Line:** A `div` with `position: absolute; width: 2px; background: red;` that calculates its `left` position based on `(CurrentMinute / TotalMinutes) * TotalWidth`.
2.  **Contextual Tooltips:** Hovering over a block shows:
    - Customer & Vessel Name.
    - Port/Terminal.
    - Exact Start/End times.
    - Delay reason (if applicable).

### C. Interaction Flow
- **Click to Act:** Clicking a **Planned (Yellow)** block opens a modal to "Start Dispatch," pre-filling the `DispatchTicket` with the planned data.
- **Visual Warning:** If a conflict is detected, the Planned block's background uses a `repeating-linear-gradient` with Red to draw immediate attention.

---

## 5. SignalR Integration (Real-time Updates)
To ensure the dashboard stays "Alive" without refresh:
- **Hub:** `TugboatHub`.
- **Events:** 
    - `OnDispatchStarted`: Updates a Yellow block to Green.
    - `OnDispatchEnded`: Updates a Green block to Blue.
    - `OnScheduleChanged`: Recalculates conflicts and moves Yellow blocks.

---

## 6. Detailed Implementation Roadmap

### Day 2 - Morning: Foundation
1.  **Models:** Update `JobOrder.cs` and create the Migration.
2.  **DTOs:** Define `TimelineBlockDto` and `FleetStatusDto`.
3.  **Repository:** Add `GetTugboatTimelineDataAsync(DateTime start, DateTime end)` to `IUnitOfWork`.

### Day 2 - Afternoon: Logic & API
4.  **Service:** Build `TugboatMonitoringService` with the Buffer-aware conflict logic.
5.  **Controller:** Implement `TugboatMonitoringController.GetData()` returning JSON for the timeline.

### Day 2 - Evening: UI Development
6.  **View:** Create the `Index.cshtml` with the CSS Grid/Flexbox layout.
7.  **JavaScript:** Build the `TimelineManager.js` to handle:
    - Rendering blocks from JSON.
    - Positioning the "Now" line.
    - Handling sideways scroll sync.

---

## 7. Quality Assurance Checklist
- [ ] **Overlap Test:** Create two Job Orders at the same time for Tugboat A. Confirm both turn Red.
- [ ] **Drift Test:** Set an In-Progress trip to end *after* a Planned trip starts. Confirm the Planned trip turns Red.
- [ ] **Performance Test:** Load 20 Tugboats with 5 jobs each. Ensure horizontal scroll remains smooth.
- [ ] **Mobile Test:** Ensure the timeline is usable on tablets (Critical for on-site supervisors).

**Documented by:** Gemini CLI
**Version:** 1.1 (Detailed Implementation Blueprint)
