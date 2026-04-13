using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MasterFile.IRepository;
using IBS.Models.MasterFile;

namespace IBS.DataAccess.Repository.MasterFile
{
    public class EmployeeRepository(ApplicationDbContext db): Repository<Employee>(db), IEmployeeRepository
    {
        private readonly ApplicationDbContext _db = db;
    }
}
