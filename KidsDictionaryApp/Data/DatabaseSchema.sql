-- ============================================================
-- Kids Dictionary – Full Database Schema
-- ============================================================
-- Existing tables (reference)
-- ============================================================

CREATE TABLE IF NOT EXISTS "FavoriteWord" (
	"Id"	integer NOT NULL,
	"WordText"	varchar,
	"AddedAt"	bigint,
	PRIMARY KEY("Id" AUTOINCREMENT)
);

CREATE TABLE IF NOT EXISTS "Word" (
	"Id"	integer NOT NULL,
	"WordText"	varchar,
	"PartOfSpeech"	varchar,
	"Category"	varchar,
	"Meaning"	varchar,
	"Example"	varchar,
	"Phonics"	varchar,
	"Syllables"	varchar,
	"Synonyms"	varchar,
	"Antonyms"	varchar,
	"DifficultyLevel"	varchar,
	"FrequencyRank"	integer,
	PRIMARY KEY("Id" AUTOINCREMENT)
);

CREATE TABLE IF NOT EXISTS "WordHistory" (
	"Id"	integer NOT NULL,
	"WordText"	varchar,
	"LookedUpAt"	bigint,
	PRIMARY KEY("Id" AUTOINCREMENT)
);

-- ============================================================
-- New tables added for user profiles, progress & achievements
-- ============================================================

CREATE TABLE IF NOT EXISTS "UserProfile" (
	"Id"	integer NOT NULL,
	"Name"	varchar,
	"AvatarEmoji"	varchar,
	"CreatedAt"	bigint,
	"TotalScore"	integer,
	PRIMARY KEY("Id" AUTOINCREMENT)
);

CREATE TABLE IF NOT EXISTS "ProfileWordProgress" (
	"Id"	integer NOT NULL,
	"ProfileId"	integer,
	"WordText"	varchar,
	"TimesLookedUp"	integer,
	"IsLearned"	integer,
	"LastAccessedAt"	bigint,
	PRIMARY KEY("Id" AUTOINCREMENT)
);

CREATE INDEX IF NOT EXISTS "IX_ProfileWordProgress_ProfileId"
	ON "ProfileWordProgress" ("ProfileId");

CREATE INDEX IF NOT EXISTS "IX_ProfileWordProgress_WordText"
	ON "ProfileWordProgress" ("WordText");

CREATE TABLE IF NOT EXISTS "ProfileGameScore" (
	"Id"	integer NOT NULL,
	"ProfileId"	integer,
	"GameName"	varchar,
	"Score"	integer,
	"Attempts"	integer,
	"Completed"	integer,
	"PlayedAt"	bigint,
	PRIMARY KEY("Id" AUTOINCREMENT)
);

CREATE INDEX IF NOT EXISTS "IX_ProfileGameScore_ProfileId"
	ON "ProfileGameScore" ("ProfileId");

CREATE TABLE IF NOT EXISTS "Achievement" (
	"Id"	integer NOT NULL,
	"Name"	varchar,
	"Description"	varchar,
	"BadgeEmoji"	varchar,
	"Category"	varchar,
	"AchievementType"	varchar,
	"Threshold"	integer,
	"BonusScore"	integer,
	PRIMARY KEY("Id" AUTOINCREMENT)
);

CREATE TABLE IF NOT EXISTS "ProfileAchievement" (
	"Id"	integer NOT NULL,
	"ProfileId"	integer,
	"AchievementId"	integer,
	"EarnedAt"	bigint,
	PRIMARY KEY("Id" AUTOINCREMENT)
);

CREATE INDEX IF NOT EXISTS "IX_ProfileAchievement_ProfileId"
	ON "ProfileAchievement" ("ProfileId");

CREATE INDEX IF NOT EXISTS "IX_ProfileAchievement_AchievementId"
	ON "ProfileAchievement" ("AchievementId");
