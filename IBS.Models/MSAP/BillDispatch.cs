using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.MMSI
{
    public class BillDispatch
    {
        [Key]
        public int BillDispatchId { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string BillingNumber { get; set; } = null!;

        [Column(TypeName = "varchar(20)")]
        public string DispatchNumber { get; set; } = null!;

        [Column(TypeName = "numeric(18,4)")]
        public decimal Rate { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal ApOtherTug { get; set; }

        public int BillingId { get; set; }

        [ForeignKey(nameof(BillingId))]
        public Billing Billing { get; set; } = null!;

        public int DispatchTicketId { get; set; }

        [ForeignKey(nameof(DispatchTicketId))]
        public DispatchTicket DispatchTicket { get; set; } = null!;
    }
}
