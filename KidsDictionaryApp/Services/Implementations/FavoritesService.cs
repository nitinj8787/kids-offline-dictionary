using KidsDictionaryApp.Data;
using KidsDictionaryApp.Models;
using KidsDictionaryApp.Services.Interfaces;
using SQLite;

namespace KidsDictionaryApp.Services.Implementations
{
    public class FavoritesService : IFavoritesService
    {
        private readonly SQLiteAsyncConnection _db;

        public FavoritesService(DictionaryDbContext context)
        {
            _db = context.Database;
        }

        public async Task AddAsync(string word)
        {
            var existing = await _db.Table<FavoriteWord>()
                .Where(f => f.WordText == word)
                .FirstOrDefaultAsync();

            if (existing == null)
            {
                await _db.InsertAsync(new FavoriteWord { WordText = word });
            }
        }

        public async Task RemoveAsync(string word)
        {
            var existing = await _db.Table<FavoriteWord>()
                .Where(f => f.WordText == word)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                await _db.DeleteAsync(existing);
            }
        }

        public async Task<bool> IsFavoriteAsync(string word)
        {
            return await _db.Table<FavoriteWord>()
                .Where(f => f.WordText == word)
                .CountAsync() > 0;
        }

        public async Task<List<FavoriteWord>> GetFavoritesAsync()
        {
            return await _db.Table<FavoriteWord>()
                .OrderByDescending(f => f.AddedAt)
                .ToListAsync();
        }
    }
}
