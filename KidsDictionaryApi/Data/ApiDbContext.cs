using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;

namespace KidsDictionaryApi.Data
{
    /// <summary>
    /// Lightweight connection factory for Dapper queries.
    /// Creates the database schema on first use via <see cref="EnsureSchemaAsync"/>.
    /// </summary>
    public class ApiDbContext
    {
        private readonly string _connectionString;

        public ApiDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>Opens and returns a new SQLite connection. The caller is responsible for disposing it.</summary>
        public IDbConnection CreateConnection()
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();
            return conn;
        }

        /// <summary>
        /// Ensures all required tables and indexes exist. Safe to call on every startup.
        /// </summary>
        public async Task EnsureSchemaAsync()
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            // Enable foreign key enforcement for this connection
            await conn.ExecuteAsync("PRAGMA foreign_keys = ON;");

            await conn.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS UserAccount (
                    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    Email       TEXT    NOT NULL,
                    CreatedAt   TEXT    NOT NULL,
                    LastLoginAt TEXT
                );

                CREATE UNIQUE INDEX IF NOT EXISTS IX_UserAccount_Email ON UserAccount(Email);

                CREATE TABLE IF NOT EXISTS CentralProfile (
                    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserAccountId INTEGER NOT NULL,
                    AvatarName    TEXT    NOT NULL,
                    AvatarEmoji   TEXT    NOT NULL DEFAULT '🧒',
                    TotalScore    INTEGER NOT NULL DEFAULT 0,
                    CreatedAt     TEXT    NOT NULL,
                    UpdatedAt     TEXT    NOT NULL,
                    LastSyncedAt  TEXT,
                    FOREIGN KEY (UserAccountId) REFERENCES UserAccount(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS AppUsage (
                    Id               INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserAccountId    INTEGER NOT NULL,
                    CentralProfileId INTEGER,
                    EventType        TEXT    NOT NULL,
                    EventData        TEXT,
                    CreatedAt        TEXT    NOT NULL,
                    FOREIGN KEY (UserAccountId) REFERENCES UserAccount(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS OtpRecord (
                    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                    Email     TEXT    NOT NULL,
                    Code      TEXT    NOT NULL,
                    ExpiresAt TEXT    NOT NULL,
                    IsUsed    INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT    NOT NULL
                );

                CREATE INDEX IF NOT EXISTS IX_OtpRecord_Email ON OtpRecord(Email);
            ");
        }
    }
}
