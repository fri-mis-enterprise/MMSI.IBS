using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models
{
    public class AuditTrail(string username, string activity, string documentType, string company)
    {
        public Guid Id { get; set; }

        public string Username { get; set; } = username;

        [Column(TypeName = "timestamp without time zone")]
        public DateTime Date { get; set; } = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila"));

        [Display(Name = "Machine Name")]
        public string MachineName { get; set; } = Environment.MachineName;

        public string Activity { get; set; } = activity;

        [Display(Name = "Document Type")]
        public string DocumentType { get; set; } = documentType;

        public string Company { get; set; } = company;
    }
}
