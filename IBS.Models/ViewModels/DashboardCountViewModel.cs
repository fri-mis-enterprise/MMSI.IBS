namespace IBS.Models.ViewModels
{
    public class DashboardCountViewModel
    {
        public int MsapServiceRequestForPosting { get; set; }
        public int MsapDispatchTicketForTariff { get; set; }
        public int MsapDispatchTicketForApproval { get; set; }
        public int MsapDispatchTicketForBilling { get; set; }
        public int MsapBillingForCollection { get; set; }
    }
}
