using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using KidsDictionaryApi.Data;
using KidsDictionaryApi.Endpoints;

namespace KidsDictionaryApi.Tests;

/// <summary>
/// Integration tests for the Auth and Profile API endpoints.
/// Uses an isolated SQLite file database per test session.
/// </summary>
public class ApiIntegrationTests : IAsyncLifetime
{
    private readonly string _dbPath;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public ApiIntegrationTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"api_test_{Guid.NewGuid()}.db");
    }

    public Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace the ApiDbContext singleton with one pointing at a unique
                // test-specific SQLite file so each test run is fully isolated.
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(ApiDbContext));
                if (descriptor != null) services.Remove(descriptor);

                services.AddSingleton(new ApiDbContext($"Data Source={_dbPath}"));
            });
        });

        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    // ──────────────────────────────────────────────
    // Health endpoint
    // ──────────────────────────────────────────────

    [Fact]
    public async Task HealthEndpoint_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ──────────────────────────────────────────────
    // Auth: Request OTP
    // ──────────────────────────────────────────────

    [Fact]
    public async Task RequestOtp_ValidEmail_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/request-otp",
            new { email = "parent@example.com" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RequestOtpResponse>();
        Assert.NotNull(body);
        Assert.Contains("OTP", body!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RequestOtp_InvalidEmail_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/request-otp",
            new { email = "not-an-email" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RequestOtp_EmptyEmail_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/request-otp",
            new { email = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RequestOtp_ReturnsOtpInDevMode()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/request-otp",
            new { email = "dev@example.com" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RequestOtpResponse>();
        Assert.NotNull(body);
        // In dev mode (ReturnOtpInResponse: true in appsettings.Development.json)
        // the OTP is exposed; if null it means production mode is active — both are valid.
        if (body!.Otp != null)
        {
            Assert.Equal(6, body.Otp.Length);
            Assert.True(body.Otp.All(char.IsDigit));
        }
    }

    // ──────────────────────────────────────────────
    // Auth: Verify OTP
    // ──────────────────────────────────────────────

    [Fact]
    public async Task VerifyOtp_ValidCode_ReturnsToken()
    {
        const string email = "verify@example.com";

        // 1. Request OTP
        var reqResponse = await _client.PostAsJsonAsync("/api/auth/request-otp",
            new { email });
        var reqBody = await reqResponse.Content.ReadFromJsonAsync<RequestOtpResponse>();
        Assert.NotNull(reqBody?.Otp);

        // 2. Verify OTP
        var verifyResponse = await _client.PostAsJsonAsync("/api/auth/verify-otp",
            new { email, code = reqBody!.Otp });
        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);

        var verifyBody = await verifyResponse.Content.ReadFromJsonAsync<VerifyOtpResponse>();
        Assert.NotNull(verifyBody);
        Assert.NotEmpty(verifyBody!.Token);
    }

    [Fact]
    public async Task VerifyOtp_WrongCode_ReturnsUnprocessableEntity()
    {
        const string email = "wrong@example.com";
        await _client.PostAsJsonAsync("/api/auth/request-otp", new { email });

        var response = await _client.PostAsJsonAsync("/api/auth/verify-otp",
            new { email, code = "000000" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    // ──────────────────────────────────────────────
    // Profiles: Authenticated CRUD
    // ──────────────────────────────────────────────

    private async Task<string> AuthenticateAsync(string email)
    {
        await _client.PostAsJsonAsync("/api/auth/request-otp", new { email });
        var reqBody = await (await _client.PostAsJsonAsync("/api/auth/request-otp",
            new { email })).Content.ReadFromJsonAsync<RequestOtpResponse>();

        var verifyResponse = await _client.PostAsJsonAsync("/api/auth/verify-otp",
            new { email, code = reqBody!.Otp });
        var verifyBody = await verifyResponse.Content.ReadFromJsonAsync<VerifyOtpResponse>();
        return verifyBody!.Token;
    }

    [Fact]
    public async Task GetProfiles_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/profiles");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProfiles_Authenticated_ReturnsEmptyList()
    {
        var token = await AuthenticateAsync("getprofiles@example.com");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/profiles");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var profiles = await response.Content.ReadFromJsonAsync<List<ProfileEndpoints.ProfileDto>>();
        Assert.NotNull(profiles);
        Assert.Empty(profiles!);
    }

    [Fact]
    public async Task CreateProfile_ValidData_ReturnsCreated()
    {
        var token = await AuthenticateAsync("create@example.com");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/profiles",
            new { avatarName = "SuperStar", avatarEmoji = "🌟", totalScore = 0 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var profile = await response.Content.ReadFromJsonAsync<ProfileEndpoints.ProfileDto>();
        Assert.NotNull(profile);
        Assert.Equal("SuperStar", profile!.AvatarName);
        Assert.Equal("🌟", profile.AvatarEmoji);
    }

    [Fact]
    public async Task CreateProfile_EmptyName_ReturnsBadRequest()
    {
        var token = await AuthenticateAsync("badprofile@example.com");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/profiles",
            new { avatarName = "", avatarEmoji = "🧒", totalScore = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateAndGetProfile_RoundTrip()
    {
        var token = await AuthenticateAsync("roundtrip@example.com");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Create
        await _client.PostAsJsonAsync("/api/profiles",
            new { avatarName = "Dino", avatarEmoji = "🦕", totalScore = 50 });

        // List
        var listResponse = await _client.GetAsync("/api/profiles");
        var profiles = await listResponse.Content.ReadFromJsonAsync<List<ProfileEndpoints.ProfileDto>>();

        Assert.NotNull(profiles);
        Assert.Single(profiles!);
        Assert.Equal("Dino", profiles![0].AvatarName);
        Assert.Equal(50, profiles![0].TotalScore);
    }

    [Fact]
    public async Task UpdateProfile_ChangesScore()
    {
        var token = await AuthenticateAsync("update@example.com");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Create
        var createResponse = await _client.PostAsJsonAsync("/api/profiles",
            new { avatarName = "Rocket", avatarEmoji = "🚀", totalScore = 10 });
        var created = await createResponse.Content.ReadFromJsonAsync<ProfileEndpoints.ProfileDto>();

        // Update
        var updateResponse = await _client.PutAsJsonAsync($"/api/profiles/{created!.Id}",
            new { avatarName = (string?)null, avatarEmoji = (string?)null, totalScore = 200 });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await updateResponse.Content.ReadFromJsonAsync<ProfileEndpoints.ProfileDto>();
        Assert.Equal(200, updated!.TotalScore);
        Assert.Equal("Rocket", updated.AvatarName); // unchanged
    }

    [Fact]
    public async Task DeleteProfile_RemovesIt()
    {
        var token = await AuthenticateAsync("delete@example.com");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Create
        var createResponse = await _client.PostAsJsonAsync("/api/profiles",
            new { avatarName = "Ghost", avatarEmoji = "👻", totalScore = 0 });
        var created = await createResponse.Content.ReadFromJsonAsync<ProfileEndpoints.ProfileDto>();

        // Delete
        var deleteResponse = await _client.DeleteAsync($"/api/profiles/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify gone
        var listResponse = await _client.GetAsync("/api/profiles");
        var profiles = await listResponse.Content.ReadFromJsonAsync<List<ProfileEndpoints.ProfileDto>>();
        Assert.Empty(profiles!);
    }

    [Fact]
    public async Task UserCannotAccessOtherUsersProfiles()
    {
        // User A creates a profile
        var tokenA = await AuthenticateAsync("usera@example.com");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);
        var createResponse = await _client.PostAsJsonAsync("/api/profiles",
            new { avatarName = "Private", avatarEmoji = "🔒", totalScore = 0 });
        var created = await createResponse.Content.ReadFromJsonAsync<ProfileEndpoints.ProfileDto>();

        // User B tries to get User A's profile
        var tokenB = await AuthenticateAsync("userb@example.com");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenB);

        var getResponse = await _client.GetAsync($"/api/profiles/{created!.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    // ──────────────────────────────────────────────
    // Usage tracking
    // ──────────────────────────────────────────────

    [Fact]
    public async Task RecordUsage_ValidEvent_ReturnsOk()
    {
        var token = await AuthenticateAsync("usage@example.com");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/usage",
            new { eventType = "WordLookup", centralProfileId = (int?)null, eventData = "{\"word\":\"apple\"}" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetUsage_ReturnsRecordedEvents()
    {
        var token = await AuthenticateAsync("getusage@example.com");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsJsonAsync("/api/usage",
            new { eventType = "GamePlayed", centralProfileId = (int?)null, eventData = "{\"game\":\"Hangman\"}" });

        var response = await _client.GetAsync("/api/usage");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var usages = await response.Content.ReadFromJsonAsync<List<UsageEndpoints.UsageDto>>();
        Assert.NotNull(usages);
        Assert.Single(usages!);
        Assert.Equal("GamePlayed", usages![0].EventType);
    }

    // ──────────────────────────────────────────────
    // Response DTOs for deserialization
    // ──────────────────────────────────────────────

    private record RequestOtpResponse(string Message, string? Otp);
    private record VerifyOtpResponse(string Token, int UserAccountId, string Email);
}
