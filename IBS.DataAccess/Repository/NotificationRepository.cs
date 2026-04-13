using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Utility.Helpers;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository
{
    public class NotificationRepository(ApplicationDbContext db): INotificationRepository
    {
        public async Task AddNotificationAsync(string userId, string message, bool requiresResponse = false)
        {
            var notification = new Notification
            {
                Message = message,
                CreatedDate = DateTimeHelper.GetCurrentPhilippineTime()
            };

            await db.Notifications.AddAsync(notification);
            await db.SaveChangesAsync();

            var userNotification = new UserNotification
            {
                UserId = userId,
                NotificationId = notification.NotificationId,
                IsRead = false,
                RequiresResponse = requiresResponse
            };

            await db.UserNotifications.AddAsync(userNotification);
            await db.SaveChangesAsync();
        }

        public async Task AddNotificationToMultipleUsersAsync(List<string> userIds, string message, bool requiresResponse = false)
        {
            var notification = new Notification
            {
                Message = message,
                CreatedDate = DateTimeHelper.GetCurrentPhilippineTime()
            };

            await db.Notifications.AddAsync(notification);
            await db.SaveChangesAsync();

            var userNotifications = userIds.Select(userId => new UserNotification
            {
                UserId = userId,
                NotificationId = notification.NotificationId,
                IsRead = false,
                RequiresResponse = requiresResponse
            }).ToList();

            await db.UserNotifications.AddRangeAsync(userNotifications);
            await db.SaveChangesAsync();
        }

        public async Task ArchiveAsync(Guid userNotificationId)
        {
            var userNotification = await db.UserNotifications.FindAsync(userNotificationId);

            if (userNotification != null)
            {
                userNotification.IsArchived = true;
                await db.SaveChangesAsync();
            }
        }

        public async Task<int> GetUnreadNotificationCountAsync(string userId)
        {
            return await db.UserNotifications.CountAsync(n => n.UserId == userId && !n.IsRead && !n.IsArchived);
        }

        public async Task<List<UserNotification>> GetUserNotificationsAsync(string userId)
        {
            return await db.UserNotifications
                .Include(un => un.Notification)
                .Where(un => un.UserId == userId && !un.IsArchived)
                .OrderByDescending(un => un.Notification.CreatedDate)
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(Guid userNotificationId)
        {
            var userNotification = await db.UserNotifications.FindAsync(userNotificationId);
            if (userNotification != null)
            {
                userNotification.IsRead = true;
                await db.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(string userId, CancellationToken cancellation = default)
        {
            await db.UserNotifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(n => n.IsRead, true), cancellation);
        }

        public async Task ArchiveAllAsync(string userId, CancellationToken cancellation = default)
        {
            await db.UserNotifications
                .Where(n => n.UserId == userId && !n.IsArchived)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(n => n.IsArchived, true), cancellation);
        }
    }
}
