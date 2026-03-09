using Microsoft.EntityFrameworkCore;
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
            // Invalidate any existing unused OTPs for this email
            var existing = await _db.OtpRecords
                .Where(o => o.Email == email && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();
            foreach (var old in existing)
            {
                old.IsUsed = true;
            }

            // Generate a cryptographically random 6-digit OTP
            var code = GenerateSecureCode();

            _db.OtpRecords.Add(new OtpRecord
            {
                Email = email.ToLowerInvariant(),
                Code = code,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_expiryMinutes),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return code;
        }

        public async Task<bool> ValidateOtpAsync(string email, string code)
        {
            var normalizedEmail = email.ToLowerInvariant();
            var record = await _db.OtpRecords
                .Where(o => o.Email == normalizedEmail
                         && o.Code == code
                         && !o.IsUsed
                         && o.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (record == null) return false;

            record.IsUsed = true;
            await _db.SaveChangesAsync();
            return true;
        }

        private static string GenerateSecureCode()
        {
            // Use a cryptographically secure random number within 0–999999.
            // Convert bytes to uint to avoid the Math.Abs(int.MinValue) pitfall.
            var bytes = new byte[4];
            System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
            var value = (BitConverter.ToUInt32(bytes, 0) % 1_000_000);
            return value.ToString("D6");
        }
    }
}
