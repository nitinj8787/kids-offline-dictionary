using KidsDictionaryApp.Models;

namespace KidsDictionaryApp.Services.Interfaces
{
    /// <summary>
    /// Provides synchronization between the local offline profile store and the
    /// centralized profile management API.
    /// </summary>
    public interface ISyncService
    {
        /// <summary>
        /// Requests a one-time password to be sent to the given parent email.
        /// Returns true when the request was accepted by the server.
        /// </summary>
        Task<bool> RequestOtpAsync(string email);

        /// <summary>
        /// Verifies the OTP entered by the parent. On success, stores the JWT token
        /// returned by the server for subsequent authenticated calls.
        /// Returns true on success.
        /// </summary>
        Task<bool> VerifyOtpAsync(string email, string code);

        /// <summary>
        /// Synchronizes the given local profile to the central server.
        /// Creates a new remote profile if <see cref="UserProfile.RemoteId"/> is null,
        /// or updates the existing one.
        /// Updates <see cref="UserProfile.RemoteId"/> and <see cref="UserProfile.LastSyncedAt"/>
        /// on success, then persists those changes locally.
        /// </summary>
        Task<SyncResult> SyncProfileAsync(UserProfile profile);

        /// <summary>True when a valid JWT token is available for authenticated API calls.</summary>
        bool IsAuthenticated { get; }

        /// <summary>The parent email associated with the current session token, if authenticated.</summary>
        string? AuthenticatedEmail { get; }
    }

    public record SyncResult(bool Success, string? ErrorMessage = null);
}
