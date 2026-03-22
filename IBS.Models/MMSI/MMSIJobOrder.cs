using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.MasterFile;
using IBS.Models.MMSI.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.MMSI
{
    public class MMSIJobOrder : BaseEntity
    {
        [Key]
        public int JobOrderId { get; set; }

        [Required]
        [Column(TypeName = "varchar(20)")]
        public string JobOrderNumber { get; set; }

        [Required]
        public DateOnly Date { get; set; }

        [Required]
        [Column(TypeName = "varchar(20)")]
        public string Status { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? COSNumber { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string? VoyageNumber { get; set; }

        public string? Remarks { get; set; }

        #region ---Foreign Keys---

        public int CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }

        public int VesselId { get; set; }
        [ForeignKey(nameof(VesselId))]
        public MMSIVessel? Vessel { get; set; }

        public int? PortId { get; set; }
        [ForeignKey(nameof(PortId))]
        public MMSIPort? Port { get; set; }

        public int? TerminalId { get; set; }
        [ForeignKey(nameof(TerminalId))]
        public MMSITerminal? Terminal { get; set; }

        #endregion

        public virtual ICollection<MMSIDispatchTicket> DispatchTickets { get; set; } = new List<MMSIDispatchTicket>();

        #region ---Select Lists (Not Mapped)---

        [NotMapped]
        public List<SelectListItem>? Customers { get; set; }

        [NotMapped]
        public List<SelectListItem>? Vessels { get; set; }

        [NotMapped]
        public List<SelectListItem>? Ports { get; set; }

        [NotMapped]
        public List<SelectListItem>? Terminals { get; set; }

        #endregion
    }
}
