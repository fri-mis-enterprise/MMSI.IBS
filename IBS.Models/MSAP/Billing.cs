using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.MasterFile;
using IBS.Models.MMSI.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.MMSI
{
    public class Billing : BaseEntity
    {
        [Key]
        public int MMSIBillingId { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string MMSIBillingNumber
        {
            get => _billingNumber;
            set => _billingNumber = value.Trim();
        }

        private string _billingNumber = null!;

        public DateOnly Date { get; set; }

        public string Status { get; set; } = null!;

        public bool IsUndocumented { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string BilledTo { get; set; } = null!;

        public string? VoyageNumber
        {
            get => _voyageNumber;
            set => _voyageNumber = value?.Trim();
        }

        private string? _voyageNumber;

        [Column(TypeName = "numeric(18,4)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal AmountPaid { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal Balance { get; set; }

        public bool IsPaid { get; set; }

        public decimal DispatchAmount { get; set; }

        public decimal BAFAmount { get; set; }

        public bool IsPrincipal { get; set; }

        public int? CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }

        public int? PrincipalId { get; set; }
        [ForeignKey(nameof(PrincipalId))]
        public Principal? Principal { get; set; }

        public int? VesselId { get; set; }
        [ForeignKey(nameof(VesselId))]
        public Vessel? Vessel { get; set; }

        public int? PortId { get; set; }
        [ForeignKey(nameof(PortId))]
        public Port? Port { get; set; }

        public int? TerminalId { get; set; }
        [ForeignKey(nameof(TerminalId))]
        public Terminal? Terminal { get; set; }

        public decimal ApOtherTug { get; set; }

        public bool IsVatable { get; set; }

        public bool IsPrinted { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal Discount { get; set; }

        [Column(TypeName = "date")]
        public DateOnly DueDate { get; set; }

        [StringLength(15)]
        public string? Terms { get; set; }

        [StringLength(20)]
        public string Company { get; set; } = string.Empty;

        #region ---Address Lines---

        [NotMapped]
        public string? AddressLine1 { get; set; }

        [NotMapped]
        public string? AddressLine2 { get; set; }

        [NotMapped]
        public string? AddressLine3 { get; set; }

        [NotMapped]
        public string? AddressLine4 { get; set; }

        [NotMapped]
        public List<string>? UniqueTugboats { get; set; }

        #endregion

        #region ---Select Lists---

        [NotMapped]
        public List<SelectListItem>? Customers { get; set; }

        [NotMapped]
        public List<SelectListItem>? Vessels { get; set; }

        [NotMapped]
        public List<SelectListItem>? Ports { get; set; }

        [NotMapped]
        public List<SelectListItem>? Terminals { get; set; }

        [NotMapped]
        public List<SelectListItem>? UnbilledDispatchTickets { get; set; }

        [NotMapped]
        public List<string>? ToBillDispatchTickets { get; set; }

        [NotMapped]
        public List<DispatchTicket>? PaidDispatchTickets { get; set; }

        public int? CollectionId { get; set; }

        [NotMapped]
        public Collection? Collection { get; set; }

        public string? CollectionNumber { get; set; }

        [NotMapped]
        public List<SelectListItem>? CustomerPrincipal { get; set; }

        #endregion ---Select Lists---
    }
}
