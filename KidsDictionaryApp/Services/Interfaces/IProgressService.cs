using KidsDictionaryApp.Models;

namespace KidsDictionaryApp.Services.Interfaces
{
    public interface IProgressService
    {
        /// <summary>
        /// Records a word lookup for the given profile and awards points.
        /// </summary>
        Task RecordWordLookupAsync(int profileId, string wordText);

        /// <summary>
        /// Records a game session result for the given profile.
        /// </summary>
        Task RecordGameScoreAsync(int profileId, string gameName, int score, int attempts, bool completed);

        /// <summary>
        /// Gets all word progress entries for a profile.
        /// </summary>
        Task<List<ProfileWordProgress>> GetWordProgressAsync(int profileId);

        /// <summary>
        /// Gets game scores for a profile.
        /// </summary>
        Task<List<ProfileGameScore>> GetGameScoresAsync(int profileId);

        /// <summary>
        /// Returns the total number of unique words looked up by a profile.
        /// </summary>
        Task<int> GetUniqueWordCountAsync(int profileId);

        /// <summary>
        /// Returns the total number of games completed by a profile.
        /// </summary>
        Task<int> GetGamesCompletedCountAsync(int profileId);

        /// <summary>
        /// Adds score points directly to a profile's TotalScore.
        /// </summary>
        Task AddScoreAsync(int profileId, int points);
    }
}
