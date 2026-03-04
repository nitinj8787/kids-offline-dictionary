using Microsoft.Maui.Storage;

namespace KidsDictionaryApp.Data
{
    public static class DatabaseInitializer
    {
        public static async Task<string> CopyDatabaseIfNotExists()
        {
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "dictionary.db");

            if (!File.Exists(dbPath))
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("dictionary.db");
                using var newStream = File.Create(dbPath);
                await stream.CopyToAsync(newStream);
                await newStream.FlushAsync(); // Ensure data is written to disk
            }

            return dbPath;
        }
    }
}