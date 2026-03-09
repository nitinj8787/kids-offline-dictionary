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

        public async Task<int> GetTodayCountAsync()
        {
            var todayUtc = DateTime.UtcNow.Date;
            var tomorrowUtc = todayUtc.AddDays(1);
            return await _db.Table<WordHistory>()
                .Where(h => h.LookedUpAt >= todayUtc && h.LookedUpAt < tomorrowUtc)
                .CountAsync();
        }

        public async Task<int> GetThisWeekCountAsync()
        {
            var weekStartUtc = DateTime.UtcNow.Date.AddDays(-6);
            return await _db.Table<WordHistory>()
                .Where(h => h.LookedUpAt >= weekStartUtc)
                .CountAsync();
        }

        public async Task<int> GetThisMonthCountAsync()
        {
            var now = DateTime.UtcNow;
            var monthStartUtc = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            return await _db.Table<WordHistory>()
                .Where(h => h.LookedUpAt >= monthStartUtc)
                .CountAsync();
        }
    }
}
