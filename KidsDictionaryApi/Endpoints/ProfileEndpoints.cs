using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
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

            var profiles = await db.CentralProfiles
                .Where(p => p.UserAccountId == accountId)
                .OrderBy(p => p.CreatedAt)
                .Select(p => new ProfileDto(p.Id, p.AvatarName, p.AvatarEmoji, p.TotalScore, p.LastSyncedAt))
                .ToListAsync();

            return Results.Ok(profiles);
        }

        private static async Task<IResult> GetProfile(int id, ClaimsPrincipal user, ApiDbContext db)
        {
            var accountId = GetUserAccountId(user);
            if (accountId == null) return Results.Unauthorized();

            var profile = await db.CentralProfiles
                .Where(p => p.Id == id && p.UserAccountId == accountId)
                .FirstOrDefaultAsync();

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

            var profile = new CentralProfile
            {
                UserAccountId = accountId.Value,
                AvatarName = dto.AvatarName.Trim(),
                AvatarEmoji = string.IsNullOrWhiteSpace(dto.AvatarEmoji) ? "🧒" : dto.AvatarEmoji.Trim(),
                TotalScore = dto.TotalScore,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastSyncedAt = DateTime.UtcNow
            };

            db.CentralProfiles.Add(profile);
            await db.SaveChangesAsync();

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

            var profile = await db.CentralProfiles
                .Where(p => p.Id == id && p.UserAccountId == accountId)
                .FirstOrDefaultAsync();

            if (profile == null) return Results.NotFound();

            if (!string.IsNullOrWhiteSpace(dto.AvatarName))
                profile.AvatarName = dto.AvatarName.Trim();

            if (!string.IsNullOrWhiteSpace(dto.AvatarEmoji))
                profile.AvatarEmoji = dto.AvatarEmoji.Trim();

            if (dto.TotalScore.HasValue)
                profile.TotalScore = dto.TotalScore.Value;

            profile.UpdatedAt = DateTime.UtcNow;
            profile.LastSyncedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Results.Ok(new ProfileDto(profile.Id, profile.AvatarName, profile.AvatarEmoji, profile.TotalScore, profile.LastSyncedAt));
        }

        private static async Task<IResult> DeleteProfile(int id, ClaimsPrincipal user, ApiDbContext db)
        {
            var accountId = GetUserAccountId(user);
            if (accountId == null) return Results.Unauthorized();

            var profile = await db.CentralProfiles
                .Where(p => p.Id == id && p.UserAccountId == accountId)
                .FirstOrDefaultAsync();

            if (profile == null) return Results.NotFound();

            db.CentralProfiles.Remove(profile);
            await db.SaveChangesAsync();

            return Results.NoContent();
        }

        public record ProfileDto(int Id, string AvatarName, string AvatarEmoji, int TotalScore, DateTime? LastSyncedAt);
        public record CreateProfileDto(string AvatarName, string AvatarEmoji, int TotalScore = 0);
        public record UpdateProfileDto(string? AvatarName, string? AvatarEmoji, int? TotalScore);
    }
}
