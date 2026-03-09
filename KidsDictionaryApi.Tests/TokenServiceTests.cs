using Microsoft.Extensions.Configuration;
using KidsDictionaryApi.Services;

namespace KidsDictionaryApi.Tests;

/// <summary>
/// Unit tests for the TokenService — JWT generation and claim extraction.
/// </summary>
public class TokenServiceTests
{
    private readonly TokenService _service;

    public TokenServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test_secret_key_that_is_at_least_32_characters_long",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:ExpiryMinutes"] = "60"
            })
            .Build();

        _service = new TokenService(config);
    }

    [Fact]
    public void GenerateToken_ReturnsNonEmptyString()
    {
        var token = _service.GenerateToken(1, "test@example.com");
        Assert.NotEmpty(token);
    }

    [Fact]
    public void GenerateToken_IsValidJwt()
    {
        var token = _service.GenerateToken(42, "user@example.com");

        // JWT has exactly 3 parts separated by dots
        var parts = token.Split('.');
        Assert.Equal(3, parts.Length);
    }

    [Fact]
    public void GetUserAccountId_ValidToken_ReturnsCorrectId()
    {
        var token = _service.GenerateToken(99, "user@example.com");
        var id = _service.GetUserAccountId(token);

        Assert.Equal(99, id);
    }

    [Fact]
    public void GetUserAccountId_InvalidToken_ReturnsNull()
    {
        var id = _service.GetUserAccountId("not.a.valid.token");
        Assert.Null(id);
    }

    [Fact]
    public void GetUserAccountId_TamperedToken_ReturnsNull()
    {
        var token = _service.GenerateToken(1, "user@example.com");

        // Tamper with the signature
        var parts = token.Split('.');
        var tampered = parts[0] + "." + parts[1] + ".invalidsignature";

        var id = _service.GetUserAccountId(tampered);
        Assert.Null(id);
    }

    [Fact]
    public void GetUserAccountId_DifferentUsers_ReturnsDifferentIds()
    {
        var token1 = _service.GenerateToken(1, "alice@example.com");
        var token2 = _service.GenerateToken(2, "bob@example.com");

        Assert.Equal(1, _service.GetUserAccountId(token1));
        Assert.Equal(2, _service.GetUserAccountId(token2));
    }
}
