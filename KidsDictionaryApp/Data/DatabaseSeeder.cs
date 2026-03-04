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
                new Word { WordText = "Apple", Meaning = "A round fruit with red or green skin", Example = "I ate an apple for lunch.", Synonyms = "fruit" },
                new Word { WordText = "Book", Meaning = "A set of written pages bound together", Example = "She read a book about dinosaurs.", Synonyms = "novel, publication" },
                new Word { WordText = "Cat", Meaning = "A small furry animal that says meow", Example = "The cat climbed the tree.", Synonyms = "feline, kitten" },
                // Add more words here
            };
            
            await db.InsertAllAsync(words);
            
            await db.CloseAsync();
        }
    }
}