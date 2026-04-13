using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MasterFile.IRepository;
using IBS.Models.Enums;
using IBS.Models.MasterFile;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace IBS.DataAccess.Repository.MasterFile
{
    public class CustomerRepository(ApplicationDbContext db): Repository<Customer>(db), ICustomerRepository
    {
        private readonly ApplicationDbContext _db = db;

        public async Task<string> GenerateCodeAsync(string customerType, CancellationToken cancellationToken = default)
        {
            var lastCustomer = await _db
                .Customers
                .OrderByDescending(c => c.CustomerId)
                .FirstOrDefaultAsync(c => c.CustomerType == customerType, cancellationToken);

            if (lastCustomer == null)
            {
                return customerType switch
                {
                    nameof(CustomerType.Retail) => "RET0001",
                    nameof(CustomerType.Industrial) => "IND0001",
                    nameof(CustomerType.Reseller) => "RES0001",
                    _ => "GOV0001"
                };
            }

            var lastCode = lastCustomer.CustomerCode!;
            var numericPart = lastCode.Substring(3);

            // Parse the numeric part and increment it by one
            var incrementedNumber = int.Parse(numericPart) + 1;

            // Format the incremented number with leading zeros and concatenate with the letter part
            return lastCode.Substring(0, 3) + incrementedNumber.ToString("D4");
        }

        public async Task<bool> IsTinNoExistAsync(string tin, string company, CancellationToken cancellationToken = default)
        {
            if (tin == "000-000-000-00000")
            {
                return false;
            }

            return await _db.Customers
                .AnyAsync(c =>
                    c.Company == company &&
                    c.CustomerTin == tin,
                    cancellationToken);
        }

        public async Task UpdateAsync(Customer model, CancellationToken cancellationToken = default)
        {
            var existingCustomer = await _db.Customers
                .FirstOrDefaultAsync(x => x.CustomerId == model.CustomerId, cancellationToken)
                                   ?? throw new InvalidOperationException($"Customer with id '{model.CustomerId}' not found.");

            existingCustomer.CustomerName = model.CustomerName;
            existingCustomer.CustomerAddress = model.CustomerAddress;
            existingCustomer.CustomerTin = model.CustomerTin;
            existingCustomer.BusinessStyle = model.BusinessStyle;
            existingCustomer.CustomerTerms = model.CustomerTerms;
            existingCustomer.CustomerType = model.CustomerType;
            existingCustomer.WithHoldingVat = model.WithHoldingVat;
            existingCustomer.WithHoldingTax = model.WithHoldingTax;
            existingCustomer.ClusterCode = model.ClusterCode;
            existingCustomer.CreditLimit = model.CreditLimit;
            existingCustomer.CreditLimitAsOfToday = model.CreditLimitAsOfToday;
            existingCustomer.ZipCode = model.ZipCode;
            existingCustomer.RetentionRate = model.RetentionRate;
            existingCustomer.IsFilpride = model.IsFilpride;
            existingCustomer.IsMMSI = model.IsMMSI;
            existingCustomer.VatType = model.VatType;
            existingCustomer.Type = model.Type;
            existingCustomer.RequiresPriceAdjustment = model.RequiresPriceAdjustment;
            existingCustomer.StationCode = model.StationCode;
            existingCustomer.CommissionRate = model.CommissionRate;
            existingCustomer.CommissioneeId = model.CommissioneeId;

            if (_db.ChangeTracker.HasChanges())
            {
                existingCustomer.EditedBy = model.EditedBy;
                existingCustomer.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("No data changes!");
            }
        }

        public async Task<List<SelectListItem>> GetCustomerBranchesSelectListAsync(int customerId, CancellationToken cancellationToken = default)
        {
            return await _db.CustomerBranches
                .OrderBy(c => c.BranchName)
                .Where(c => c.CustomerId == customerId)
                .Select(b => new SelectListItem
                {
                    Value = b.BranchName,
                    Text = b.BranchName
                })
                .ToListAsync(cancellationToken);
        }

        public override async Task<Customer?> GetAsync(Expression<Func<Customer, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                    .Include(c => c.Commissionee)
                    .FirstOrDefaultAsync(cancellationToken);
        }

        public override async Task<IEnumerable<Customer>> GetAllAsync(Expression<Func<Customer, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<Customer> query = dbSet
                .Include(dr => dr.Commissionee);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }
    }
}
