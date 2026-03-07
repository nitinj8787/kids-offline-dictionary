using SQLite;

namespace KidsDictionaryApp.Models
{
    public class ProfileAchievement
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int ProfileId { get; set; }

        [Indexed]
        public int AchievementId { get; set; }

        public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
    }
}
