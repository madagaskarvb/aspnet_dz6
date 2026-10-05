using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecureTodo.Models;

namespace SecureTodo.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public RegisterModel(UserManager<AppUser> um, SignInManager<AppUser> sm)
        {
            _userManager = um;
            _signInManager = sm;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Укажите имя")]
            [Display(Name = "Полное имя")]
            public string FullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Укажите email")]
            [EmailAddress(ErrorMessage = "Некорректный email")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Укажите пароль")]
            [StringLength(100, MinimumLength = 6,
                ErrorMessage = "Пароль должен содержать минимум 6 символов")]
            [DataType(DataType.Password)]
            [Display(Name = "Пароль")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Подтвердите пароль")]
            [Compare("Password", ErrorMessage = "Пароли не совпадают")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var user = new AppUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                FullName = Input.FullName,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                // назначаем роль User
                await _userManager.AddToRoleAsync(user, "User");

                // авто-вход
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToPage("/Index");
            }

            foreach (var err in result.Errors)
                ModelState.AddModelError(string.Empty, err.Description);

            return Page();
        }
    }
}