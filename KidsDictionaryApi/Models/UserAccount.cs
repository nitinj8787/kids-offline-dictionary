using System.ComponentModel.DataAnnotations;

namespace KidsDictionaryApi.Models
{
    /// <summary>
    /// Represents a parent's account identified by their email address.
    /// Only the email is stored — no child personal information is collected.
    /// </summary>
    public class UserAccount
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        // Navigation
        public ICollection<CentralProfile> Profiles { get; set; } = new List<CentralProfile>();
        public ICollection<AppUsage> Usages { get; set; } = new List<AppUsage>();
    }
}
