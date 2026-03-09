-- =============================================================================
-- Kids Dictionary App — Offline (MAUI) SQLite Database Schema
-- =============================================================================
-- Run this script once against the local SQLite file before the first user
-- creates a profile. Every statement is idempotent (CREATE ... IF NOT EXISTS)
-- so it is safe to re-run on an existing database.
--
-- File location at runtime (Windows):
--   %LOCALAPPDATA%\Packages\com.companyname.kidsdictionaryapp_...\LocalState\dictionary.db
--
-- The schema here mirrors exactly what DictionaryDbContext.InitializeAsync()
-- creates via sqlite-net-pcl at app startup.
-- =============================================================================

PRAGMA journal_mode = WAL;
PRAGMA foreign_keys = ON;

-- ---------------------------------------------------------------------------
-- Word
-- Main dictionary content — populated from the bundled dictionary.db asset.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "Word" (
    "Id"              INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    "WordText"        TEXT,
    "PartOfSpeech"    TEXT,
    "Category"        TEXT,
    "Meaning"         TEXT,
    "Example"         TEXT,
    "Phonics"         TEXT,
    "Syllables"       TEXT,
    "Synonyms"        TEXT,
    "Antonyms"        TEXT,
    "DifficultyLevel" INTEGER NOT NULL DEFAULT 0,
    "FrequencyRank"   INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS "IX_Word_WordText" ON "Word" ("WordText");

-- ---------------------------------------------------------------------------
-- WordHistory
-- Global (non-profile-specific) history of recently looked-up words.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "WordHistory" (
    "Id"         INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    "WordText"   TEXT    NOT NULL DEFAULT '',
    "LookedUpAt" TEXT    NOT NULL  -- ISO-8601 UTC timestamp
);

CREATE INDEX IF NOT EXISTS "IX_WordHistory_WordText" ON "WordHistory" ("WordText");

-- ---------------------------------------------------------------------------
-- FavoriteWord
-- Global list of words saved as favourites (not per-profile).
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "FavoriteWord" (
    "Id"       INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    "WordText" TEXT    NOT NULL DEFAULT '',
    "AddedAt"  TEXT    NOT NULL  -- ISO-8601 UTC timestamp
);

CREATE INDEX IF NOT EXISTS "IX_FavoriteWord_WordText" ON "FavoriteWord" ("WordText");

-- ---------------------------------------------------------------------------
-- UserProfile
-- One row per child using the app on this device.
-- ParentEmail / RemoteId / LastSyncedAt are populated only after cloud sync.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "UserProfile" (
    "Id"           INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    "Name"         TEXT    NOT NULL DEFAULT '',
    "AvatarEmoji"  TEXT    NOT NULL DEFAULT '🧒',
    "CreatedAt"    TEXT    NOT NULL,   -- ISO-8601 UTC timestamp
    "TotalScore"   INTEGER NOT NULL DEFAULT 0,
    "ParentEmail"  TEXT,               -- optional; enables cloud sync
    "RemoteId"     INTEGER,            -- assigned by KidsDictionaryApi on first sync
    "LastSyncedAt" TEXT                -- ISO-8601 UTC timestamp of last successful sync
);

-- ---------------------------------------------------------------------------
-- ProfileWordProgress
-- Tracks how many times each profile has looked up each word.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "ProfileWordProgress" (
    "Id"             INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    "ProfileId"      INTEGER NOT NULL DEFAULT 0,
    "WordText"       TEXT    NOT NULL DEFAULT '',
    "TimesLookedUp"  INTEGER NOT NULL DEFAULT 0,
    "IsLearned"      INTEGER NOT NULL DEFAULT 0,  -- 0 = false, 1 = true
    "LastAccessedAt" TEXT    NOT NULL              -- ISO-8601 UTC timestamp
);

CREATE INDEX IF NOT EXISTS "IX_ProfileWordProgress_ProfileId" ON "ProfileWordProgress" ("ProfileId");
CREATE INDEX IF NOT EXISTS "IX_ProfileWordProgress_WordText"   ON "ProfileWordProgress" ("WordText");

-- ---------------------------------------------------------------------------
-- ProfileGameScore
-- One row per game session played by a profile.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "ProfileGameScore" (
    "Id"        INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    "ProfileId" INTEGER NOT NULL DEFAULT 0,
    "GameName"  TEXT    NOT NULL DEFAULT '',  -- e.g. 'MatchGame', 'Hangman', 'WordScramble'
    "Score"     INTEGER NOT NULL DEFAULT 0,
    "Attempts"  INTEGER NOT NULL DEFAULT 0,
    "Completed" INTEGER NOT NULL DEFAULT 0,   -- 0 = false, 1 = true
    "PlayedAt"  TEXT    NOT NULL              -- ISO-8601 UTC timestamp
);

CREATE INDEX IF NOT EXISTS "IX_ProfileGameScore_ProfileId" ON "ProfileGameScore" ("ProfileId");

-- ---------------------------------------------------------------------------
-- Achievement
-- Seeded once by AchievementService.SeedAchievementsAsync() at app startup.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "Achievement" (
    "Id"              INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    "Name"            TEXT    NOT NULL DEFAULT '',
    "Description"     TEXT    NOT NULL DEFAULT '',
    "BadgeEmoji"      TEXT    NOT NULL DEFAULT '🏅',
    "Category"        TEXT    NOT NULL DEFAULT 'General',
    "AchievementType" TEXT    NOT NULL DEFAULT 'WordCount',  -- WordCount | GameScore | FavoriteCount | GamePlayed
    "Threshold"       INTEGER NOT NULL DEFAULT 1,
    "BonusScore"      INTEGER NOT NULL DEFAULT 50
);

-- ---------------------------------------------------------------------------
-- ProfileAchievement
-- Junction table — records when a profile earns an achievement.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "ProfileAchievement" (
    "Id"            INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    "ProfileId"     INTEGER NOT NULL DEFAULT 0,
    "AchievementId" INTEGER NOT NULL DEFAULT 0,
    "EarnedAt"      TEXT    NOT NULL  -- ISO-8601 UTC timestamp
);

CREATE INDEX IF NOT EXISTS "IX_ProfileAchievement_ProfileId"     ON "ProfileAchievement" ("ProfileId");
CREATE INDEX IF NOT EXISTS "IX_ProfileAchievement_AchievementId" ON "ProfileAchievement" ("AchievementId");
