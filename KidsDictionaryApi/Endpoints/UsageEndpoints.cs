using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using KidsDictionaryApi.Data;
using KidsDictionaryApi.Models;

namespace KidsDictionaryApi.Endpoints
{
    public static class UsageEndpoints
    {
        public static void MapUsageEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/usage")
                .WithTags("Usage")
                .RequireAuthorization();

            // POST /api/usage — record an app usage event
            group.MapPost("/", RecordUsage).WithName("RecordUsage");

            // GET /api/usage — get usage events for the authenticated account
            group.MapGet("/", GetUsage).WithName("GetUsage");
        }

        private static int? GetUserAccountId(ClaimsPrincipal user)
        {
            var claim = user.FindFirst("userAccountId") ?? user.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
        }

        private static async Task<IResult> RecordUsage(
            RecordUsageDto dto,
            ClaimsPrincipal user,
            ApiDbContext db)
        {
            var accountId = GetUserAccountId(user);
            if (accountId == null) return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.EventType))
                return Results.BadRequest(new { error = "EventType is required." });

            db.AppUsages.Add(new AppUsage
            {
                UserAccountId = accountId.Value,
                CentralProfileId = dto.CentralProfileId,
                EventType = dto.EventType.Trim(),
                EventData = dto.EventData,
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Usage recorded." });
        }

        private static async Task<IResult> GetUsage(ClaimsPrincipal user, ApiDbContext db)
        {
            var accountId = GetUserAccountId(user);
            if (accountId == null) return Results.Unauthorized();

            var usages = await db.AppUsages
                .Where(u => u.UserAccountId == accountId)
                .OrderByDescending(u => u.CreatedAt)
                .Take(100)
                .Select(u => new UsageDto(u.Id, u.CentralProfileId, u.EventType, u.EventData, u.CreatedAt))
                .ToListAsync();

            return Results.Ok(usages);
        }

        public record RecordUsageDto(string EventType, int? CentralProfileId = null, string? EventData = null);
        public record UsageDto(int Id, int? CentralProfileId, string EventType, string? EventData, DateTime CreatedAt);
    }
}
