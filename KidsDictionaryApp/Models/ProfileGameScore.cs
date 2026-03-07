using SQLite;

namespace KidsDictionaryApp.Models
{
    public class ProfileGameScore
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int ProfileId { get; set; }

        /// <summary>
        /// Name of the game, e.g. "MatchGame" or "Hangman"
        /// </summary>
        public string GameName { get; set; } = string.Empty;

        public int Score { get; set; } = 0;

        public int Attempts { get; set; } = 0;

        public bool Completed { get; set; } = false;

        public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
    }
}
