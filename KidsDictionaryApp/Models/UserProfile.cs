using SQLite;

namespace KidsDictionaryApp.Models
{
    public class UserProfile
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string AvatarEmoji { get; set; } = "🧒";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int TotalScore { get; set; } = 0;

        /// <summary>
        /// Parent's email address used to link this offline profile to a centralized account.
        /// Optional — profiles without an email work fully offline.
        /// </summary>
        public string? ParentEmail { get; set; }

        /// <summary>
        /// The profile ID assigned by the central API after a successful sync.
        /// Null until the profile has been synced at least once.
        /// </summary>
        public int? RemoteId { get; set; }

        /// <summary>The UTC timestamp of the last successful sync to the central server.</summary>
        public DateTime? LastSyncedAt { get; set; }
    }
}
