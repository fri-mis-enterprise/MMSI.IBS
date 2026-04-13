using IBS.DataAccess.Data;
using IBS.DTOs;
using IBS.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace IBS.Services
{
    public interface ISubAccountResolver
    {
        Task<SubAccountInfoDto?> ResolveAsync(SubAccountType type,
            int subAccountId,
            CancellationToken cancellationToken = default);
        Task<bool> ValidateExistsAsync(SubAccountType type,
            int subAccountId,
            CancellationToken cancellationToken = default);
    }

    public class SubAccountResolver(ApplicationDbContext context): ISubAccountResolver
    {
        public async Task<SubAccountInfoDto?> ResolveAsync(SubAccountType type,
            int subAccountId,
            CancellationToken cancellationToken = default)
        {
            switch (type)
            {
                case SubAccountType.Customer:
                    var customer = await context.Customers
                        .Where(c => c.CustomerId == subAccountId)
                        .Select(c => new SubAccountInfoDto
                        {
                            Type = SubAccountType.Customer,
                            Id = c.CustomerId,
                            Name = c.CustomerName
                        })
                        .FirstOrDefaultAsync(cancellationToken);
                    return customer;

                case SubAccountType.Supplier:
                    var supplier = await context.Suppliers
                        .Where(s => s.SupplierId == subAccountId)
                        .Select(s => new SubAccountInfoDto
                        {
                            Type = SubAccountType.Supplier,
                            Id = s.SupplierId,
                            Name = s.SupplierName
                        })
                        .FirstOrDefaultAsync(cancellationToken);
                    return supplier;

                case SubAccountType.Employee:
                    var employee = await context.Employees
                        .Where(e => e.EmployeeId == subAccountId)
                        .Select(e => new SubAccountInfoDto
                        {
                            Type = SubAccountType.Employee,
                            Id = e.EmployeeId,
                            Name = $"{e.FirstName} {e.LastName}"
                        })
                        .FirstOrDefaultAsync(cancellationToken);
                    return employee;

                case SubAccountType.BankAccount:
                    var bank = await context.BankAccounts
                        .Where(b => b.BankAccountId == subAccountId)
                        .Select(b => new SubAccountInfoDto
                        {
                            Type = SubAccountType.BankAccount,
                            Id = b.BankAccountId,
                            Name = $"{b.AccountNo} {b.AccountName}"
                        })
                        .FirstOrDefaultAsync(cancellationToken);
                    return bank;

                case SubAccountType.Company:
                    var company = await context.Companies
                        .Where(c => c.CompanyId == subAccountId)
                        .Select(c => new SubAccountInfoDto
                        {
                            Type = SubAccountType.Company,
                            Id = c.CompanyId,
                            Name = c.CompanyName
                        })
                        .FirstOrDefaultAsync(cancellationToken);
                    return company;

                default:
                    return null;
            }
        }

        public async Task<bool> ValidateExistsAsync(SubAccountType type,
            int subAccountId,
            CancellationToken cancellationToken = default)
        {
            return type switch
            {
                SubAccountType.Customer => await context.Customers
                    .AnyAsync(c => c.CustomerId == subAccountId, cancellationToken),
                SubAccountType.Supplier => await context.Suppliers
                    .AnyAsync(s => s.SupplierId == subAccountId, cancellationToken),
                SubAccountType.Employee => await context.Employees
                    .AnyAsync(e => e.EmployeeId == subAccountId, cancellationToken),
                SubAccountType.BankAccount => await context.BankAccounts
                    .AnyAsync(b => b.BankAccountId == subAccountId, cancellationToken),
                SubAccountType.Company => await context.Companies
                    .AnyAsync(c => c.CompanyId == subAccountId, cancellationToken),
                _ => false
            };
        }
    }
}
