using KidsDictionaryApp.Data;
using KidsDictionaryApp.Models;
using KidsDictionaryApp.Services.Interfaces;
using SQLite;

namespace KidsDictionaryApp.Services.Implementations
{
    public class WordRepository : IWordRepository
    {
        private readonly SQLiteAsyncConnection _db;

        public WordRepository(DictionaryDbContext context)
        {
            _db = context.Database;
        }

        public async Task<Word?> GetWordAsync(string word)
        {
            return await _db.Table<Word>()
                .Where(w => w.WordText.ToLower() == word.ToLower())
                .FirstOrDefaultAsync();
        }

        public async Task<List<Word>> GetAllWordsAsync()
        {
            return await _db.Table<Word>().ToListAsync();
        }
    }
}