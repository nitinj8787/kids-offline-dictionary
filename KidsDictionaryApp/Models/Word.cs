using SQLite;

namespace KidsDictionaryApp.Models
{
    public class Word
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public string WordText { get; set; }

        public string Type { get; set; } 
        public string Category { get; set; } 

        public string Meaning { get; set; }

        public string Example { get; set; }

        public string Synonyms { get; set; }
    }
}