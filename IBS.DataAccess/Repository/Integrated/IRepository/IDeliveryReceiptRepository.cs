using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Integrated;
using IBS.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Integrated.IRepository
{
    public interface IDeliveryReceiptRepository : IRepository<DeliveryReceipt>
    {
        Task<string> GenerateCodeAsync(string companyClaims, string documentType, CancellationToken cancellationToken = default);

        Task UpdateAsync(DeliveryReceiptViewModel viewModel, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetDeliveryReceiptListAsync(string companyClaims, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetDeliveryReceiptListForSalesInvoice(string companyClaims, int cosId, CancellationToken cancellationToken = default);

        Task PostAsync(DeliveryReceipt deliveryReceipt, CancellationToken cancellationToken = default);

        Task DeductTheVolumeToCos(int cosId, decimal drVolume, CancellationToken cancellationToken = default);

        Task UpdatePreviousAppointedSupplierAsync(DeliveryReceipt model);

        Task AssignNewPurchaseOrderAsync(DeliveryReceipt model);

        Task AutoReversalEntryForInTransit(CancellationToken cancellationToken = default);

        Task<bool> CheckIfManualDrNoExists(string manualDrNo);

        Task RecalculateDeliveryReceipts(int customerOrderSlipId,
            decimal updatedPrice,
            string userName,
            CancellationToken cancellationToken = default);

        Task CreateEntriesForUpdatingCommission(DeliveryReceipt deliveryReceipt,
            decimal difference,
            string userName,
            CancellationToken cancellationToken = default);

        Task CreateEntriesForUpdatingFreight(DeliveryReceipt deliveryReceipt,
            decimal difference,
            string userName,
            CancellationToken cancellationToken = default);
    }
}
