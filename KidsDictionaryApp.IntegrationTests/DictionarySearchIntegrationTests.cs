using SQLite;

namespace KidsDictionaryApp.IntegrationTests;

/// <summary>
/// Integration tests for searching words in the dictionary.db SQLite database.
/// </summary>
public class DictionarySearchIntegrationTests : IAsyncLifetime, IDisposable
{
    private readonly string _dbPath;
    private SQLiteAsyncConnection _db = null!;

    public DictionarySearchIntegrationTests()
    {
        // Copy the bundled dictionary.db to a temp path so each test run is isolated
        var sourcePath = Path.Combine(AppContext.BaseDirectory, "dictionary.db");
        _dbPath = Path.Combine(Path.GetTempPath(), $"dict_test_{Guid.NewGuid()}.db");
        File.Copy(sourcePath, _dbPath, overwrite: true);
    }

    public async Task InitializeAsync()
    {
        _db = new SQLiteAsyncConnection(_dbPath);
        // Ensure the Word table exists (it should already from the bundled db)
        await _db.CreateTableAsync<Word>();
    }

    public async Task DisposeAsync()
    {
        if (_db != null)
            await _db.CloseAsync();
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    [Fact]
    public async Task SearchWord_ExactMatch_ReturnsCorrectWord()
    {
        var result = await _db.Table<Word>()
            .Where(w => w.WordText.ToLower() == "apple")
            .FirstOrDefaultAsync();

        Assert.NotNull(result);
        Assert.Equal("apple", result.WordText);
        Assert.NotNull(result.Meaning);
        Assert.NotEmpty(result.Meaning);
    }

    [Fact]
    public async Task SearchWord_CaseInsensitive_ReturnsWord()
    {
        var searchTerm = "CAT";

        var result = await _db.Table<Word>()
            .Where(w => w.WordText.ToLower() == searchTerm.ToLower())
            .FirstOrDefaultAsync();

        Assert.NotNull(result);
        Assert.Equal("cat", result.WordText);
    }

    [Fact]
    public async Task SearchWord_NotFound_ReturnsNull()
    {
        var result = await _db.Table<Word>()
            .Where(w => w.WordText.ToLower() == "zzznonsenseword")
            .FirstOrDefaultAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task SearchWord_ReturnsWordWithMeaningExampleAndSynonyms()
    {
        var result = await _db.Table<Word>()
            .Where(w => w.WordText.ToLower() == "dog")
            .FirstOrDefaultAsync();

        Assert.NotNull(result);
        Assert.NotNull(result.Meaning);
        Assert.NotNull(result.Example);
        Assert.NotNull(result.Synonyms);
    }

    [Fact]
    public async Task GetAllWords_ReturnsMultipleWords()
    {
        var words = await _db.Table<Word>().ToListAsync();

        Assert.NotNull(words);
        Assert.NotEmpty(words);
    }

    [Fact]
    public async Task GetAllWords_CanBeSortedAndGroupedAlphabetically()
    {
        var words = await _db.Table<Word>().ToListAsync();

        var sorted = words.Where(w => !string.IsNullOrEmpty(w.WordText)).OrderBy(w => w.WordText).ToList();
        var groups = sorted
            .GroupBy(w => w.WordText[0].ToString().ToUpper())
            .OrderBy(g => g.Key)
            .ToList();

        Assert.NotEmpty(groups);
        // Each group key should be a single uppercase letter
        foreach (var group in groups)
        {
            Assert.Single(group.Key);
            Assert.True(char.IsLetter(group.Key[0]));
        }
    }

    [Fact]
    public async Task SearchWord_EmptyString_ReturnsNull()
    {
        // Mirrors DictionaryService behavior: empty/whitespace returns null
        var searchTerm = "   ";
        Word? result = null;

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            result = await _db.Table<Word>()
                .Where(w => w.WordText.ToLower() == searchTerm.Trim().ToLower())
                .FirstOrDefaultAsync();
        }

        Assert.Null(result);
    }

    [Theory]
    [InlineData("apple")]
    [InlineData("butterfly")]
    [InlineData("elephant")]
    [InlineData("happy")]
    [InlineData("jungle")]
    public async Task SearchWord_KnownWords_AreFoundInDatabase(string wordText)
    {
        var result = await _db.Table<Word>()
            .Where(w => w.WordText.ToLower() == wordText.ToLower())
            .FirstOrDefaultAsync();

        Assert.NotNull(result);
        Assert.Equal(wordText, result.WordText);
    }
}

/// <summary>
/// Represents a word entry in the dictionary database.
/// Mirrors KidsDictionaryApp.Models.Word.
/// </summary>
[Table("Word")]
public class Word
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public string WordText { get; set; } = string.Empty;

    public string Meaning { get; set; } = string.Empty;

    public string PartOfSpeech { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Example { get; set; } = string.Empty;

    public string Phonics { get; set; } = string.Empty;

    public string Syllables { get; set; } = string.Empty;

    public string Synonyms { get; set; } = string.Empty;

    public string Antonyms { get; set; } = string.Empty;

    public int DifficultyLevel { get; set; }

    public int FrequencyRank { get; set; }
}
