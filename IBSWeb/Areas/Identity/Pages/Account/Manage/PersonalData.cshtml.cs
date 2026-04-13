// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IBSWeb.Areas.Identity.Pages.Account.Manage
{
    public class PersonalDataModel(
        UserManager<IdentityUser> userManager,
        ILogger<PersonalDataModel> logger)
        : PageModel
    {
        private readonly ILogger<PersonalDataModel> _logger = logger;

        public async Task<IActionResult> OnGet()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }

            return Page();
        }
    }
}
