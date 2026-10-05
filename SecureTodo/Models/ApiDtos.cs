using System.ComponentModel.DataAnnotations;

namespace SecureTodo.Models
{
    public class LoginDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class TaskDto
    {
        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        public bool IsCompleted { get; set; }
    }

    public class RefreshRequestDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class LogoutRequestDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}