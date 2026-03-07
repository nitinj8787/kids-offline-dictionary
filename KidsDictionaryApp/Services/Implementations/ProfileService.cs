using KidsDictionaryApp.Data;
using KidsDictionaryApp.Models;
using KidsDictionaryApp.Services.Interfaces;
using SQLite;

namespace KidsDictionaryApp.Services.Implementations
{
    public class ProfileService : IProfileService
    {
        private readonly SQLiteAsyncConnection _db;

        public UserProfile? ActiveProfile { get; private set; }

        public event Action? ActiveProfileChanged;

        public ProfileService(DictionaryDbContext context)
        {
            _db = context.Database;
        }

        public async Task<List<UserProfile>> GetProfilesAsync()
        {
            return await _db.Table<UserProfile>()
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<UserProfile?> GetProfileAsync(int id)
        {
            return await _db.Table<UserProfile>()
                .Where(p => p.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<UserProfile> CreateProfileAsync(string name, string avatarEmoji)
        {
            var profile = new UserProfile
            {
                Name = name.Trim(),
                AvatarEmoji = avatarEmoji,
                CreatedAt = DateTime.UtcNow,
                TotalScore = 0
            };
            await _db.InsertAsync(profile);
            return profile;
        }

        public async Task UpdateProfileAsync(UserProfile profile)
        {
            await _db.UpdateAsync(profile);
            if (ActiveProfile?.Id == profile.Id)
            {
                ActiveProfile = profile;
                ActiveProfileChanged?.Invoke();
            }
        }

        public async Task DeleteProfileAsync(int id)
        {
            var profile = await _db.Table<UserProfile>().Where(p => p.Id == id).FirstOrDefaultAsync();
            if (profile != null)
            {
                await _db.DeleteAsync(profile);
                // Also delete related data
                await _db.ExecuteAsync("DELETE FROM ProfileWordProgress WHERE ProfileId = ?", id);
                await _db.ExecuteAsync("DELETE FROM ProfileGameScore WHERE ProfileId = ?", id);
                await _db.ExecuteAsync("DELETE FROM ProfileAchievement WHERE ProfileId = ?", id);

                if (ActiveProfile?.Id == id)
                {
                    ActiveProfile = null;
                    ActiveProfileChanged?.Invoke();
                }
            }
        }

        public void SetActiveProfile(UserProfile? profile)
        {
            ActiveProfile = profile;
            ActiveProfileChanged?.Invoke();
        }
    }
}
