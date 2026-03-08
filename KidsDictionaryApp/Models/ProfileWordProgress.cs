using SQLite;

namespace KidsDictionaryApp.Models
{
    public class ProfileWordProgress
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int ProfileId { get; set; }

        [Indexed]
        public string WordText { get; set; } = string.Empty;

        public int TimesLookedUp { get; set; } = 0;

        public bool IsLearned { get; set; } = false;

        public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
    }
}
