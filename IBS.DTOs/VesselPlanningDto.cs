namespace IBS.DTOs
{
    public class VesselBlockDto
    {
        public string Id { get; set; } = null!; // JO-123 or DT-456
        public string VesselName { get; set; } = null!;
        public string? ServiceType { get; set; }
        public int RequiredTugs { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Status { get; set; } = null!; // Planned, In-Progress, Completed
        public bool IsCapacityConflict { get; set; }
        public string? LinkUrl { get; set; }
        public string? CustomerName { get; set; }
        public string? PortTerminal { get; set; }
        public string? Remarks { get; set; }
        public List<string> ConflictingVessels { get; set; } = new();
    }

    public class TerminalTimelineDto
    {
        public int TerminalId { get; set; }
        public string TerminalName { get; set; } = null!;
        public List<VesselBlockDto> Blocks { get; set; } = new();
    }

    public class FleetCapacityDto
    {
        public DateTime Time { get; set; }
        public int TotalTugs { get; set; }
        public int BusyTugs { get; set; }
        public bool IsOverCapacity => BusyTugs > TotalTugs;
    }

    public class VesselPlanningDataDto
    {
        public List<TerminalTimelineDto> Terminals { get; set; } = new();
        public List<FleetCapacityDto> CapacityHeatmap { get; set; } = new();
    }
}
