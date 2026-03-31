using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.MasterFile;

namespace IBS.Models.Integrated
{
    public class COSAppointedSupplier
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SequenceId { get; set; }

        public int CustomerOrderSlipId { get; set; }

        [ForeignKey(nameof(CustomerOrderSlipId))]
        public CustomerOrderSlip? CustomerOrderSlip { get; set; }

        public int PurchaseOrderId { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal UnservedQuantity { get; set; }

        public bool IsAssignedToDR { get; set; }

        public int SupplierId { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public Supplier? Supplier { get; set; }

        [StringLength(20)]
        public string? AtlNo { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal UnreservedQuantity { get; set; }

    }
}
