using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.MMSI.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.MMSI.ViewModels
{
    public class ServiceRequestViewModel
    {
        public int? JobOrderId { get; set; }

        public int? DispatchTicketId { get; set; }

        public DateOnly? Date { get; set; }

        [StringLength(10, ErrorMessage = "Dispatch Number can only contain 10 characters")]
        public string? COSNumber { get; set; }

        [StringLength(20, ErrorMessage = "Dispatch Number should not exceed 20 characters")]
        public string DispatchNumber { get; set; } = null!;

        [StringLength(100)]
        public string? VoyageNumber { get; set; }

        public int CustomerId { get; set; }

        public DateOnly? DateLeft { get; set; }

        public TimeOnly? TimeLeft { get; set; }

        public DateOnly? DateArrived { get; set; }

        public TimeOnly? TimeArrived { get; set; }

        public int TerminalId { get; set; }

        [ForeignKey(nameof(TerminalId))]
        public Terminal? Terminal { get; set; }

        public int PortId { get; set; }

        public int ServiceId { get; set; }

        public int TugBoatId { get; set; }

        public int? TugMasterId { get; set; }

        public int VesselId { get; set; }

        public string? ImageName { get; set; }

        public string? VideoName { get; set; }

        public string? ImageSignedUrl { get; set; }

        public string? VideoSignedUrl { get; set; }

        [StringLength(100)]
        public string? Remarks { get; set; }

        #region ---Select Lists---

        [NotMapped]
        public List<SelectListItem>? Tugboats { get; set; }

        [NotMapped]
        public List<SelectListItem>? TugMasters { get; set; }

        [NotMapped]
        public List<SelectListItem>? Ports { get; set; }

        [NotMapped]
        public List<SelectListItem>? Terminals { get; set; }

        [NotMapped]
        public List<SelectListItem>? Vessels { get; set; }

        [NotMapped]
        public List<SelectListItem>? Services { get; set; }

        [NotMapped]
        public List<SelectListItem>? Customers { get; set; }

        #endregion ---Select Lists---

        public DispatchTicket ToEntity()
        {
            return new DispatchTicket
            {
                DispatchTicketId = DispatchTicketId ?? 0,
                Date = Date,
                COSNumber = COSNumber,
                DispatchNumber = DispatchNumber,
                VoyageNumber = VoyageNumber,
                CustomerId = CustomerId,
                DateLeft = DateLeft,
                TimeLeft = TimeLeft,
                DateArrived = DateArrived,
                TimeArrived = TimeArrived,
                TerminalId = TerminalId,
                ServiceId = ServiceId,
                TugBoatId = TugBoatId,
                TugMasterId = TugMasterId,
                VesselId = VesselId,
                PortId = PortId,
                Remarks = Remarks,
                JobOrderId = JobOrderId
            };
        }

        public void FromEntity(DispatchTicket entity)
        {
            Date = entity.Date;
            COSNumber = entity.COSNumber;
            DispatchNumber = entity.DispatchNumber;
            VoyageNumber = entity.VoyageNumber;
            CustomerId = entity.CustomerId;
            DateLeft = entity.DateLeft;
            TimeLeft = entity.TimeLeft;
            DateArrived = entity.DateArrived;
            TimeArrived = entity.TimeArrived;
            TerminalId = entity.TerminalId;
            ServiceId = entity.ServiceId;
            TugBoatId = entity.TugBoatId;
            TugMasterId = entity.TugMasterId;
            VesselId = entity.VesselId;
            Terminal = entity.Terminal;
            PortId = entity.PortId;
            Remarks = entity.Remarks;
            ImageName = entity.ImageName;
            VideoName = entity.VideoName;
            DispatchTicketId = entity.DispatchTicketId;
            JobOrderId = entity.JobOrderId;
        }
    }
}
