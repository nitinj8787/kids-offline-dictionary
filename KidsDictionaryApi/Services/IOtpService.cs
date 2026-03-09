namespace KidsDictionaryApi.Services
{
    public interface IOtpService
    {
        /// <summary>
        /// Generates a 6-digit OTP for the given email, stores it in the database,
        /// and (in development) returns it. In production this would trigger an email.
        /// </summary>
        Task<string> GenerateOtpAsync(string email);

        /// <summary>
        /// Validates the OTP for the given email. Returns true and marks the OTP as used
        /// if the code is correct and not expired.
        /// </summary>
        Task<bool> ValidateOtpAsync(string email, string code);
    }
}
