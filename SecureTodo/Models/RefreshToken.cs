using System.ComponentModel.DataAnnotations;

namespace SecureTodo.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Token { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;

        public AppUser? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        public bool IsActive =>
            RevokedAt == null && DateTime.UtcNow < ExpiresAt;
    }
}