using Android.Content;
using Android.Speech;
using KidsDictionaryApp.Services.Interfaces;

namespace KidsDictionaryApp.Platforms.Android
{
    public class AndroidSpeechService : ISpeechService
    {
        public Task<string?> ListenAsync()
        {
            // For MVP: use MAUI built-in SpeechRecognition if available
            return Task.FromResult<string?>("example"); // stub for now
        }
    }
}