using System.Net.Http.Json;
using System.Text.Json.Serialization;
using KidsDictionaryApp.Models;
using KidsDictionaryApp.Services.Interfaces;

namespace KidsDictionaryApp.Services.Implementations
{
    /// <summary>
    /// Synchronizes local offline profiles with the centralized Kids Dictionary API.
    /// All network calls are best-effort: failures are caught and reported without
    /// crashing the app, preserving the offline-first guarantee.
    /// </summary>
    public class SyncService : ISyncService
    {
        private readonly HttpClient _http;
        private readonly IProfileService _profileService;
        private string? _jwtToken;

        public bool IsAuthenticated => !string.IsNullOrEmpty(_jwtToken);
        public string? AuthenticatedEmail { get; private set; }

        public SyncService(HttpClient http, IProfileService profileService)
        {
            _http = http;
            _profileService = profileService;
        }

        public async Task<bool> RequestOtpAsync(string email)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("/api/auth/request-otp",
                    new { email });
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> VerifyOtpAsync(string email, string code)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("/api/auth/verify-otp",
                    new { email, code });

                if (!response.IsSuccessStatusCode) return false;

                var body = await response.Content.ReadFromJsonAsync<VerifyOtpResponse>();
                if (body?.Token == null) return false;

                _jwtToken = body.Token;
                AuthenticatedEmail = email;
                _http.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _jwtToken);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<SyncResult> SyncProfileAsync(UserProfile profile)
        {
            if (!IsAuthenticated)
                return new SyncResult(false, "Not authenticated. Please verify your OTP first.");

            try
            {
                HttpResponseMessage response;

                if (profile.RemoteId == null)
                {
                    // Create new remote profile
                    response = await _http.PostAsJsonAsync("/api/profiles", new
                    {
                        avatarName = profile.Name,
                        avatarEmoji = profile.AvatarEmoji,
                        totalScore = profile.TotalScore
                    });
                }
                else
                {
                    // Update existing remote profile
                    response = await _http.PutAsJsonAsync($"/api/profiles/{profile.RemoteId}", new
                    {
                        avatarName = profile.Name,
                        avatarEmoji = profile.AvatarEmoji,
                        totalScore = (int?)profile.TotalScore
                    });
                }

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return new SyncResult(false, $"Sync failed ({response.StatusCode}): {error}");
                }

                var remoteProfile = await response.Content.ReadFromJsonAsync<RemoteProfileResponse>();
                if (remoteProfile == null)
                    return new SyncResult(false, "Invalid response from server.");

                // Persist the remote ID and sync timestamp back to the local profile
                profile.RemoteId = remoteProfile.Id;
                profile.LastSyncedAt = DateTime.UtcNow;
                await _profileService.UpdateProfileAsync(profile);

                return new SyncResult(true);
            }
            catch (Exception ex)
            {
                return new SyncResult(false, $"Network error: {ex.Message}");
            }
        }

        // ── Response DTOs ────────────────────────────────────────────────────
        private record VerifyOtpResponse(
            [property: JsonPropertyName("token")] string Token,
            [property: JsonPropertyName("userAccountId")] int UserAccountId,
            [property: JsonPropertyName("email")] string Email);

        private record RemoteProfileResponse(
            [property: JsonPropertyName("id")] int Id,
            [property: JsonPropertyName("avatarName")] string AvatarName,
            [property: JsonPropertyName("avatarEmoji")] string AvatarEmoji,
            [property: JsonPropertyName("totalScore")] int TotalScore,
            [property: JsonPropertyName("lastSyncedAt")] DateTime? LastSyncedAt);
    }
}
