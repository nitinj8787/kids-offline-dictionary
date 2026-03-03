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

            // Database
            builder.Services.AddSingleton<DictionaryDbContext>(sp =>
            {
                var dbPath = Task.Run(() => DatabaseInitializer.CopyDatabaseIfNotExists()).GetAwaiter().GetResult();
                var context = new DictionaryDbContext(dbPath);
                Task.Run(() => context.InitializeAsync()).GetAwaiter().GetResult();
                return context;
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

            return builder.Build();
        }
    }
}
