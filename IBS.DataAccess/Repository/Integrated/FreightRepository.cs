using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Integrated.IRepository;
using IBS.Models;

namespace IBS.DataAccess.Repository.Integrated
{
    public class FreightRepository(ApplicationDbContext db): Repository<Freight>(db), IFreightRepository
    {
        private readonly ApplicationDbContext _db = db;
    }
}
