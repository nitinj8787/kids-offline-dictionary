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
            // MigrateTable adds new columns (ParentEmail, RemoteId, LastSyncedAt) to existing databases
            await _database.CreateTableAsync<UserProfile>(CreateFlags.MigrateTable);
            await _database.CreateTableAsync<ProfileWordProgress>();
            await _database.CreateTableAsync<ProfileGameScore>();
            await _database.CreateTableAsync<Achievement>();
            await _database.CreateTableAsync<ProfileAchievement>();
        }

        public SQLiteAsyncConnection Database => _database;
    }
}