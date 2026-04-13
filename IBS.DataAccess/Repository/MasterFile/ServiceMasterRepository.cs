using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MasterFile.IRepository;
using IBS.Models.MasterFile;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.MasterFile
{
    public class ServiceMasterRepository(ApplicationDbContext db)
        : Repository<ServiceMaster>(db), IServiceMasterRepository
    {
        private readonly ApplicationDbContext _db = db;

        public async Task<string> GetLastNumber(CancellationToken cancellationToken = default)
        {
            var lastNumber = await _db
                .Services
                .OrderByDescending(s => s.ServiceId)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastNumber == null || !int.TryParse(lastNumber.ServiceNo, out var serviceNo))
            {
                return "2001";
            }

            return (serviceNo + 1).ToString();

        }

        public async Task<bool> IsServicesExist(string serviceName, string company, CancellationToken cancellationToken = default)
        {
            return await _db.Services
                .AnyAsync(c => c.Company == company && c.Name == serviceName, cancellationToken);
        }
    }
}
