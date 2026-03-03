using SQLite;
using KidsDictionaryApp.Models;

namespace KidsDictionaryApp.Data
{
    public class DictionaryDbContext
    {
        private readonly SQLiteAsyncConnection _database;

        public DictionaryDbContext(string dbPath)
        {
            _database = new SQLiteAsyncConnection(dbPath);
        }

        public async Task InitializeAsync()
        {
            await _database.CreateTableAsync<Word>();
            await _database.CreateTableAsync<WordHistory>();
            await _database.CreateTableAsync<FavoriteWord>();
        }

        public SQLiteAsyncConnection Database => _database;
    }
}