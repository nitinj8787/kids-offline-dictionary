namespace KidsDictionaryApi.Services
{
    public interface ITokenService
    {
        /// <summary>Generates a signed JWT for the given user account ID and email.</summary>
        string GenerateToken(int userAccountId, string email);

        /// <summary>Extracts the UserAccountId claim from a valid JWT. Returns null if invalid.</summary>
        int? GetUserAccountId(string token);
    }
}
