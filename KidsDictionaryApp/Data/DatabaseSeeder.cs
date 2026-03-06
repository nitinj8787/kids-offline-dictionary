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
                new Word { WordText = "apple", PartOfSpeech = "noun", Category = "Food", Meaning = "A round fruit with red or green skin", Example = "I ate an apple for lunch.", Phonics = "AP-ul", Syllables = "ap-ple", Synonyms = "fruit", DifficultyLevel = 1, FrequencyRank = 1 },
                new Word { WordText = "book", PartOfSpeech = "noun", Category = "Object", Meaning = "A set of written pages bound together", Example = "She read a book about dinosaurs.", Phonics = "BUHK", Syllables = "book", Synonyms = "novel, publication", DifficultyLevel = 1, FrequencyRank = 2 },
                new Word { WordText = "cat", PartOfSpeech = "noun", Category = "Animal", Meaning = "A small furry animal that says meow", Example = "The cat climbed the tree.", Phonics = "KAT", Syllables = "cat", Synonyms = "feline, kitten", Antonyms = "dog", DifficultyLevel = 1, FrequencyRank = 3 },
                new Word { WordText = "dog", PartOfSpeech = "noun", Category = "Animal", Meaning = "A loyal furry animal that barks", Example = "The dog wagged its tail.", Phonics = "DAWG", Syllables = "dog", Synonyms = "canine, hound", Antonyms = "cat", DifficultyLevel = 1, FrequencyRank = 4 },
                new Word { WordText = "butterfly", PartOfSpeech = "noun", Category = "Animal", Meaning = "A flying insect with colorful wings", Example = "The butterfly landed on the flower.", Phonics = "BUT-er-fly", Syllables = "but-ter-fly", Synonyms = "insect", DifficultyLevel = 2, FrequencyRank = 5 },
                new Word { WordText = "elephant", PartOfSpeech = "noun", Category = "Animal", Meaning = "A very large animal with a long trunk", Example = "The elephant sprayed water with its trunk.", Phonics = "EL-uh-funt", Syllables = "el-e-phant", Synonyms = "pachyderm", DifficultyLevel = 2, FrequencyRank = 6 },
                new Word { WordText = "happy", PartOfSpeech = "adjective", Category = "Emotion", Meaning = "Feeling or showing pleasure or joy", Example = "She was happy to see her friends.", Phonics = "HAP-ee", Syllables = "hap-py", Synonyms = "joyful, glad, pleased", Antonyms = "sad, unhappy", DifficultyLevel = 1, FrequencyRank = 7 },
                new Word { WordText = "jungle", PartOfSpeech = "noun", Category = "Nature", Meaning = "A dense tropical forest full of wildlife", Example = "The explorer walked through the jungle.", Phonics = "JUHNG-gul", Syllables = "jun-gle", Synonyms = "forest, rainforest", Antonyms = "desert", DifficultyLevel = 2, FrequencyRank = 8 },
            };
            
            await db.InsertAllAsync(words);
            
            await db.CloseAsync();
        }
    }
}