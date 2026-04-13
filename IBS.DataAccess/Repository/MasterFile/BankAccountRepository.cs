using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MasterFile.IRepository;
using IBS.Models.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.MasterFile
{
    public class BankAccountRepository(ApplicationDbContext db): Repository<BankAccount>(db), IBankAccountRepository
    {
        private readonly ApplicationDbContext _db = db;

        public async Task<List<SelectListItem>> GetBankAccountListAsync(string company, CancellationToken cancellationToken = default)
        {
            return await _db.BankAccounts
                 .Where(a => a.IsFilpride)
                 .Select(ba => new SelectListItem
                 {
                     Value = ba.BankAccountId.ToString(),
                     Text = ba.AccountName
                 })
                 .ToListAsync(cancellationToken);
        }

        public async Task<bool> IsBankAccountNameExist(string accountName, CancellationToken cancellationToken = default)
        {
            return await _db.BankAccounts
                .AnyAsync(b => b.AccountName == accountName, cancellationToken);
        }

        public async Task<bool> IsBankAccountNoExist(string accountNo, CancellationToken cancellationToken = default)
        {
            return await _db.BankAccounts
                .AnyAsync(b => b.AccountNo == accountNo, cancellationToken);
        }
    }
}
