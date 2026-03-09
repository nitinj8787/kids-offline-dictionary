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
        public string EventType { get; set; } = string.Empty;

        /// <summary>JSON-encoded event payload (no personal data).</summary>
        public string? EventData { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
