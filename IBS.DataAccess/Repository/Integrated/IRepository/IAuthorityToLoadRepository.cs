using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Integrated;

namespace IBS.DataAccess.Repository.Integrated.IRepository
{
    public interface IAuthorityToLoadRepository : IRepository<AuthorityToLoad>
    {
        Task<string> GenerateAtlNo(string company, CancellationToken cancellationToken);
    }
}
