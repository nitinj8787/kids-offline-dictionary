using SQLite;

namespace KidsDictionaryApp.Models
{
    public class WordHistory
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public string WordText { get; set; } = string.Empty;

        public DateTime LookedUpAt { get; set; } = DateTime.UtcNow;
    }
}
