using KidsDictionaryApp.Data;
using KidsDictionaryApp.Models;
using KidsDictionaryApp.Services.Interfaces;
using SQLite;

namespace KidsDictionaryApp.Services.Implementations
{
    public class ProfileService : IProfileService
    {
        private readonly SQLiteAsyncConnection _db;
        private int _initializationFlag = 0; // 0 = not yet started, 1 = started

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

            // Auto-set as active when this is the very first profile
            if (ActiveProfile == null)
            {
                var count = await _db.Table<UserProfile>().CountAsync();
                if (count == 1)
                {
                    ActiveProfile = profile;
                    ActiveProfileChanged?.Invoke();
                }
            }

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
                }

                // Auto-select if exactly one profile remains and none is currently active
                var remaining = await GetProfilesAsync();
                if (remaining.Count == 1 && ActiveProfile == null)
                {
                    ActiveProfile = remaining[0];
                }

                ActiveProfileChanged?.Invoke();
            }
        }

        public void SetActiveProfile(UserProfile? profile)
        {
            ActiveProfile = profile;
            ActiveProfileChanged?.Invoke();
        }

        /// <inheritdoc />
        public async Task InitializeAsync()
        {
            // Use an atomic compare-and-swap so that only the first caller runs the body,
            // even if multiple components initialise concurrently.
            if (Interlocked.CompareExchange(ref _initializationFlag, 1, 0) != 0) return;

            if (ActiveProfile == null)
            {
                var profiles = await GetProfilesAsync();
                if (profiles.Count == 1)
                {
                    ActiveProfile = profiles[0];
                    ActiveProfileChanged?.Invoke();
                }
            }
        }
    }
}
