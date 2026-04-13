using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;

namespace IBS.DataAccess.Repository
{
    public class AuditTrailRepository(ApplicationDbContext db): Repository<AuditTrail>(db), IAuditTrailRepository
    {
        private readonly ApplicationDbContext _db = db;
    }
}
