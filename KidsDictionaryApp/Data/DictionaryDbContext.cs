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
            await _database.CreateTableAsync<UserProfile>();
            await _database.CreateTableAsync<ProfileWordProgress>();
            await _database.CreateTableAsync<ProfileGameScore>();
            await _database.CreateTableAsync<Achievement>();
            await _database.CreateTableAsync<ProfileAchievement>();

            // Add new columns to UserProfile for existing databases.
            // SQLite raises an error if the column already exists; we catch and ignore it.
            await TryAddColumnAsync("UserProfile", "ParentEmail", "TEXT");
            await TryAddColumnAsync("UserProfile", "RemoteId", "INTEGER");
            await TryAddColumnAsync("UserProfile", "LastSyncedAt", "TEXT");
        }

        private async Task TryAddColumnAsync(string table, string column, string type)
        {
            // Allowlist the table name and column name to prevent any possible injection
            // (these are internal constants, but defence-in-depth is good practice).
            if (!AllowedTables.Contains(table) || !AllowedColumns.Contains(column))
                throw new ArgumentException($"Unsupported table/column for migration: {table}.{column}");

            try
            {
                await _database.ExecuteAsync($"ALTER TABLE {table} ADD COLUMN {column} {type}");
            }
            catch
            {
                // Column already exists — nothing to do.
            }
        }

        private static readonly HashSet<string> AllowedTables = new() { "UserProfile" };
        private static readonly HashSet<string> AllowedColumns = new() { "ParentEmail", "RemoteId", "LastSyncedAt" };

        public SQLiteAsyncConnection Database => _database;
    }
}