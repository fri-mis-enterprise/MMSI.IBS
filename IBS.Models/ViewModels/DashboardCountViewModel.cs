namespace IBS.Models.ViewModels
{
    public class DashboardCountViewModel
    {
        public int SupplierAppointmentCount { get; set; }
        public int HaulerAppointmentCount { get; set; }
        public int ATLBookingCount { get; set; }
        public int OMApprovalCOSCount { get; set; }
        public int OMApprovalDRCount { get; set; }
        public int OMApprovalPOCount { get; set; }
        public int CNCApprovalCount { get; set; }
        public int FMApprovalCount { get; set; }
        public int DRCount { get; set; }
        public int InTransitCount { get; set; }
        public int ForInvoiceCount { get; set; }
        public int RecordLiftingDateCount { get; set; }
        public int RecordSupplierDetails { get; set; }
        public int MsapServiceRequestForPosting { get; set; }
        public int MsapDispatchTicketForTariff { get; set; }
        public int MsapDispatchTicketForApproval { get; set; }
        public int MsapDispatchTicketForBilling { get; set; }
        public int MsapBillingForCollection { get; set; }
    }
}
