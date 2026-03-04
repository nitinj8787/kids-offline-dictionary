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
                
                // Copy pre-populated database from Resources\Raw if it doesn't exist or is invalid
                if (!File.Exists(dbPath) || !IsValidSQLiteDatabase(dbPath))
                {
                    try
                    {
                        // Remove any invalid/incomplete file before copying
                        if (File.Exists(dbPath))
                        {
                            File.Delete(dbPath);
                        }

                        // Ensure directory exists
                        var directory = Path.GetDirectoryName(dbPath);
                        if (!string.IsNullOrEmpty(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        // Open source stream from app package
                        using var stream = FileSystem.OpenAppPackageFileAsync("dictionary.db").GetAwaiter().GetResult();
                        
                        // Create destination file and copy
                        using (var fileStream = new FileStream(dbPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            stream.CopyTo(fileStream);
                            fileStream.Flush(flushToDisk: true);
                        }
                        
                        // Verify the file was created successfully and is a valid SQLite database
                        if (!File.Exists(dbPath) || new FileInfo(dbPath).Length == 0 || !IsValidSQLiteDatabase(dbPath))
                        {
                            throw new InvalidOperationException("Database file was not copied correctly.");
                        }
                    }
                    catch (Exception ex)
                    {
                        // If copy fails, delete the incomplete file
                        if (File.Exists(dbPath))
                        {
                            File.Delete(dbPath);
                        }
                        throw new InvalidOperationException($"Failed to initialize database: {ex.Message}", ex);
                    }
                }
                
                return new DictionaryDbContext(dbPath);
            });

            // Repositories & Services
            builder.Services.AddSingleton<IWordRepository, WordRepository>();
            builder.Services.AddSingleton<IDictionaryService, DictionaryService>();
            builder.Services.AddSingleton<IWordHistoryService, WordHistoryService>();
            builder.Services.AddSingleton<IFavoritesService, FavoritesService>();
            builder.Services.AddSingleton<ITextToSpeechService, MauiTextToSpeechService>();

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

#if DEBUG
        // Development-only method to create/regenerate the database file in Resources\Raw
        // Run this once manually when you need to update the bundled database
        public static async Task RegenerateRawDatabaseAsync()
        {
            var projectPath = @"C:\Users\nitin\source\repos\KidsDictionaryApp\KidsDictionaryApp";
            var rawDbPath = Path.Combine(projectPath, "Resources", "Raw", "dictionary.db");

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(rawDbPath));

            // Delete if exists to recreate
            if (File.Exists(rawDbPath))
                File.Delete(rawDbPath);

            await DatabaseSeeder.CreatePrePopulatedDatabase(rawDbPath);
            Console.WriteLine($"Database created successfully at: {rawDbPath}");
            Console.WriteLine("Make sure the file is set to 'MauiAsset' build action in the .csproj");
        }
#endif
    }
}
