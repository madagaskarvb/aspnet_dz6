using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecureTodo.Models;

namespace SecureTodo.Pages.Account
{
    public class TwoFactorLoginModel : PageModel
    {
        private readonly SignInManager<AppUser> _signInManager;

        public TwoFactorLoginModel(SignInManager<AppUser> sm) => _signInManager = sm;

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public bool RememberMe { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Введите код из приложения")]
            [StringLength(7, MinimumLength = 6, ErrorMessage = "Код состоит из 6 цифр")]
            [Display(Name = "6-значный код")]
            public string Code { get; set; } = string.Empty;

            [Display(Name = "Запомнить это устройство")]
            public bool RememberMachine { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(bool rememberMe = false)
        {
            // Проверяем, что пользователь действительно на этапе 2FA
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null) return RedirectToPage("./Login");

            RememberMe = rememberMe;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(bool rememberMe = false)
        {
            if (!ModelState.IsValid) return Page();

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null) return RedirectToPage("./Login");

            var code = Input.Code.Replace(" ", "").Replace("-", "");

            var result = await _signInManager.TwoFactorSignInAsync(
                TokenOptions.DefaultAuthenticatorProvider,
                code,
                isPersistent: rememberMe,
                rememberClient: Input.RememberMachine);

            if (result.Succeeded)
                return RedirectToPage("/Index");

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty,
                    "Аккаунт заблокирован на 15 минут.");
                return Page();
            }

            ModelState.AddModelError(string.Empty, "Неверный код. Попробуйте снова.");
            return Page();
        }
    }
}