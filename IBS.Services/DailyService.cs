using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Enums;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace IBS.Services
{
    public class DailyService(
        ApplicationDbContext dbContext,
        ILogger<DailyService> logger,
        UserManager<ApplicationUser> userManager,
        IUnitOfWork unitOfWork)
        : IJob
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task Execute(IJobExecutionContext context)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            try
            {
                var today = DateOnly.FromDateTime(DateTimeHelper.GetCurrentPhilippineTime());

                await CosExpiration(today);

                await transaction.CommitAsync();

            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogError(ex, ex.Message);
            }
        }

        private async Task CosExpiration(DateOnly today)
        {
            var cosList = await dbContext.CustomerOrderSlips
                .Where(cos => cos.ExpirationDate <= today
                              && cos.Status != nameof(CosStatus.Completed)
                              && cos.Status != nameof(CosStatus.Expired)
                              && cos.Status != nameof(CosStatus.Closed)
                              && cos.Status != nameof(CosStatus.Disapproved))
                .ToListAsync();

            if (cosList.Count == 0)
            {
                return;
            }

            foreach (var cos in cosList)
            {
                // Record the current status before updating
                var previousStatus = cos.Status;

                // Update the status to Expired
                cos.Status = nameof(CosStatus.Expired);

                // Append the previous status and timestamp to the remarks
                cos.Remarks = $"Previous status: [{previousStatus}] updated to Expired on {today}. {cos.Remarks}";
            }

            await dbContext.SaveChangesAsync();
        }
    }
}
