using SQLite;

namespace KidsDictionaryApp.Models
{
    public class FavoriteWord
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public string WordText { get; set; } = string.Empty;

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}
