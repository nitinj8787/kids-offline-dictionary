using Microsoft.EntityFrameworkCore;
using KidsDictionaryApi.Data;
using KidsDictionaryApi.Models;
using KidsDictionaryApi.Services;

namespace KidsDictionaryApi.Endpoints
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/auth").WithTags("Authentication");

            // POST /api/auth/request-otp
            group.MapPost("/request-otp", RequestOtp)
                .WithName("RequestOtp")
                .WithSummary("Request a one-time password sent to the parent email address.")
                .AllowAnonymous();

            // POST /api/auth/verify-otp
            group.MapPost("/verify-otp", VerifyOtp)
                .WithName("VerifyOtp")
                .WithSummary("Verify the OTP and receive a JWT access token.")
                .AllowAnonymous();
        }

        private static async Task<IResult> RequestOtp(
            RequestOtpDto dto,
            ApiDbContext db,
            IOtpService otpService,
            IConfiguration config,
            ILogger<RequestOtpDto> logger)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return Results.BadRequest(new { error = "Email is required." });

            var email = dto.Email.Trim().ToLowerInvariant();

            // Validate email format
            try { _ = new System.Net.Mail.MailAddress(email); }
            catch { return Results.BadRequest(new { error = "Invalid email address." }); }

            var code = await otpService.GenerateOtpAsync(email);

            // In production this would trigger an email via SendGrid / Azure Communication Services.
            // For development the OTP is returned in the response when ReturnOtpInResponse is true.
            var returnInResponse = config.GetValue<bool>("Otp:ReturnOtpInResponse", false);

            logger.LogInformation("OTP requested for {Email}. Code: {Code}", email, code);

            // Ensure or create the user account
            var account = await db.UserAccounts.FirstOrDefaultAsync(u => u.Email == email);
            if (account == null)
            {
                account = new UserAccount { Email = email, CreatedAt = DateTime.UtcNow };
                db.UserAccounts.Add(account);
                await db.SaveChangesAsync();
            }

            return Results.Ok(new
            {
                message = "OTP sent to your email address. Please check your inbox.",
                otp = returnInResponse ? code : null  // Only exposed in dev mode
            });
        }

        private static async Task<IResult> VerifyOtp(
            VerifyOtpDto dto,
            ApiDbContext db,
            IOtpService otpService,
            ITokenService tokenService)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Code))
                return Results.BadRequest(new { error = "Email and code are required." });

            var email = dto.Email.Trim().ToLowerInvariant();
            var valid = await otpService.ValidateOtpAsync(email, dto.Code.Trim());
            if (!valid)
                return Results.UnprocessableEntity(new { error = "Invalid or expired OTP." });

            // Find or create the account
            var account = await db.UserAccounts.FirstOrDefaultAsync(u => u.Email == email);
            if (account == null)
            {
                account = new UserAccount { Email = email, CreatedAt = DateTime.UtcNow };
                db.UserAccounts.Add(account);
            }

            account.LastLoginAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            var token = tokenService.GenerateToken(account.Id, account.Email);

            return Results.Ok(new
            {
                token,
                userAccountId = account.Id,
                email = account.Email
            });
        }

        public record RequestOtpDto(string Email);
        public record VerifyOtpDto(string Email, string Code);
    }
}
