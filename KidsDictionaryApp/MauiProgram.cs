using Microsoft.Extensions.Logging;
using KidsDictionaryApp.Data;
using KidsDictionaryApp.Services.Interfaces;
using KidsDictionaryApp.Services.Implementations;
using KidsDictionaryApp.ViewModels;

namespace KidsDictionaryApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            // Database - will copy from Resources\Raw on first run
            builder.Services.AddSingleton<DictionaryDbContext>(sp =>
            {
                var dbPath = Path.Combine(FileSystem.AppDataDirectory, "dictionary.db");

                // Add debugging
                System.Diagnostics.Debug.WriteLine($"[DB] App data directory: {FileSystem.AppDataDirectory}");
                System.Diagnostics.Debug.WriteLine($"[DB] Target path: {dbPath}");
                System.Diagnostics.Debug.WriteLine($"[DB] File exists: {File.Exists(dbPath)}");

                // Copy pre-populated database from Resources\Raw if it doesn't exist or is invalid
                if (!File.Exists(dbPath) || !IsValidSQLiteDatabase(dbPath))
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"[DB] Attempting to copy from app package...");

                        // Remove any invalid/incomplete file before copying
                        if (File.Exists(dbPath))
                        {
                            File.Delete(dbPath);
                            System.Diagnostics.Debug.WriteLine($"[DB] Deleted invalid existing file");
                        }

                        // Ensure directory exists
                        var directory = Path.GetDirectoryName(dbPath);
                        if (!string.IsNullOrEmpty(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        // Open source stream from app package
                        Stream? stream = null;
                        bool foundDatabase = false;

#if WINDOWS
                        // For Windows, try multiple possible locations
                        var possiblePaths = new[]
                        {
                            Path.Combine(AppContext.BaseDirectory, "dictionary.db"),
                            Path.Combine(AppContext.BaseDirectory, "Resources", "Raw", "dictionary.db"),
                            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dictionary.db"),
                            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Raw", "dictionary.db")
                        };

                        System.Diagnostics.Debug.WriteLine($"[DB] Windows: Searching for database...");
                        foreach (var path in possiblePaths)
                        {
                            System.Diagnostics.Debug.WriteLine($"[DB] Windows: Checking: {path}");
                            if (File.Exists(path))
                            {
                                System.Diagnostics.Debug.WriteLine($"[DB] Windows: Found database at: {path}");
                                stream = File.OpenRead(path);
                                foundDatabase = true;
                                break;
                            }
                        }

                        if (!foundDatabase)
                        {
                            System.Diagnostics.Debug.WriteLine($"[DB] Windows: Database not found in file system, trying app package");
#endif
                        try
                        {
                            stream = FileSystem.OpenAppPackageFileAsync("dictionary.db").GetAwaiter().GetResult();
                            foundDatabase = true;
                            System.Diagnostics.Debug.WriteLine($"[DB] Found database in app package root");
                        }
                        catch
                        {
                            System.Diagnostics.Debug.WriteLine($"[DB] Not in app package root, trying Resources/Raw/dictionary.db");
                            stream = FileSystem.OpenAppPackageFileAsync("Resources/Raw/dictionary.db").GetAwaiter().GetResult();
                            foundDatabase = true;
                            System.Diagnostics.Debug.WriteLine($"[DB] Found database in Resources/Raw");
                        }
#if WINDOWS
                        }
#endif

                        if (!foundDatabase || stream == null)
                        {
                            throw new FileNotFoundException("Could not locate dictionary.db in app package");
                        }

                        using (stream)
                        {
                            // Create destination file and copy
                            using (var fileStream = new FileStream(dbPath, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                stream.CopyTo(fileStream);
                                fileStream.Flush(flushToDisk: true);
                            }
                        }

                        System.Diagnostics.Debug.WriteLine($"[DB] File copied, size: {new FileInfo(dbPath).Length}");

                        // Verify the file was created successfully and is a valid SQLite database
                        if (!File.Exists(dbPath) || new FileInfo(dbPath).Length == 0 || !IsValidSQLiteDatabase(dbPath))
                        {
                            throw new InvalidOperationException("Database file was not copied correctly.");
                        }

                        System.Diagnostics.Debug.WriteLine($"[DB] Database initialized successfully");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DB] ERROR: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"[DB] Stack trace: {ex.StackTrace}");

                        // Fallback: Create empty database with schema
                        System.Diagnostics.Debug.WriteLine($"[DB] FALLBACK: Creating empty database with schema");
                        try
                        {
                            if (File.Exists(dbPath))
                            {
                                File.Delete(dbPath);
                            }

                            var context = new DictionaryDbContext(dbPath);
                            context.InitializeAsync().GetAwaiter().GetResult();
                            System.Diagnostics.Debug.WriteLine($"[DB] Empty database created successfully as fallback");
                            return context;
                        }
                        catch (Exception innerEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"[DB] CRITICAL: Failed to create fallback database: {innerEx.Message}");
                            throw new InvalidOperationException($"Failed to initialize database: {ex.Message}", ex);
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DB] Using existing database at {dbPath}");
                }

                return new DictionaryDbContext(dbPath);
            });

            // Repositories & Services
            builder.Services.AddSingleton<IWordRepository, WordRepository>();
            builder.Services.AddSingleton<IDictionaryService, DictionaryService>();
            builder.Services.AddSingleton<IWordHistoryService, WordHistoryService>();
            builder.Services.AddSingleton<IFavoritesService, FavoritesService>();
            builder.Services.AddSingleton<IProfileService, ProfileService>();
            builder.Services.AddSingleton<IProgressService, ProgressService>();
            builder.Services.AddSingleton<IAchievementService, AchievementService>();

            // Centralized profile sync service
            // The base URL should be set to the deployed Azure App Service URL in production.
            // Leave it empty to disable sync (offline-only mode).
            var apiBaseUrl = ""; // e.g. "https://your-api.azurewebsites.net"
            builder.Services.AddHttpClient<ISyncService, SyncService>(client =>
            {
                if (!string.IsNullOrWhiteSpace(apiBaseUrl))
                    client.BaseAddress = new Uri(apiBaseUrl);
            });
            // Platform-specific text-to-speech
#if ANDROID
            builder.Services.AddSingleton<ITextToSpeechService, KidsDictionaryApp.Platforms.Android.AndroidTextToSpeechService>();
#elif WINDOWS
            builder.Services.AddSingleton<ITextToSpeechService, KidsDictionaryApp.Platforms.Windows.WindowsTextToSpeechService>();
#else
            builder.Services.AddSingleton<ITextToSpeechService, MauiTextToSpeechService>();
#endif

            // Platform-specific speech recognition
#if ANDROID
            builder.Services.AddSingleton<ISpeechService, KidsDictionaryApp.Platforms.Android.AndroidSpeechService>();
#elif IOS
            builder.Services.AddSingleton<ISpeechService, KidsDictionaryApp.Platforms.iOS.iOSSpeechService>();
#elif MACCATALYST
            builder.Services.AddSingleton<ISpeechService, KidsDictionaryApp.Platforms.MacCatalyst.MacSpeechService>();
#elif WINDOWS
            builder.Services.AddSingleton<ISpeechService, KidsDictionaryApp.Platforms.Windows.WindowsSpeechService>();
#endif

            // ViewModels
            builder.Services.AddTransient<DictionaryViewModel>();

#if DEBUG
            // Verify database is bundled
            //_ = Task.Run(async () => await RegenerateRawDatabaseAsync());
#endif

            return builder.Build();
        }

        private static readonly byte[] SqliteMagicHeader = new byte[] { 0x53, 0x51, 0x4C, 0x69, 0x74, 0x65, 0x20, 0x66, 0x6F, 0x72, 0x6D, 0x61, 0x74, 0x20, 0x33, 0x00 };

        private static bool IsValidSQLiteDatabase(string path)
        {
            const int SQLiteHeaderSize = 16;
            try
            {
                using var stream = File.OpenRead(path);
                var header = new byte[SQLiteHeaderSize];
                if (stream.Read(header, 0, SQLiteHeaderSize) < SQLiteHeaderSize) return false;
                // SQLite databases begin with the magic string "SQLite format 3\0"
                return header.SequenceEqual(SqliteMagicHeader);
            }
            catch
            {
                return false;
            }
        }
    }
}
