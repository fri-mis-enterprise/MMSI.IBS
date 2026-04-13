using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.MasterFile;
using IBS.Models.MMSI.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.MMSI
{
    public class TariffRate
    {
        [Key]
        public int TariffRateId { get; set; }

        public DateOnly AsOfDate { get; set; }

        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }

        public int TerminalId { get; set; }

        [ForeignKey(nameof(TerminalId))]
        public Terminal? Terminal { get; set; }

        public int ServiceId { get; set; }

        [ForeignKey(nameof(ServiceId))]
        public Service? Service { get; set; }

        public decimal Dispatch { get; set; }

        public decimal BAF { get; set; }

        public string? CreatedBy { get; set; }

        [Column(TypeName = "timestamp without time zone")]
        public DateTime? CreatedDate { get; set; }

        public string? UpdateBy { get; set; }

        [Column(TypeName = "timestamp without time zone")]
        public DateTime? UpdateDate { get; set; }

        public decimal DispatchDiscount { get; set; }

        public decimal BAFDiscount { get; set; }

        #region -- Select Lists --

        [NotMapped]
        public List<SelectListItem>? Customers { get; set; }

        [NotMapped]
        public List<SelectListItem>? Ports { get; set; }

        [NotMapped]
        public List<SelectListItem>? Services { get; set; }

        [NotMapped]
        public List<SelectListItem>? Terminals { get; set; }

        #endregion
    }
}
