using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecureTodo.Models;

namespace SecureTodo.Pages.Author
{
    [Authorize(Roles = "Author,Admin")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        public IndexModel(UserManager<AppUser> um) => _userManager = um;

        public IList<string> Roles { get; set; } = new List<string>();

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
                Roles = await _userManager.GetRolesAsync(user);
        }
    }
}