using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using KidsDictionaryApi.Data;
using KidsDictionaryApi.Services;

namespace KidsDictionaryApi.Tests;

/// <summary>
/// Unit tests for the OtpService — generation and validation of one-time passwords.
/// </summary>
public class OtpServiceTests : IAsyncLifetime, IDisposable
{
    private readonly string _dbPath;
    private readonly ApiDbContext _db;
    private readonly OtpService _service;

    public OtpServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"otp_test_{Guid.NewGuid()}.db");

        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;
        _db = new ApiDbContext(options);
        _db.Database.EnsureCreated();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Otp:ExpiryMinutes"] = "10"
            })
            .Build();

        _service = new OtpService(_db, config);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _db.DisposeAsync();

    public void Dispose()
    {
        _db.Dispose();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    [Fact]
    public async Task GenerateOtp_Returns6DigitCode()
    {
        var code = await _service.GenerateOtpAsync("test@example.com");

        Assert.Equal(6, code.Length);
        Assert.True(code.All(char.IsDigit));
    }

    [Fact]
    public async Task GenerateOtp_SavesRecordToDatabase()
    {
        var email = "save@example.com";
        var code = await _service.GenerateOtpAsync(email);

        var record = await _db.OtpRecords
            .FirstOrDefaultAsync(o => o.Email == email && o.Code == code);

        Assert.NotNull(record);
        Assert.False(record!.IsUsed);
        Assert.True(record.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task ValidateOtp_ValidCode_ReturnsTrue()
    {
        var email = "valid@example.com";
        var code = await _service.GenerateOtpAsync(email);

        var result = await _service.ValidateOtpAsync(email, code);

        Assert.True(result);
    }

    [Fact]
    public async Task ValidateOtp_ValidCode_MarksOtpAsUsed()
    {
        var email = "used@example.com";
        var code = await _service.GenerateOtpAsync(email);

        await _service.ValidateOtpAsync(email, code);

        var record = await _db.OtpRecords
            .FirstOrDefaultAsync(o => o.Email == email && o.Code == code);

        Assert.NotNull(record);
        Assert.True(record!.IsUsed);
    }

    [Fact]
    public async Task ValidateOtp_InvalidCode_ReturnsFalse()
    {
        var email = "wrong@example.com";
        await _service.GenerateOtpAsync(email);

        var result = await _service.ValidateOtpAsync(email, "000000");

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateOtp_AlreadyUsedCode_ReturnsFalse()
    {
        var email = "reuse@example.com";
        var code = await _service.GenerateOtpAsync(email);

        // First use — succeeds
        var first = await _service.ValidateOtpAsync(email, code);
        Assert.True(first);

        // Second use — must fail
        var second = await _service.ValidateOtpAsync(email, code);
        Assert.False(second);
    }

    [Fact]
    public async Task ValidateOtp_WrongEmail_ReturnsFalse()
    {
        var code = await _service.GenerateOtpAsync("a@example.com");

        var result = await _service.ValidateOtpAsync("b@example.com", code);

        Assert.False(result);
    }

    [Fact]
    public async Task GenerateOtp_InvalidatesPreviousUnusedOtp()
    {
        var email = "replace@example.com";
        var firstCode = await _service.GenerateOtpAsync(email);

        // Second OTP replaces the first
        await _service.GenerateOtpAsync(email);

        // The first code should now be invalidated
        var firstValid = await _service.ValidateOtpAsync(email, firstCode);
        Assert.False(firstValid);
    }

    [Fact]
    public async Task GenerateOtp_ProducesUniformlySizedCodes()
    {
        // Generate 20 OTPs and confirm all are exactly 6 digits
        var codes = new List<string>();
        for (int i = 0; i < 20; i++)
        {
            codes.Add(await _service.GenerateOtpAsync($"user{i}@example.com"));
        }

        Assert.All(codes, code =>
        {
            Assert.Equal(6, code.Length);
            Assert.True(code.All(char.IsDigit));
        });
    }
}
