using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace IBS.Services
{
    public class StartOfTheMonthService(
        IUnitOfWork unitOfWork,
        ILogger<StartOfTheMonthService> logger,
        ApplicationDbContext dbContext)
        : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            try
            {
                var today = DateTimeHelper.GetCurrentPhilippineTime();
                var previousMonthDate =  today.AddMonths(-1);

                // This method will capture the unlifted DR, send the notification to TNS if found any.
                await GetTheUnliftedDrs(previousMonthDate);

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task GetTheUnliftedDrs(DateTime previousMonthDate)
        {
            try
            {
                var hasUnliftedDrs = await dbContext.DeliveryReceipts
                    .AnyAsync(x => x.Date.Month == previousMonthDate.Month
                                   && x.Date.Year == previousMonthDate.Year
                                   && !x.HasReceivingReport);

                if (hasUnliftedDrs)
                {
                    var users = await dbContext.ApplicationUsers
                        .Where(u => u.Department == SD.Department_TradeAndSupply
                                    || u.Department == SD.Department_ManagementAccounting)
                        .Select(u => u.Id)
                        .ToListAsync();

                    var message = $"There are still unlifted reports for {previousMonthDate:MMM yyyy}. " +
                                  $"Please ensure the lifting dates for the remaining DRs are recorded to avoid issues during the month-end closing. " +
                                  $"CC: Management Accounting";

                    await unitOfWork.Notifications.AddNotificationToMultipleUsersAsync(users, message);

                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An error occurred while getting the unlifted DRs.", ex);
            }
        }
    }
}
