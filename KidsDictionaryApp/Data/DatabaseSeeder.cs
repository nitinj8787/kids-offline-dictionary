using SQLite;
using KidsDictionaryApp.Models;
using KidsDictionaryApp.Data;
using System.IO;

namespace KidsDictionaryApp.Data
{
    public static class DatabaseSeeder
    {
        public static async Task CreatePrePopulatedDatabase(string dbPath)
        {
            var db = new SQLiteAsyncConnection(dbPath);
            
            // Create tables
            await db.CreateTableAsync<Word>();
            await db.CreateTableAsync<WordHistory>();
            await db.CreateTableAsync<FavoriteWord>();
            
            // Insert sample words
            var words = new[]
            {
                new Word { WordText = "apple", Meaning = "A round fruit with red or green skin", Example = "I ate an apple for lunch.", Synonyms = "fruit" },
                new Word { WordText = "book", Meaning = "A set of written pages bound together", Example = "She read a book about dinosaurs.", Synonyms = "novel, publication" },
                new Word { WordText = "cat", Meaning = "A small furry animal that says meow", Example = "The cat climbed the tree.", Synonyms = "feline, kitten" },
                new Word { WordText = "dog", Meaning = "A loyal furry animal that barks", Example = "The dog wagged its tail.", Synonyms = "canine, hound" },
                new Word { WordText = "butterfly", Meaning = "A flying insect with colorful wings", Example = "The butterfly landed on the flower.", Synonyms = "insect" },
                new Word { WordText = "elephant", Meaning = "A very large animal with a long trunk", Example = "The elephant sprayed water with its trunk.", Synonyms = "pachyderm" },
                new Word { WordText = "happy", Meaning = "Feeling or showing pleasure or joy", Example = "She was happy to see her friends.", Synonyms = "joyful, glad, pleased" },
                new Word { WordText = "jungle", Meaning = "A dense tropical forest full of wildlife", Example = "The explorer walked through the jungle.", Synonyms = "forest, rainforest" },
            };
            
            await db.InsertAllAsync(words);
            
            await db.CloseAsync();
        }
    }
}