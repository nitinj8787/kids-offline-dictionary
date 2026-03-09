using System.ComponentModel.DataAnnotations;

namespace KidsDictionaryApi.Models
{
    /// <summary>
    /// Records anonymous app usage metrics for analytics and engagement tracking.
    /// No personal child data is stored — only aggregate usage events.
    /// </summary>
    public class AppUsage
    {
        public int Id { get; set; }

        public int UserAccountId { get; set; }

        /// <summary>Optional profile reference for per-profile analytics.</summary>
        public int? CentralProfileId { get; set; }

        /// <summary>Event type: e.g. "WordLookup", "GamePlayed", "ProfileSync".</summary>
        [Required]
        [MaxLength(100)]
        public string EventType { get; set; } = string.Empty;

        /// <summary>JSON-encoded event payload (no personal data).</summary>
        [MaxLength(1000)]
        public string? EventData { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public UserAccount UserAccount { get; set; } = null!;
    }
}
