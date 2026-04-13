using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models
{
    public class PurchaseLockedRecordsQueue
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column(TypeName = "date")]
        public DateOnly LockedDate { get; set; }

        public int ReceivingReportId { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal Price { get; set; }

    }
}
