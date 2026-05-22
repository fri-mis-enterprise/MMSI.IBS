using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Msap.IRepository;
using IBS.Models.MSAP.MasterFile;

namespace IBS.DataAccess.Repository.Msap
{
    public class TugboatOwnerRepository(ApplicationDbContext db): Repository<TugboatOwner>(db), ITugboatOwnerRepository
    {
        private readonly ApplicationDbContext _db = db;
    }
}



