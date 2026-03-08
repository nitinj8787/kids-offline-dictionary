using KidsDictionaryApp.Data;
using KidsDictionaryApp.Models;
using KidsDictionaryApp.Services.Interfaces;
using SQLite;

namespace KidsDictionaryApp.Services.Implementations
{
    public class ProgressService : IProgressService
    {
        private const int PointsPerNewWord = 10;
        private const int PointsPerRepeatWord = 2;
        private const int PointsPerGameComplete = 20;

        private readonly SQLiteAsyncConnection _db;

        public ProgressService(DictionaryDbContext context)
        {
            _db = context.Database;
        }

        public async Task RecordWordLookupAsync(int profileId, string wordText)
        {
            var existing = await _db.Table<ProfileWordProgress>()
                .Where(p => p.ProfileId == profileId && p.WordText == wordText)
                .FirstOrDefaultAsync();

            int points;
            if (existing == null)
            {
                await _db.InsertAsync(new ProfileWordProgress
                {
                    ProfileId = profileId,
                    WordText = wordText,
                    TimesLookedUp = 1,
                    LastAccessedAt = DateTime.UtcNow
                });
                points = PointsPerNewWord;
            }
            else
            {
                existing.TimesLookedUp++;
                existing.LastAccessedAt = DateTime.UtcNow;
                // Mark as learned after looking it up 3+ times
                if (existing.TimesLookedUp >= 3)
                    existing.IsLearned = true;
                await _db.UpdateAsync(existing);
                points = PointsPerRepeatWord;
            }

            await AddScoreAsync(profileId, points);
        }

        public async Task RecordGameScoreAsync(int profileId, string gameName, int score, int attempts, bool completed)
        {
            await _db.InsertAsync(new ProfileGameScore
            {
                ProfileId = profileId,
                GameName = gameName,
                Score = score,
                Attempts = attempts,
                Completed = completed,
                PlayedAt = DateTime.UtcNow
            });

            if (completed)
            {
                await AddScoreAsync(profileId, PointsPerGameComplete);
            }
        }

        public async Task<List<ProfileWordProgress>> GetWordProgressAsync(int profileId)
        {
            return await _db.Table<ProfileWordProgress>()
                .Where(p => p.ProfileId == profileId)
                .OrderByDescending(p => p.LastAccessedAt)
                .ToListAsync();
        }

        public async Task<List<ProfileGameScore>> GetGameScoresAsync(int profileId)
        {
            return await _db.Table<ProfileGameScore>()
                .Where(g => g.ProfileId == profileId)
                .OrderByDescending(g => g.PlayedAt)
                .ToListAsync();
        }

        public async Task<int> GetUniqueWordCountAsync(int profileId)
        {
            return await _db.Table<ProfileWordProgress>()
                .Where(p => p.ProfileId == profileId)
                .CountAsync();
        }

        public async Task<int> GetGamesCompletedCountAsync(int profileId)
        {
            return await _db.Table<ProfileGameScore>()
                .Where(g => g.ProfileId == profileId && g.Completed)
                .CountAsync();
        }

        public async Task AddScoreAsync(int profileId, int points)
        {
            var profile = await _db.Table<UserProfile>()
                .Where(p => p.Id == profileId)
                .FirstOrDefaultAsync();

            if (profile != null)
            {
                profile.TotalScore += points;
                await _db.UpdateAsync(profile);
            }
        }
    }
}
