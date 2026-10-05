using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QRCoder;
using SecureTodo.Models;

namespace SecureTodo.Pages.Account
{
    [Authorize]
    public class EnableAuthenticatorModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<EnableAuthenticatorModel> _logger;

        public EnableAuthenticatorModel(UserManager<AppUser> um,
                                        ILogger<EnableAuthenticatorModel> logger)
        {
            _userManager = um;
            _logger = logger;
        }

        public string SharedKey { get; set; } = string.Empty;
        public string QrCodeBase64 { get; set; } = string.Empty;

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Введите код из приложения")]
            [StringLength(7, MinimumLength = 6, ErrorMessage = "Код состоит из 6 цифр")]
            [Display(Name = "Код подтверждения")]
            public string Code { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("./Login");

            // Если 2FA уже включена — нечего тут делать
            if (await _userManager.GetTwoFactorEnabledAsync(user))
                return RedirectToPage("./Profile");

            await LoadSharedKeyAndQrAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("./Login");

            if (!ModelState.IsValid)
            {
                await LoadSharedKeyAndQrAsync(user);
                return Page();
            }

            var code = Input.Code.Replace(" ", "").Replace("-", "");

            var isValid = await _userManager.VerifyTwoFactorTokenAsync(
                user,
                _userManager.Options.Tokens.AuthenticatorTokenProvider,
                code);

            if (!isValid)
            {
                ModelState.AddModelError("Input.Code", "Код неверный. Попробуйте снова.");
                await LoadSharedKeyAndQrAsync(user);
                return Page();
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);

            // Обновляем cookie, чтобы новое состояние 2FA подхватилось
            await _userManager.UpdateSecurityStampAsync(user);

            TempData["Message"] = "Двухфакторная аутентификация включена.";
            return RedirectToPage("./Profile");
        }

        private async Task LoadSharedKeyAndQrAsync(AppUser user)
        {
            var key = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(key))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                key = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            SharedKey = FormatKey(key!);

            var issuer = "SecureTodo";
            var email = await _userManager.GetEmailAsync(user);

            var otpauthUrl =
                $"otpauth://totp/{UrlEncoder.Default.Encode(issuer)}:" +
                $"{UrlEncoder.Default.Encode(email ?? user.UserName ?? "user")}" +
                $"?secret={key}" +
                $"&issuer={UrlEncoder.Default.Encode(issuer)}" +
                $"&digits=6";

            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(otpauthUrl, QRCodeGenerator.ECCLevel.Q);
            var pngQr = new PngByteQRCode(qrData);
            byte[] bytes = pngQr.GetGraphic(20);
            QrCodeBase64 = Convert.ToBase64String(bytes);
        }

        // Форматируем ключ по 4 символа — удобнее вводить вручную
        private static string FormatKey(string unformattedKey)
        {
            var sb = new StringBuilder();
            int i = 0;
            while (i + 4 < unformattedKey.Length)
            {
                sb.Append(unformattedKey.AsSpan(i, 4)).Append(' ');
                i += 4;
            }
            if (i < unformattedKey.Length)
                sb.Append(unformattedKey.AsSpan(i));
            return sb.ToString().ToLowerInvariant();
        }
    }
}