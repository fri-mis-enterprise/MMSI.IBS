using IBS.Models.MSAP;

namespace IBS.DataAccess.Repository.Msap.IRepository
{
    public interface IReportRepository
    {
        Task<List<DispatchTicket>> GetSalesReport(DateOnly DateFrom, DateOnly DateTo, CancellationToken cancellationToken = default);
    }
}



