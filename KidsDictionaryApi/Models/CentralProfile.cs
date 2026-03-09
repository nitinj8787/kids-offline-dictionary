using System.ComponentModel.DataAnnotations;

namespace KidsDictionaryApi.Models
{
    /// <summary>
    /// Centralized profile record for a child's avatar.
    /// Only avatar nickname and emoji are stored — no child personal information.
    /// </summary>
    public class CentralProfile
    {
        public int Id { get; set; }

        public int UserAccountId { get; set; }

        /// <summary>Avatar nickname chosen by the parent — not the child's real name.</summary>
        [Required]
        [MaxLength(50)]
        public string AvatarName { get; set; } = string.Empty;

        [MaxLength(10)]
        public string AvatarEmoji { get; set; } = "🧒";

        public int TotalScore { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Tracks when this profile was last synced from the mobile app.</summary>
        public DateTime? LastSyncedAt { get; set; }

        // Navigation
        public UserAccount UserAccount { get; set; } = null!;
    }
}
