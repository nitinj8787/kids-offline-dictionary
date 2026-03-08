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
    }
}
