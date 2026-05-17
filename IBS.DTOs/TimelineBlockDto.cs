namespace IBS.DTOs
{
    public class TimelineBlockDto
    {
        public string Id { get; set; } = null!; // JO-123 or DT-456
        public string Title { get; set; } = null!; // Vessel Name / Service
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Status { get; set; } = null!; // Planned, InProgress, Completed
        public bool IsConflict { get; set; }
        public string? LinkUrl { get; set; } // Link to JO Details or DT Edit
        public string? CustomerName { get; set; }
        public string? PortTerminal { get; set; }
        public string? Remarks { get; set; }
    }

    public class TugboatTimelineDto
    {
        public int TugboatId { get; set; }
        public string TugboatName { get; set; } = null!;
        public List<TimelineBlockDto> Blocks { get; set; } = new();
    }
}
