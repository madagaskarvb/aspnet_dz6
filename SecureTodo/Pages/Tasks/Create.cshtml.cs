using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecureTodo.Data;
using SecureTodo.Models;

namespace SecureTodo.Pages.Tasks
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public CreateModel(ApplicationDbContext db, UserManager<AppUser> um)
        {
            _db = db;
            _userManager = um;
        }

        [BindProperty]
        public TaskItem Task { get; set; } = new();

        public IActionResult OnGet() => Page();

        public async Task<IActionResult> OnPostAsync()
        {
            // Валидируем только введённые поля, UserId — всегда из текущего пользователя
            if (!ModelState.IsValid) return Page();

            Task.UserId = _userManager.GetUserId(User)!;
            Task.CreatedAt = DateTime.UtcNow;
            Task.IsCompleted = false;

            _db.Tasks.Add(Task);
            await _db.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}