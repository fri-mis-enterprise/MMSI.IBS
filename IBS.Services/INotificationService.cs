using IBS.Models.Enums;

namespace IBS.Services
{
    public interface INotificationService
    {
        Task NotifyByAccessAsync(ProcedureEnum procedure, string message, bool requiresResponse = false, string? targetUrl = null, CancellationToken cancellationToken = default);
        
        Task NotifyUserAsync(string userId, string message, bool requiresResponse = false, string? targetUrl = null, CancellationToken cancellationToken = default);

        Task NotifyMultipleUsersAsync(List<string> userIds, string message, bool requiresResponse = false, string? targetUrl = null, CancellationToken cancellationToken = default);
    }
}
