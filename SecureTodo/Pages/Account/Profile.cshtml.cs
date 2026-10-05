using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecureTodo.Models;

namespace SecureTodo.Pages.Account
{
    [Authorize]
    public class ProfileModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;

        public ProfileModel(UserManager<AppUser> um) => _userManager = um;

        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public IList<string> Roles { get; set; } = new List<string>();
        public bool TwoFactorEnabled { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("./Login");

            await FillAsync(user);
            return Page();
        }

        // Отключить 2FA
        public async Task<IActionResult> OnPostDisable2faAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("./Login");

            // Сбрасываем и флаг 2FA, и ключ аутентификатора
            await _userManager.SetTwoFactorEnabledAsync(user, false);
            await _userManager.ResetAuthenticatorKeyAsync(user);

            // Обновляем cookie (SecurityStamp поменялся)
            await _userManager.UpdateSecurityStampAsync(user);

            TempData["Message"] = "Двухфакторная аутентификация отключена.";
            return RedirectToPage();
        }

        private async Task FillAsync(AppUser user)
        {
            Email = user.Email ?? string.Empty;
            FullName = user.FullName ?? string.Empty;
            Roles = await _userManager.GetRolesAsync(user);
            TwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        }
    }
}