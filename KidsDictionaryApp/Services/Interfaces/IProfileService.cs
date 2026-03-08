using KidsDictionaryApp.Models;

namespace KidsDictionaryApp.Services.Interfaces
{
    public interface IProfileService
    {
        Task<List<UserProfile>> GetProfilesAsync();
        Task<UserProfile?> GetProfileAsync(int id);
        Task<UserProfile> CreateProfileAsync(string name, string avatarEmoji);
        Task UpdateProfileAsync(UserProfile profile);
        Task DeleteProfileAsync(int id);

        /// <summary>
        /// Gets the currently active profile (null if none selected).
        /// </summary>
        UserProfile? ActiveProfile { get; }

        /// <summary>
        /// Sets the active profile for the current session.
        /// </summary>
        void SetActiveProfile(UserProfile? profile);

        event Action? ActiveProfileChanged;
    }
}
