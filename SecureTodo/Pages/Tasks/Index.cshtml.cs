using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecureTodo.Data;
using SecureTodo.Models;

namespace SecureTodo.Pages.Tasks
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAuthorizationService _auth;

        public IndexModel(ApplicationDbContext db, IAuthorizationService auth)
        {
            _db = db;
            _auth = auth;
        }

        public List<(TaskItem Task, bool CanEdit)> Items { get; set; } = new();

        public async Task OnGetAsync()
        {
            var tasks = await _db.Tasks
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            foreach (var t in tasks)
            {
                var result = await _auth.AuthorizeAsync(User, t, "CanEditTask");
                Items.Add((t, result.Succeeded));
            }
        }

        // AJAX handler: ?handler=MarkCompleted&id=X
        public async Task<IActionResult> OnPostMarkCompletedAsync(int id)
        {
            var task = await _db.Tasks.FindAsync(id);
            if (task == null) return NotFound();

            var result = await _auth.AuthorizeAsync(User, task, "CanEditTask");
            if (!result.Succeeded)
            {
                if (User.Identity?.IsAuthenticated != true) return Challenge();
                return Forbid();
            }

            task.IsCompleted = !task.IsCompleted;
            await _db.SaveChangesAsync();

            return new JsonResult(new { id = task.Id, isCompleted = task.IsCompleted });
        }
    }
}