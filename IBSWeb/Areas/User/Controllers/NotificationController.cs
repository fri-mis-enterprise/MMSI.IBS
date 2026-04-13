using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Utility.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class NotificationController(
        IUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        ILogger<NotificationController> logger)
        : Controller
    {
        public async Task<IActionResult> Index()
        {
            var notifications = await unitOfWork.Notifications.GetUserNotificationsAsync(userManager.GetUserId(User)!);
            return View(notifications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(Guid userNotificationId)
        {
            await unitOfWork.Notifications.MarkAsReadAsync(userNotificationId);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetNotificationCount()
        {
            var userId = userManager.GetUserId(User);

            if (userId == null)
            {
                return Json(0);
            }

            var count = await unitOfWork.Notifications.GetUnreadNotificationCountAsync(userId);

            return Json(count);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(Guid userNotificationId)
        {
            await unitOfWork.Notifications.ArchiveAsync(userNotificationId);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RespondToNotification(Guid userNotificationId, string response)
        {
            if (string.IsNullOrWhiteSpace(response))
            {
                return BadRequest("Response cannot be null or empty.");
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            try
            {
                if (response.Equals("yes", StringComparison.OrdinalIgnoreCase))
                {
                    var userNotification = await dbContext.UserNotifications.FindAsync(userNotificationId);

                    if (userNotification == null)
                    {
                        return NotFound($"Notification with ID {userNotificationId} not found.");
                    }

                    var relatedUserNotifications = await dbContext.UserNotifications
                        .Where(un => un.NotificationId == userNotification.NotificationId)
                        .ToListAsync();

                    foreach (var notification in relatedUserNotifications)
                    {
                        notification.RequiresResponse = false;
                        notification.IsRead = true;
                    }

                    var lockDrAppSetting = await dbContext.AppSettings
                        .FirstOrDefaultAsync(a => a.SettingKey == AppSettingKey.LockTheCreationOfDr);

                    if (lockDrAppSetting != null)
                    {
                        lockDrAppSetting.Value = "false";
                    }

                    await dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                else
                {
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogError(ex, "An error occurred while responding to notification.");
                TempData["error"] = "An error occurred while processing your request.";
                return RedirectToAction(nameof(Index));
            }

            TempData["success"] = "Notification response processed successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellation)
        {
            var userId = userManager.GetUserId(User);

            if (userId == null)
            {
                return BadRequest();
            }

            await unitOfWork.Notifications.MarkAllAsReadAsync(userId, cancellation);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveAll(CancellationToken cancellation)
        {
            var userId = userManager.GetUserId(User);

            if (userId == null)
            {
                return BadRequest();
            }

            await unitOfWork.Notifications.ArchiveAllAsync(userId, cancellation);
            return RedirectToAction(nameof(Index));
        }
    }
}
