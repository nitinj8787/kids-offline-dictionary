using SQLite;

namespace KidsDictionaryApp.Models
{
    public class Achievement
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string BadgeEmoji { get; set; } = "🏅";

        public string Category { get; set; } = "General";

        /// <summary>
        /// Type of achievement: WordCount, GameScore, FavoriteCount, GamePlayed
        /// </summary>
        public string AchievementType { get; set; } = "WordCount";

        /// <summary>
        /// Threshold to unlock this achievement (e.g., look up 10 words)
        /// </summary>
        public int Threshold { get; set; } = 1;

        /// <summary>
        /// Bonus score awarded when this achievement is earned
        /// </summary>
        public int BonusScore { get; set; } = 50;
    }
}
