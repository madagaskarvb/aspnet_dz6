using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecureTodo.Models;

namespace SecureTodo.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;

        public IndexModel(UserManager<AppUser> um) => _userManager = um;

        public List<(AppUser User, IList<string> Roles)> Users { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        // POST ?handler=ChangeRole
        public async Task<IActionResult> OnPostChangeRoleAsync(string userId, string newRole)
        {
            var currentUserId = _userManager.GetUserId(User);
            var target = await _userManager.FindByIdAsync(userId);

            if (target == null)
            {
                TempData["Error"] = "Пользователь не найден.";
                return RedirectToPage();
            }

            // Разрешённые роли
            var allowed = new[] { "User", "Author", "Admin" };
            if (!allowed.Contains(newRole))
            {
                TempData["Error"] = "Недопустимая роль.";
                return RedirectToPage();
            }

            // Защита: нельзя снять роль Admin с самого себя
            if (target.Id == currentUserId && newRole != "Admin")
            {
                TempData["Error"] = "Нельзя снять роль Admin с самого себя.";
                return RedirectToPage();
            }

            // 1) Убираем все текущие роли
            var currentRoles = await _userManager.GetRolesAsync(target);
            var removeResult = await _userManager.RemoveFromRolesAsync(target, currentRoles);
            if (!removeResult.Succeeded)
            {
                TempData["Error"] = string.Join("; ",
                    removeResult.Errors.Select(e => e.Description));
                return RedirectToPage();
            }

            // 2) Назначаем новую роль
            var addResult = await _userManager.AddToRoleAsync(target, newRole);
            if (!addResult.Succeeded)
            {
                TempData["Error"] = string.Join("; ",
                    addResult.Errors.Select(e => e.Description));
                return RedirectToPage();
            }

            // 3) Форсируем обновление cookie у target — SecurityStampValidator
            //    пересоздаст claims при следующем запросе пользователя.
            await _userManager.UpdateSecurityStampAsync(target);

            TempData["Message"] = $"Роль пользователя {target.Email} изменена на {newRole}.";
            return RedirectToPage();
        }

        private async Task LoadAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                Users.Add((u, roles));
            }
        }
    }
}