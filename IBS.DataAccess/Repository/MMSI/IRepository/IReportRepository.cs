using IBS.Models.MMSI;

namespace IBS.DataAccess.Repository.MMSI.IRepository
{
    public interface IReportRepository
    {
        Task<List<DispatchTicket>> GetSalesReport(DateOnly DateFrom, DateOnly DateTo, CancellationToken cancellationToken = default);
    }
}
