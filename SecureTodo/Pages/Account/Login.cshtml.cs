using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecureTodo.Models;

namespace SecureTodo.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<AppUser> _signInManager;

        public LoginModel(SignInManager<AppUser> sm) => _signInManager = sm;

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Укажите email")]
            [EmailAddress(ErrorMessage = "Некорректный email")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Укажите пароль")]
            [DataType(DataType.Password)]
            [Display(Name = "Пароль")]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Запомнить меня")]
            public bool RememberMe { get; set; }
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var result = await _signInManager.PasswordSignInAsync(
                Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
                return RedirectToPage("/Index");

            // Включена 2FA — уводим на страницу ввода кода
            if (result.RequiresTwoFactor)
                return RedirectToPage("./TwoFactorLogin",
                    new { rememberMe = Input.RememberMe });

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty,
                    "Аккаунт заблокирован на 15 минут из-за 5 неудачных попыток входа.");
                return Page();
            }

            ModelState.AddModelError(string.Empty, "Неверный email или пароль.");
            return Page();
        }
    }
}