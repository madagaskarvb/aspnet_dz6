using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecureTodo.Data;
using SecureTodo.Models;

namespace SecureTodo.Pages.Tasks
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAuthorizationService _auth;

        public DeleteModel(ApplicationDbContext db, IAuthorizationService auth)
        {
            _db = db;
            _auth = auth;
        }

        [BindProperty]
        public TaskItem Task { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var task = await _db.Tasks.FindAsync(id);
            if (task == null) return NotFound();

            var result = await _auth.AuthorizeAsync(User, task, "CanEditTask");
            if (!result.Succeeded)
            {
                if (User.Identity?.IsAuthenticated != true) return Challenge();
                return Forbid();
            }

            Task = task;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var task = await _db.Tasks.FindAsync(id);
            if (task == null) return NotFound();

            var result = await _auth.AuthorizeAsync(User, task, "CanEditTask");
            if (!result.Succeeded)
            {
                if (User.Identity?.IsAuthenticated != true) return Challenge();
                return Forbid();
            }

            _db.Tasks.Remove(task);
            await _db.SaveChangesAsync();
            return RedirectToPage("./Index");
        }
    }
}