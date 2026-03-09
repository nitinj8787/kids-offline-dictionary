using System.Security.Claims;
using Dapper;
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

            using var conn = db.CreateConnection();
            await conn.ExecuteAsync(
                @"INSERT INTO AppUsage (UserAccountId, CentralProfileId, EventType, EventData, CreatedAt)
                  VALUES (@UserAccountId, @CentralProfileId, @EventType, @EventData, @CreatedAt)",
                new
                {
                    UserAccountId = accountId.Value,
                    dto.CentralProfileId,
                    EventType = dto.EventType.Trim(),
                    dto.EventData,
                    CreatedAt = DateTime.UtcNow
                });

            return Results.Ok(new { message = "Usage recorded." });
        }

        private static async Task<IResult> GetUsage(ClaimsPrincipal user, ApiDbContext db)
        {
            var accountId = GetUserAccountId(user);
            if (accountId == null) return Results.Unauthorized();

            using var conn = db.CreateConnection();
            var usages = await conn.QueryAsync<AppUsage>(
                @"SELECT * FROM AppUsage WHERE UserAccountId = @AccountId
                  ORDER BY CreatedAt DESC LIMIT 100",
                new { AccountId = accountId });

            return Results.Ok(usages.Select(u =>
                new UsageDto(u.Id, u.CentralProfileId, u.EventType, u.EventData, u.CreatedAt)));
        }

        public record RecordUsageDto(string EventType, int? CentralProfileId = null, string? EventData = null);
        public record UsageDto(int Id, int? CentralProfileId, string EventType, string? EventData, DateTime CreatedAt);
    }
}
