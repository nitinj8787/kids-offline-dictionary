using KidsDictionaryApp.Data;
using KidsDictionaryApp.Models;
using KidsDictionaryApp.Services.Interfaces;
using SQLite;

namespace KidsDictionaryApp.Services.Implementations
{
    public class AchievementService : IAchievementService
    {
        private readonly SQLiteAsyncConnection _db;

        private static readonly List<Achievement> DefaultAchievements = new()
        {
            new Achievement { Name = "First Steps",       Description = "Look up your first word",          BadgeEmoji = "👣", Category = "Learning",  AchievementType = "WordCount",  Threshold = 1,  BonusScore = 20  },
            new Achievement { Name = "Word Explorer",     Description = "Look up 10 different words",       BadgeEmoji = "🗺️", Category = "Learning",  AchievementType = "WordCount",  Threshold = 10, BonusScore = 50  },
            new Achievement { Name = "Bookworm",          Description = "Look up 25 different words",       BadgeEmoji = "📚", Category = "Learning",  AchievementType = "WordCount",  Threshold = 25, BonusScore = 100 },
            new Achievement { Name = "Dictionary Master", Description = "Look up 50 different words",       BadgeEmoji = "🎓", Category = "Learning",  AchievementType = "WordCount",  Threshold = 50, BonusScore = 200 },
            new Achievement { Name = "Game On!",          Description = "Complete your first game",         BadgeEmoji = "🎮", Category = "Gaming",    AchievementType = "GamePlayed", Threshold = 1,  BonusScore = 30  },
            new Achievement { Name = "Game Enthusiast",   Description = "Complete 5 games",                 BadgeEmoji = "🕹️", Category = "Gaming",    AchievementType = "GamePlayed", Threshold = 5,  BonusScore = 80  },
            new Achievement { Name = "Champion",          Description = "Complete 20 games",                BadgeEmoji = "🏆", Category = "Gaming",    AchievementType = "GamePlayed", Threshold = 20, BonusScore = 200 },
            new Achievement { Name = "High Scorer",       Description = "Reach a total score of 100",      BadgeEmoji = "⭐", Category = "Score",     AchievementType = "TotalScore", Threshold = 100,  BonusScore = 50  },
            new Achievement { Name = "Star Student",      Description = "Reach a total score of 500",      BadgeEmoji = "🌟", Category = "Score",     AchievementType = "TotalScore", Threshold = 500,  BonusScore = 100 },
            new Achievement { Name = "Legend",            Description = "Reach a total score of 1000",     BadgeEmoji = "🦁", Category = "Score",     AchievementType = "TotalScore", Threshold = 1000, BonusScore = 300 },
        };

        public AchievementService(DictionaryDbContext context)
        {
            _db = context.Database;
        }

        public async Task SeedAchievementsAsync()
        {
            var existing = await _db.Table<Achievement>().CountAsync();
            if (existing == 0)
            {
                await _db.InsertAllAsync(DefaultAchievements);
            }
        }

        public async Task<List<Achievement>> GetAllAchievementsAsync()
        {
            return await _db.Table<Achievement>()
                .OrderBy(a => a.Threshold)
                .ToListAsync();
        }

        public async Task<List<ProfileAchievement>> GetEarnedAchievementsAsync(int profileId)
        {
            return await _db.Table<ProfileAchievement>()
                .Where(pa => pa.ProfileId == profileId)
                .ToListAsync();
        }

        public async Task<List<Achievement>> CheckAndAwardAchievementsAsync(int profileId)
        {
            var allAchievements = await GetAllAchievementsAsync();
            var earned = await GetEarnedAchievementsAsync(profileId);
            var earnedIds = earned.Select(e => e.AchievementId).ToHashSet();

            var profile = await _db.Table<UserProfile>()
                .Where(p => p.Id == profileId)
                .FirstOrDefaultAsync();

            if (profile == null)
                return new List<Achievement>();

            int wordCount = await _db.Table<ProfileWordProgress>()
                .Where(p => p.ProfileId == profileId)
                .CountAsync();

            int gamesCompleted = await _db.Table<ProfileGameScore>()
                .Where(g => g.ProfileId == profileId && g.Completed)
                .CountAsync();

            var newlyEarned = new List<Achievement>();

            foreach (var achievement in allAchievements)
            {
                if (earnedIds.Contains(achievement.Id))
                    continue;

                bool unlocked = achievement.AchievementType switch
                {
                    "WordCount"  => wordCount >= achievement.Threshold,
                    "GamePlayed" => gamesCompleted >= achievement.Threshold,
                    "TotalScore" => profile.TotalScore >= achievement.Threshold,
                    _            => false
                };

                if (unlocked)
                {
                    await _db.InsertAsync(new ProfileAchievement
                    {
                        ProfileId = profileId,
                        AchievementId = achievement.Id,
                        EarnedAt = DateTime.UtcNow
                    });

                    // Award bonus score
                    profile.TotalScore += achievement.BonusScore;
                    await _db.UpdateAsync(profile);

                    newlyEarned.Add(achievement);
                }
            }

            return newlyEarned;
        }
    }
}
