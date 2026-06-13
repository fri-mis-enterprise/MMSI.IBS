namespace IBS.DTOs
{
    // --- NEW: Fleet Dashboard DTOs ---
    public class VesselPlanningDashboardDto
    {
        public List<PortFleetDto> Ports { get; set; } = new();
        public List<UnassignedJobDto> PendingJobs { get; set; } = new();
    }

    public class PortFleetDto
    {
        public int PortId { get; set; }
        public string PortName { get; set; } = null!;
        public int TotalOwned { get; set; }
        public int ActiveOwned { get; set; }
        public int OutsourcedInUse { get; set; }
        
        public List<TugboatCardDto> OwnedTugboats { get; set; } = new();
        public List<TugboatCardDto> OutsourcedTugboats { get; set; } = new();
    }

    public class TugboatCardDto
    {
        public int TugboatId { get; set; }
        public string TugboatName { get; set; } = null!;
        public string Status { get; set; } = "Idle"; // Idle, Working, Maintenance
        public string? CurrentVessel { get; set; }
        public DateTime? Until { get; set; }
        public string? ProviderName { get; set; }
        public bool IsCompanyOwned { get; set; }
        public int PortId { get; set; }
    }

    public class UnassignedJobDto
    {
        public int JobOrderId { get; set; }
        public string VesselName { get; set; } = null!;
        public string TerminalName { get; set; } = null!;
        public DateTime Start { get; set; }
        public int RequiredTugs { get; set; }
        public int AssignedTugs { get; set; }
        public int PortId { get; set; }
        public string PortName { get; set; } = null!;
        public List<int> AssignedTugboatIds { get; set; } = new();
    }

    // --- LEGACY: Restored for Compatibility (Used by TugboatMonitoring) ---
    public class TugboatTimelineDto
    {
        public int TugboatId { get; set; }
        public string TugboatName { get; set; } = null!;
        public string CurrentPort { get; set; } = "Unknown";
        public List<VesselBlockDto> Blocks { get; set; } = new();
    }

    public class VesselBlockDto
    {
        public string Id { get; set; } = null!; // JO-123 or DT-456
        public string VesselName { get; set; } = null!;
        public string Title { get; set; } = null!;
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Status { get; set; } = null!; // Planned, In-Progress, Completed
        public bool IsConflict { get; set; }
        public string? CustomerName { get; set; }
        public string? PortTerminal { get; set; }
        public string? Remarks { get; set; }
        public string? LinkUrl { get; set; }
    }
}
