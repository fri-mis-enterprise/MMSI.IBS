using IBS.Models.Integrated;

namespace IBS.Models.ViewModels
{
    public class DrWithAmountPaidViewModel
    {
        public DeliveryReceipt DeliveryReceipt { get; set; } = null!;
        public decimal AmountPaid { get; set; }
    }

    public record MonthYear(int Year, int Month);
}
