-- =============================================================================
-- Kids Dictionary API — Central (Online) SQLite Database Schema
-- =============================================================================
-- Run this script once against the central SQLite file before any user
-- creates or syncs a profile. Every statement is idempotent
-- (CREATE ... IF NOT EXISTS) so it is safe to re-run on an existing database.
--
-- Default file location (Development, local machine):
--   KidsDictionaryApi/bin/Debug/net9.0/kidsdictionary_api_dev.db
--
-- The schema here mirrors exactly what ApiDbContext.EnsureSchemaAsync()
-- executes via Dapper at API startup.
-- =============================================================================

PRAGMA journal_mode = WAL;
PRAGMA foreign_keys = ON;

-- ---------------------------------------------------------------------------
-- UserAccount
-- One row per parent email address. Created automatically the first time
-- an OTP is requested for that email.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "UserAccount" (
    "Id"          INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    "Email"       TEXT    NOT NULL,
    "CreatedAt"   TEXT    NOT NULL,  -- ISO-8601 UTC timestamp
    "LastLoginAt" TEXT               -- ISO-8601 UTC; updated on each successful OTP verify
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserAccount_Email" ON "UserAccount" ("Email");

-- ---------------------------------------------------------------------------
-- CentralProfile
-- One row per child profile synced from the MAUI app.
-- A single UserAccount can own multiple CentralProfiles (one per child).
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "CentralProfile" (
    "Id"            INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    "UserAccountId" INTEGER NOT NULL,
    "AvatarName"    TEXT    NOT NULL,
    "AvatarEmoji"   TEXT    NOT NULL DEFAULT '🧒',
    "TotalScore"    INTEGER NOT NULL DEFAULT 0,
    "CreatedAt"     TEXT    NOT NULL,  -- ISO-8601 UTC timestamp
    "UpdatedAt"     TEXT    NOT NULL,  -- ISO-8601 UTC timestamp; set on every PUT
    "LastSyncedAt"  TEXT,              -- ISO-8601 UTC; echoed back to the MAUI app
    FOREIGN KEY ("UserAccountId") REFERENCES "UserAccount" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_CentralProfile_UserAccountId" ON "CentralProfile" ("UserAccountId");

-- ---------------------------------------------------------------------------
-- AppUsage
-- Optional event log for analytics / monitoring.
-- EventType examples: 'word_lookup', 'game_played', 'profile_synced'
-- EventData is free-form JSON (or NULL).
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "AppUsage" (
    "Id"               INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    "UserAccountId"    INTEGER NOT NULL,
    "CentralProfileId" INTEGER,            -- NULL for account-level events
    "EventType"        TEXT    NOT NULL,
    "EventData"        TEXT,              -- JSON payload, optional
    "CreatedAt"        TEXT    NOT NULL,  -- ISO-8601 UTC timestamp
    FOREIGN KEY ("UserAccountId") REFERENCES "UserAccount" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_AppUsage_UserAccountId" ON "AppUsage" ("UserAccountId");

-- ---------------------------------------------------------------------------
-- OtpRecord
-- Stores one-time-password codes. A code is valid until ExpiresAt and
-- IsUsed = 0.  Consumed on successful /api/auth/verify-otp.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "OtpRecord" (
    "Id"        INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    "Email"     TEXT    NOT NULL,
    "Code"      TEXT    NOT NULL,         -- 6-digit numeric string
    "ExpiresAt" TEXT    NOT NULL,         -- ISO-8601 UTC timestamp
    "IsUsed"    INTEGER NOT NULL DEFAULT 0,  -- 0 = unused, 1 = consumed
    "CreatedAt" TEXT    NOT NULL          -- ISO-8601 UTC timestamp
);

CREATE INDEX IF NOT EXISTS "IX_OtpRecord_Email" ON "OtpRecord" ("Email");
