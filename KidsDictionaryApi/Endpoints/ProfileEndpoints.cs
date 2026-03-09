using System.Security.Claims;
using Dapper;
using KidsDictionaryApi.Data;
using KidsDictionaryApi.Models;

namespace KidsDictionaryApi.Endpoints
{
    public static class ProfileEndpoints
    {
        public static void MapProfileEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/profiles")
                .WithTags("Profiles")
                .RequireAuthorization();

            // GET /api/profiles — list all profiles for the authenticated account
            group.MapGet("/", GetProfiles).WithName("GetProfiles");

            // GET /api/profiles/{id} — get a single profile
            group.MapGet("/{id:int}", GetProfile).WithName("GetProfile");

            // POST /api/profiles — create a new profile
            group.MapPost("/", CreateProfile).WithName("CreateProfile");

            // PUT /api/profiles/{id} — update a profile (score sync)
            group.MapPut("/{id:int}", UpdateProfile).WithName("UpdateProfile");

            // DELETE /api/profiles/{id} — remove a profile
            group.MapDelete("/{id:int}", DeleteProfile).WithName("DeleteProfile");
        }

        private static int? GetUserAccountId(ClaimsPrincipal user)
        {
            var claim = user.FindFirst("userAccountId") ?? user.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
        }

        private static async Task<IResult> GetProfiles(ClaimsPrincipal user, ApiDbContext db)
        {
            var accountId = GetUserAccountId(user);
            if (accountId == null) return Results.Unauthorized();

            using var conn = db.CreateConnection();
            var profiles = await conn.QueryAsync<CentralProfile>(
                "SELECT * FROM CentralProfile WHERE UserAccountId = @AccountId ORDER BY CreatedAt",
                new { AccountId = accountId });

            return Results.Ok(profiles.Select(p =>
                new ProfileDto(p.Id, p.AvatarName, p.AvatarEmoji, p.TotalScore, p.LastSyncedAt)));
        }

        private static async Task<IResult> GetProfile(int id, ClaimsPrincipal user, ApiDbContext db)
        {
            var accountId = GetUserAccountId(user);
            if (accountId == null) return Results.Unauthorized();

            using var conn = db.CreateConnection();
            var profile = await conn.QuerySingleOrDefaultAsync<CentralProfile>(
                "SELECT * FROM CentralProfile WHERE Id = @Id AND UserAccountId = @AccountId",
                new { Id = id, AccountId = accountId });

            return profile == null
                ? Results.NotFound()
                : Results.Ok(new ProfileDto(profile.Id, profile.AvatarName, profile.AvatarEmoji, profile.TotalScore, profile.LastSyncedAt));
        }

        private static async Task<IResult> CreateProfile(
            CreateProfileDto dto,
            ClaimsPrincipal user,
            ApiDbContext db)
        {
            var accountId = GetUserAccountId(user);
            if (accountId == null) return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.AvatarName))
                return Results.BadRequest(new { error = "AvatarName is required." });

            var now = DateTime.UtcNow;
            var avatarEmoji = string.IsNullOrWhiteSpace(dto.AvatarEmoji) ? "🧒" : dto.AvatarEmoji.Trim();

            using var conn = db.CreateConnection();
            await conn.ExecuteAsync(
                @"INSERT INTO CentralProfile (UserAccountId, AvatarName, AvatarEmoji, TotalScore, CreatedAt, UpdatedAt, LastSyncedAt)
                  VALUES (@UserAccountId, @AvatarName, @AvatarEmoji, @TotalScore, @CreatedAt, @UpdatedAt, @LastSyncedAt)",
                new
                {
                    UserAccountId = accountId.Value,
                    AvatarName = dto.AvatarName.Trim(),
                    AvatarEmoji = avatarEmoji,
                    TotalScore = dto.TotalScore,
                    CreatedAt = now,
                    UpdatedAt = now,
                    LastSyncedAt = now
                });

            var newId = await conn.QuerySingleAsync<int>("SELECT last_insert_rowid()");
            var profile = await conn.QuerySingleAsync<CentralProfile>(
                "SELECT * FROM CentralProfile WHERE Id = @Id", new { Id = newId });

            return Results.Created(
                $"/api/profiles/{profile.Id}",
                new ProfileDto(profile.Id, profile.AvatarName, profile.AvatarEmoji, profile.TotalScore, profile.LastSyncedAt));
        }

        private static async Task<IResult> UpdateProfile(
            int id,
            UpdateProfileDto dto,
            ClaimsPrincipal user,
            ApiDbContext db)
        {
            var accountId = GetUserAccountId(user);
            if (accountId == null) return Results.Unauthorized();

            using var conn = db.CreateConnection();
            var profile = await conn.QuerySingleOrDefaultAsync<CentralProfile>(
                "SELECT * FROM CentralProfile WHERE Id = @Id AND UserAccountId = @AccountId",
                new { Id = id, AccountId = accountId });

            if (profile == null) return Results.NotFound();

            var newName = string.IsNullOrWhiteSpace(dto.AvatarName) ? profile.AvatarName : dto.AvatarName.Trim();
            var newEmoji = string.IsNullOrWhiteSpace(dto.AvatarEmoji) ? profile.AvatarEmoji : dto.AvatarEmoji.Trim();
            var newScore = dto.TotalScore ?? profile.TotalScore;
            var now = DateTime.UtcNow;

            await conn.ExecuteAsync(
                @"UPDATE CentralProfile
                  SET AvatarName = @AvatarName, AvatarEmoji = @AvatarEmoji, TotalScore = @TotalScore,
                      UpdatedAt = @UpdatedAt, LastSyncedAt = @LastSyncedAt
                  WHERE Id = @Id",
                new { AvatarName = newName, AvatarEmoji = newEmoji, TotalScore = newScore, UpdatedAt = now, LastSyncedAt = now, Id = id });

            var updated = await conn.QuerySingleAsync<CentralProfile>(
                "SELECT * FROM CentralProfile WHERE Id = @Id", new { Id = id });

            return Results.Ok(new ProfileDto(updated.Id, updated.AvatarName, updated.AvatarEmoji, updated.TotalScore, updated.LastSyncedAt));
        }

        private static async Task<IResult> DeleteProfile(int id, ClaimsPrincipal user, ApiDbContext db)
        {
            var accountId = GetUserAccountId(user);
            if (accountId == null) return Results.Unauthorized();

            using var conn = db.CreateConnection();
            var rows = await conn.ExecuteAsync(
                "DELETE FROM CentralProfile WHERE Id = @Id AND UserAccountId = @AccountId",
                new { Id = id, AccountId = accountId });

            return rows == 0 ? Results.NotFound() : Results.NoContent();
        }

        public record ProfileDto(int Id, string AvatarName, string AvatarEmoji, int TotalScore, DateTime? LastSyncedAt);
        public record CreateProfileDto(string AvatarName, string AvatarEmoji, int TotalScore = 0);
        public record UpdateProfileDto(string? AvatarName, string? AvatarEmoji, int? TotalScore);
    }
}
