using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI.IRepository;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.MMSI
{
    public class MsapRepository(ApplicationDbContext db): IMsapRepository
    {
        public async Task<List<SelectListItem>> GetMMSIUsersSelectListById(CancellationToken cancellationToken = default)
        {
            var list = await db.Users
                .OrderBy(dt => dt.UserName).Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = $"{s.UserName}"
                }).ToListAsync(cancellationToken);

            return list;
        }
    }
}
