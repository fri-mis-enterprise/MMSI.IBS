// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IBSWeb.Areas.Identity.Pages.Account
{
    public class LogoutModel(
        SignInManager<ApplicationUser> signInManager,
        ILogger<LogoutModel> logger,
        IHubConnectionRepository hubConnectionRepository)
        : PageModel
    {
        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            await hubConnectionRepository.RemoveConnectionsByUsernameAsync(User.Identity!.Name!);
            await signInManager.SignOutAsync();
            logger.LogInformation("User logged out.");
            return returnUrl != null ? LocalRedirect(returnUrl) : RedirectToPage();
        }

    }
}
