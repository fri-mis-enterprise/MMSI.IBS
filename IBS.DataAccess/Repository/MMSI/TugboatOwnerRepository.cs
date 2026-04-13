using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.Models.MMSI.MasterFile;

namespace IBS.DataAccess.Repository.MMSI
{
    public class TugboatOwnerRepository(ApplicationDbContext db): Repository<TugboatOwner>(db), ITugboatOwnerRepository
    {
        private readonly ApplicationDbContext _db = db;
    }
}
