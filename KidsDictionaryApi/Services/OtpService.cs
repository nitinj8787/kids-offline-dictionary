using Dapper;
using KidsDictionaryApi.Data;
using KidsDictionaryApi.Models;

namespace KidsDictionaryApi.Services
{
    public class OtpService : IOtpService
    {
        private readonly ApiDbContext _db;
        private readonly int _expiryMinutes;

        public OtpService(ApiDbContext db, IConfiguration configuration)
        {
            _db = db;
            _expiryMinutes = configuration.GetValue<int>("Otp:ExpiryMinutes", 10);
        }

        public async Task<string> GenerateOtpAsync(string email)
        {
            using var conn = _db.CreateConnection();

            // Invalidate any existing unused OTPs for this email
            await conn.ExecuteAsync(
                "UPDATE OtpRecord SET IsUsed = 1 WHERE Email = @Email AND IsUsed = 0 AND ExpiresAt > @Now",
                new { Email = email, Now = DateTime.UtcNow });

            // Generate a cryptographically random 6-digit OTP
            var code = GenerateSecureCode();

            await conn.ExecuteAsync(
                @"INSERT INTO OtpRecord (Email, Code, ExpiresAt, IsUsed, CreatedAt)
                  VALUES (@Email, @Code, @ExpiresAt, 0, @CreatedAt)",
                new
                {
                    Email = email.ToLowerInvariant(),
                    Code = code,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(_expiryMinutes),
                    CreatedAt = DateTime.UtcNow
                });

            return code;
        }

        public async Task<bool> ValidateOtpAsync(string email, string code)
        {
            var normalizedEmail = email.ToLowerInvariant();
            using var conn = _db.CreateConnection();

            var record = await conn.QuerySingleOrDefaultAsync<OtpRecord>(
                @"SELECT * FROM OtpRecord
                  WHERE Email = @Email AND Code = @Code AND IsUsed = 0 AND ExpiresAt > @Now
                  ORDER BY CreatedAt DESC
                  LIMIT 1",
                new { Email = normalizedEmail, Code = code, Now = DateTime.UtcNow });

            if (record == null) return false;

            await conn.ExecuteAsync(
                "UPDATE OtpRecord SET IsUsed = 1 WHERE Id = @Id",
                new { record.Id });

            return true;
        }

        private static string GenerateSecureCode()
        {
            // Use a cryptographically secure random number within 0–999999.
            // Convert bytes to uint to avoid the Math.Abs(int.MinValue) pitfall.
            var bytes = new byte[4];
            System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
            var value = BitConverter.ToUInt32(bytes, 0) % 1_000_000;
            return value.ToString("D6");
        }
    }
}
