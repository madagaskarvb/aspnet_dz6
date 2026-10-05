using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SecureTodo.Data;
using SecureTodo.Models;

namespace SecureTodo.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthApiController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;

        private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

        public AuthApiController(UserManager<AppUser> um,
                                 SignInManager<AppUser> sm,
                                 ApplicationDbContext db,
                                 IConfiguration config)
        {
            _userManager = um;
            _signInManager = sm;
            _db = db;
            _config = config;
        }

        // ---------- POST /api/auth/login ----------
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) return Unauthorized(new { error = "Неверные данные" });

            var result = await _signInManager.CheckPasswordSignInAsync(
                user, dto.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
                return StatusCode(423, new { error = "Аккаунт заблокирован" });

            if (!result.Succeeded)
                return Unauthorized(new { error = "Неверные данные" });

            var access = await IssueAccessTokenAsync(user);
            var refresh = await IssueRefreshTokenAsync(user);

            return Ok(new
            {
                accessToken = access,
                refreshToken = refresh.Token,
                accessExpiresIn = (int)AccessTokenLifetime.TotalSeconds,
                refreshExpiresAt = refresh.ExpiresAt
            });
        }

        // ---------- POST /api/auth/refresh ----------
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var stored = await _db.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == dto.RefreshToken);

            if (stored == null)
                return Unauthorized(new { error = "Refresh-токен не найден" });

            if (!stored.IsActive)
                return Unauthorized(new { error = "Refresh-токен недействителен или отозван" });

            var user = await _userManager.FindByIdAsync(stored.UserId);
            if (user == null)
                return Unauthorized(new { error = "Пользователь не найден" });

            // Ротация: отзываем старый, выдаём новый
            stored.RevokedAt = DateTime.UtcNow;

            var newAccess = await IssueAccessTokenAsync(user);
            var newRefresh = await IssueRefreshTokenAsync(user);

            await _db.SaveChangesAsync();

            return Ok(new
            {
                accessToken = newAccess,
                refreshToken = newRefresh.Token,
                accessExpiresIn = (int)AccessTokenLifetime.TotalSeconds,
                refreshExpiresAt = newRefresh.ExpiresAt
            });
        }

        // ---------- POST /api/auth/logout ----------
        // Отзывает конкретный refresh-токен. Валидный access-токен обязателен,
        // чтобы нельзя было по чужому refresh-токену разлогинить пользователя.
        [HttpPost("logout")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Logout([FromBody] LogoutRequestDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                                ?? User.FindFirstValue("sub");

            var stored = await _db.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == dto.RefreshToken);

            if (stored == null)
                return NoContent(); // идемпотентно: уже нет — и ладно

            if (stored.UserId != currentUserId)
                return Forbid();

            if (stored.RevokedAt == null)
            {
                stored.RevokedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            return NoContent();
        }

        // ---------- POST /api/auth/logout-all ----------
        [HttpPost("logout-all")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> LogoutAll()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                                ?? User.FindFirstValue("sub");

            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized();

            var active = await _db.RefreshTokens
                .Where(t => t.UserId == currentUserId && t.RevokedAt == null)
                .ToListAsync();

            var now = DateTime.UtcNow;
            foreach (var t in active)
                t.RevokedAt = now;

            await _db.SaveChangesAsync();

            return Ok(new { revoked = active.Count });
        }

        // ---------- helpers ----------

        private async Task<string> IssueAccessTokenAsync(AppUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var jwt = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.Add(AccessTokenLifetime),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<RefreshToken> IssueRefreshTokenAsync(AppUser user)
        {
            var token = new RefreshToken
            {
                Token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(RefreshTokenLifetime)
            };

            _db.RefreshTokens.Add(token);
            await _db.SaveChangesAsync();
            return token;
        }
    }
}