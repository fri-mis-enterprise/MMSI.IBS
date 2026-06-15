using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Enums;
using IBS.Services;
using IBSWeb.Hubs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Services
{
    public class NotificationService(
        IUnitOfWork unitOfWork,
        IHubContext<NotificationHub> hubContext,
        UserManager<ApplicationUser> userManager)
        : INotificationService
    {
        public async Task NotifyByAccessAsync(ProcedureEnum procedure, string message, bool requiresResponse = false, string? targetUrl = null, CancellationToken cancellationToken = default)
        {
            var userIds = await unitOfWork.UserAccess.GetUserIdsWithAccessAsync(procedure, cancellationToken);
            await NotifyMultipleUsersAsync(userIds, message, requiresResponse, targetUrl, cancellationToken);
        }

        public async Task NotifyUserAsync(string userId, string message, bool requiresResponse = false, string? targetUrl = null, CancellationToken cancellationToken = default)
        {
            await unitOfWork.Notifications.AddNotificationAsync(userId, message, requiresResponse, targetUrl);
            
            var user = await userManager.FindByIdAsync(userId);
            if (user?.UserName != null)
            {
                await hubContext.Clients.User(user.UserName).SendAsync("ReceivedNotification", message, targetUrl);
            }
        }

        public async Task NotifyMultipleUsersAsync(List<string> userIds, string message, bool requiresResponse = false, string? targetUrl = null, CancellationToken cancellationToken = default)
        {
            if (userIds == null || !userIds.Any()) return;

            await unitOfWork.Notifications.AddNotificationToMultipleUsersAsync(userIds, message, requiresResponse, targetUrl);

            var usernames = await userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => u.UserName)
                .ToListAsync(cancellationToken);

            foreach (var username in usernames.Where(un => un != null))
            {
                await hubContext.Clients.User(username!).SendAsync("ReceivedNotification", message, targetUrl);
            }
        }
    }
}
