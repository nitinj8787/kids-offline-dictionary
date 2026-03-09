using System.ComponentModel.DataAnnotations;

namespace KidsDictionaryApi.Models
{
    /// <summary>
    /// Stores one-time passwords used for passwordless email authentication.
    /// </summary>
    public class OtpRecord
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(6)]
        public string Code { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public bool IsUsed { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
