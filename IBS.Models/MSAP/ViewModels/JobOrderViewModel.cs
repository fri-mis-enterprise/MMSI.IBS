using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace IBS.Models.MMSI.ViewModels
{
    public class JobOrderViewModel
    {
        public int JobOrderId { get; set; }
        
        public string? JobOrderNumber { get; set; }
        
        [Required]
        public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Now);
        
        public string? Status { get; set; }

        [Required]
        [Display(Name = "Customer")]
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        
        [Required]
        [Display(Name = "Vessel")]
        public int VesselId { get; set; }
        public string? VesselName { get; set; }
        
        [Display(Name = "Port")]
        public int? PortId { get; set; }
        public string? PortName { get; set; }

        [Display(Name = "Terminal")]
        public int? TerminalId { get; set; }
        public string? TerminalName { get; set; }
        
        [Display(Name = "COS Number")]
        public string? COSNumber { get; set; }
        
        [Display(Name = "Voyage Number")]
        public string? VoyageNumber { get; set; }
        
        public string? Remarks { get; set; }

        public List<SelectListItem>? Customers { get; set; }
        public List<SelectListItem>? Vessels { get; set; }
        public List<SelectListItem>? Ports { get; set; }
        public List<SelectListItem>? Terminals { get; set; }
        
        public List<DispatchTicket> DispatchTickets { get; set; } = new();
    }
}
