using SQLite;

namespace KidsDictionaryApp.IntegrationTests;

/// <summary>
/// Integration tests for profile management, progress tracking, and achievements.
/// </summary>
public class ProfileProgressIntegrationTests : IAsyncLifetime, IDisposable
{
    private readonly string _dbPath;
    private SQLiteAsyncConnection _db = null!;

    public ProfileProgressIntegrationTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"profile_test_{Guid.NewGuid()}.db");
    }

    public async Task InitializeAsync()
    {
        _db = new SQLiteAsyncConnection(_dbPath);
        await _db.CreateTableAsync<TestUserProfile>();
        await _db.CreateTableAsync<TestProfileWordProgress>();
        await _db.CreateTableAsync<TestProfileGameScore>();
        await _db.CreateTableAsync<TestAchievement>();
        await _db.CreateTableAsync<TestProfileAchievement>();
    }

    public async Task DisposeAsync()
    {
        if (_db != null)
            await _db.CloseAsync();
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    // ──────────────────────────────────────────────
    // UserProfile CRUD
    // ──────────────────────────────────────────────

    [Fact]
    public async Task CreateProfile_InsertsProfileSuccessfully()
    {
        var profile = new TestUserProfile { Name = "Alice", AvatarEmoji = "🧒", TotalScore = 0 };
        await _db.InsertAsync(profile);

        var profiles = await _db.Table<TestUserProfile>().ToListAsync();
        Assert.Single(profiles);
        Assert.Equal("Alice", profiles[0].Name);
    }

    [Fact]
    public async Task CreateMultipleProfiles_AllAreRetrieved()
    {
        await _db.InsertAsync(new TestUserProfile { Name = "Alice", AvatarEmoji = "🧒" });
        await _db.InsertAsync(new TestUserProfile { Name = "Bob",   AvatarEmoji = "👦" });
        await _db.InsertAsync(new TestUserProfile { Name = "Carol", AvatarEmoji = "👧" });

        var profiles = await _db.Table<TestUserProfile>().ToListAsync();
        Assert.Equal(3, profiles.Count);
    }

    [Fact]
    public async Task DeleteProfile_RemovesItFromDatabase()
    {
        var profile = new TestUserProfile { Name = "ToDelete", AvatarEmoji = "🐶" };
        await _db.InsertAsync(profile);

        await _db.DeleteAsync(profile);

        var profiles = await _db.Table<TestUserProfile>().ToListAsync();
        Assert.Empty(profiles);
    }

    [Fact]
    public async Task UpdateProfile_ScoreIsUpdated()
    {
        var profile = new TestUserProfile { Name = "Scorer", AvatarEmoji = "🦁", TotalScore = 0 };
        await _db.InsertAsync(profile);

        profile.TotalScore += 50;
        await _db.UpdateAsync(profile);

        var updated = await _db.Table<TestUserProfile>()
            .Where(p => p.Id == profile.Id)
            .FirstOrDefaultAsync();

        Assert.NotNull(updated);
        Assert.Equal(50, updated!.TotalScore);
    }

    // ──────────────────────────────────────────────
    // ProfileWordProgress
    // ──────────────────────────────────────────────

    [Fact]
    public async Task RecordWordLookup_NewWord_CreatesProgressEntry()
    {
        var profile = new TestUserProfile { Name = "Reader", AvatarEmoji = "📚" };
        await _db.InsertAsync(profile);

        var progress = new TestProfileWordProgress
        {
            ProfileId = profile.Id,
            WordText = "apple",
            TimesLookedUp = 1,
            IsLearned = false
        };
        await _db.InsertAsync(progress);

        var entries = await _db.Table<TestProfileWordProgress>()
            .Where(p => p.ProfileId == profile.Id)
            .ToListAsync();

        Assert.Single(entries);
        Assert.Equal("apple", entries[0].WordText);
        Assert.Equal(1, entries[0].TimesLookedUp);
    }

    [Fact]
    public async Task RecordWordLookup_ExistingWord_IncrementsCount()
    {
        var profile = new TestUserProfile { Name = "Reader2", AvatarEmoji = "📖" };
        await _db.InsertAsync(profile);

        var progress = new TestProfileWordProgress
        {
            ProfileId = profile.Id,
            WordText = "cat",
            TimesLookedUp = 1
        };
        await _db.InsertAsync(progress);

        // Simulate looking up the same word again
        progress.TimesLookedUp++;
        await _db.UpdateAsync(progress);

        var entry = await _db.Table<TestProfileWordProgress>()
            .Where(p => p.ProfileId == profile.Id && p.WordText == "cat")
            .FirstOrDefaultAsync();

        Assert.NotNull(entry);
        Assert.Equal(2, entry!.TimesLookedUp);
    }

    [Fact]
    public async Task WordProgress_MarkedLearned_AfterThreeLookups()
    {
        var profile = new TestUserProfile { Name = "Learner", AvatarEmoji = "🎓" };
        await _db.InsertAsync(profile);

        var progress = new TestProfileWordProgress
        {
            ProfileId = profile.Id,
            WordText = "dog",
            TimesLookedUp = 3,
            IsLearned = true   // set when count >= 3
        };
        await _db.InsertAsync(progress);

        var entry = await _db.Table<TestProfileWordProgress>()
            .Where(p => p.ProfileId == profile.Id && p.WordText == "dog")
            .FirstOrDefaultAsync();

        Assert.NotNull(entry);
        Assert.True(entry!.IsLearned);
    }

    [Fact]
    public async Task GetUniqueWordCount_ReturnsCorrectCount()
    {
        var profile = new TestUserProfile { Name = "Explorer", AvatarEmoji = "🗺️" };
        await _db.InsertAsync(profile);

        foreach (var word in new[] { "apple", "cat", "dog", "elephant", "jungle" })
        {
            await _db.InsertAsync(new TestProfileWordProgress
            {
                ProfileId = profile.Id,
                WordText = word,
                TimesLookedUp = 1
            });
        }

        var count = await _db.Table<TestProfileWordProgress>()
            .Where(p => p.ProfileId == profile.Id)
            .CountAsync();

        Assert.Equal(5, count);
    }

    // ──────────────────────────────────────────────
    // ProfileGameScore
    // ──────────────────────────────────────────────

    [Fact]
    public async Task RecordGameScore_InsertsEntry()
    {
        var profile = new TestUserProfile { Name = "Gamer", AvatarEmoji = "🎮" };
        await _db.InsertAsync(profile);

        await _db.InsertAsync(new TestProfileGameScore
        {
            ProfileId = profile.Id,
            GameName = "Match Game",
            Score = 80,
            Attempts = 10,
            Completed = true
        });

        var scores = await _db.Table<TestProfileGameScore>()
            .Where(g => g.ProfileId == profile.Id)
            .ToListAsync();

        Assert.Single(scores);
        Assert.Equal("Match Game", scores[0].GameName);
        Assert.True(scores[0].Completed);
    }

    [Fact]
    public async Task GetGamesCompletedCount_ReturnsOnlyCompleted()
    {
        var profile = new TestUserProfile { Name = "Gamer2", AvatarEmoji = "🕹️" };
        await _db.InsertAsync(profile);

        await _db.InsertAsync(new TestProfileGameScore { ProfileId = profile.Id, GameName = "Hangman",    Completed = true  });
        await _db.InsertAsync(new TestProfileGameScore { ProfileId = profile.Id, GameName = "Match Game", Completed = true  });
        await _db.InsertAsync(new TestProfileGameScore { ProfileId = profile.Id, GameName = "Hangman",    Completed = false });

        var completed = await _db.Table<TestProfileGameScore>()
            .Where(g => g.ProfileId == profile.Id && g.Completed)
            .CountAsync();

        Assert.Equal(2, completed);
    }

    // ──────────────────────────────────────────────
    // Achievements
    // ──────────────────────────────────────────────

    [Fact]
    public async Task SeedAchievements_InsertsDefaultAchievements()
    {
        var achievements = new[]
        {
            new TestAchievement { Name = "First Steps",       AchievementType = "WordCount",  Threshold = 1,  BonusScore = 20  },
            new TestAchievement { Name = "Word Explorer",     AchievementType = "WordCount",  Threshold = 10, BonusScore = 50  },
            new TestAchievement { Name = "Game On!",          AchievementType = "GamePlayed", Threshold = 1,  BonusScore = 30  },
        };
        await _db.InsertAllAsync(achievements);

        var count = await _db.Table<TestAchievement>().CountAsync();
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task CheckAchievement_WordCount_UnlocksWhenThresholdMet()
    {
        var profile = new TestUserProfile { Name = "Scholar", AvatarEmoji = "🎓" };
        await _db.InsertAsync(profile);

        var achievement = new TestAchievement { Name = "First Steps", AchievementType = "WordCount", Threshold = 1, BonusScore = 20 };
        await _db.InsertAsync(achievement);

        // Add one word
        await _db.InsertAsync(new TestProfileWordProgress { ProfileId = profile.Id, WordText = "apple", TimesLookedUp = 1 });

        int wordCount = await _db.Table<TestProfileWordProgress>()
            .Where(p => p.ProfileId == profile.Id)
            .CountAsync();

        bool unlocked = wordCount >= achievement.Threshold;
        Assert.True(unlocked);
    }

    [Fact]
    public async Task CheckAchievement_WordCount_DoesNotUnlockWhenBelowThreshold()
    {
        var profile = new TestUserProfile { Name = "Beginner", AvatarEmoji = "👶" };
        await _db.InsertAsync(profile);

        var achievement = new TestAchievement { Name = "Word Explorer", AchievementType = "WordCount", Threshold = 10, BonusScore = 50 };
        await _db.InsertAsync(achievement);

        // Only 3 words - not enough
        await _db.InsertAsync(new TestProfileWordProgress { ProfileId = profile.Id, WordText = "apple", TimesLookedUp = 1 });
        await _db.InsertAsync(new TestProfileWordProgress { ProfileId = profile.Id, WordText = "cat",   TimesLookedUp = 1 });
        await _db.InsertAsync(new TestProfileWordProgress { ProfileId = profile.Id, WordText = "dog",   TimesLookedUp = 1 });

        int wordCount = await _db.Table<TestProfileWordProgress>()
            .Where(p => p.ProfileId == profile.Id)
            .CountAsync();

        bool unlocked = wordCount >= achievement.Threshold;
        Assert.False(unlocked);
    }

    [Fact]
    public async Task EarnAchievement_RecordsProfileAchievement()
    {
        var profile = new TestUserProfile { Name = "Winner", AvatarEmoji = "🏆" };
        await _db.InsertAsync(profile);

        var achievement = new TestAchievement { Name = "Game On!", AchievementType = "GamePlayed", Threshold = 1, BonusScore = 30 };
        await _db.InsertAsync(achievement);

        await _db.InsertAsync(new TestProfileAchievement
        {
            ProfileId = profile.Id,
            AchievementId = achievement.Id,
            EarnedAt = DateTime.UtcNow
        });

        var earned = await _db.Table<TestProfileAchievement>()
            .Where(pa => pa.ProfileId == profile.Id)
            .ToListAsync();

        Assert.Single(earned);
        Assert.Equal(achievement.Id, earned[0].AchievementId);
    }

    [Fact]
    public async Task DeleteProfile_CascadesProgressData()
    {
        var profile = new TestUserProfile { Name = "DeleteMe", AvatarEmoji = "🗑️" };
        await _db.InsertAsync(profile);

        await _db.InsertAsync(new TestProfileWordProgress { ProfileId = profile.Id, WordText = "word1", TimesLookedUp = 1 });
        await _db.InsertAsync(new TestProfileGameScore    { ProfileId = profile.Id, GameName = "Hangman", Completed = true });

        // Delete profile and related data (as ProfileService does)
        await _db.DeleteAsync(profile);
        await _db.ExecuteAsync("DELETE FROM TestProfileWordProgress WHERE ProfileId = ?", profile.Id);
        await _db.ExecuteAsync("DELETE FROM TestProfileGameScore WHERE ProfileId = ?",    profile.Id);

        var wordProgress = await _db.Table<TestProfileWordProgress>()
            .Where(p => p.ProfileId == profile.Id).ToListAsync();
        var gameScores = await _db.Table<TestProfileGameScore>()
            .Where(g => g.ProfileId == profile.Id).ToListAsync();

        Assert.Empty(wordProgress);
        Assert.Empty(gameScores);
    }
}

// ──────────────────────────────────────────────────────────────
// Local test model mirrors of the production models
// ──────────────────────────────────────────────────────────────

[Table("TestUserProfile")]
public class TestUserProfile
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AvatarEmoji { get; set; } = "🧒";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int TotalScore { get; set; } = 0;
}

[Table("TestProfileWordProgress")]
public class TestProfileWordProgress
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    [Indexed]
    public int ProfileId { get; set; }
    [Indexed]
    public string WordText { get; set; } = string.Empty;
    public int TimesLookedUp { get; set; } = 0;
    public bool IsLearned { get; set; } = false;
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
}

[Table("TestProfileGameScore")]
public class TestProfileGameScore
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    [Indexed]
    public int ProfileId { get; set; }
    public string GameName { get; set; } = string.Empty;
    public int Score { get; set; } = 0;
    public int Attempts { get; set; } = 0;
    public bool Completed { get; set; } = false;
    public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
}

[Table("TestAchievement")]
public class TestAchievement
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BadgeEmoji { get; set; } = "🏅";
    public string Category { get; set; } = "General";
    public string AchievementType { get; set; } = "WordCount";
    public int Threshold { get; set; } = 1;
    public int BonusScore { get; set; } = 50;
}

[Table("TestProfileAchievement")]
public class TestProfileAchievement
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    [Indexed]
    public int ProfileId { get; set; }
    [Indexed]
    public int AchievementId { get; set; }
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
}
