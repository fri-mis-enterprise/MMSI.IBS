namespace IBS.Models.ViewModels
{
    public class DispatchReportViewModel
    {
        public string ReportType { get; set; } = null!;

        public DateOnly DateFrom { get; set; }

        public DateOnly DateTo { get; set; }
    }
}
