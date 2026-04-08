using IBS.Models.Books;
using IBS.Models.MMSI;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.MMSI.IRepository
{
    public interface IReportRepository
    {
        Task<List<DispatchTicket>> GetSalesReport(DateOnly DateFrom, DateOnly DateTo, CancellationToken cancellationToken = default);
    }
}
