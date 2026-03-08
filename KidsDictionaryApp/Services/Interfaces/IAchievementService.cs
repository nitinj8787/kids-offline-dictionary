using KidsDictionaryApp.Models;

namespace KidsDictionaryApp.Services.Interfaces
{
    public interface IAchievementService
    {
        /// <summary>
        /// Returns all achievement definitions.
        /// </summary>
        Task<List<Achievement>> GetAllAchievementsAsync();

        /// <summary>
        /// Returns achievements earned by a specific profile.
        /// </summary>
        Task<List<ProfileAchievement>> GetEarnedAchievementsAsync(int profileId);

        /// <summary>
        /// Checks and awards any newly unlocked achievements for the profile.
        /// Returns the list of newly earned achievements (empty if none).
        /// </summary>
        Task<List<Achievement>> CheckAndAwardAchievementsAsync(int profileId);

        /// <summary>
        /// Seeds the default achievement definitions if not already present.
        /// </summary>
        Task SeedAchievementsAsync();
    }
}
