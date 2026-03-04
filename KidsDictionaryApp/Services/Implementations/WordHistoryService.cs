using KidsDictionaryApp.Data;
using KidsDictionaryApp.Models;
using KidsDictionaryApp.Services.Interfaces;
using SQLite;

namespace KidsDictionaryApp.Services.Implementations
{
    public class WordHistoryService : IWordHistoryService
    {
        private readonly SQLiteAsyncConnection _db;

        public WordHistoryService(DictionaryDbContext context)
        {
            _db = context.Database;
        }

        public async Task AddAsync(string word)
        {
            var existing = await _db.Table<WordHistory>()
                .Where(h => h.WordText == word)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                existing.LookedUpAt = DateTime.UtcNow;
                await _db.UpdateAsync(existing);
            }
            else
            {
                await _db.InsertAsync(new WordHistory { WordText = word });
            }
        }

        public async Task<List<WordHistory>> GetHistoryAsync()
        {
            return await _db.Table<WordHistory>()
                .OrderByDescending(h => h.LookedUpAt)
                .ToListAsync();
        }

        public async Task ClearAsync()
        {
            await _db.DeleteAllAsync<WordHistory>();
        }
    }
}
