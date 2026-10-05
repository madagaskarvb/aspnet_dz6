using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecureTodo.Models;

namespace SecureTodo.Pages
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;

        public IndexModel(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public string? DisplayName { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();

        public async Task OnGetAsync()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                DisplayName = user?.FullName ?? user?.Email ?? User.Identity.Name;
                if (user != null)
                    Roles = await _userManager.GetRolesAsync(user);
            }
        }
    }
}