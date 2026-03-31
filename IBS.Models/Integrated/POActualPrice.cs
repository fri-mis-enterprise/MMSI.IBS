using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Integrated
{
    public class POActualPrice
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int PurchaseOrderId { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal TriggeredVolume { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal AppliedVolume { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal TriggeredPrice { get; set; }

        public bool IsApproved { get; set; }

        [StringLength(200)]
        public string? ApprovedBy { get; set; }

        [Column(TypeName = "timestamp without time zone")]
        public DateTime? ApprovedDate { get; set; }

        [Column(TypeName = "timestamp without time zone")]
        public DateTime TriggeredDate { get; set; } = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila"));
    }
}
