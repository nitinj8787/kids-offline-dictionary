using Android.Content;
using Android.Speech;
using KidsDictionaryApp.Services.Interfaces;

namespace KidsDictionaryApp.Platforms.Android
{
    public class AndroidSpeechService : ISpeechService
    {
        public Task<string?> ListenAsync()
        {
            // For MVP: stub for voice recognition. Replace with full implementation via SpeechRecognizer.
            return Task.FromResult<string?>(null);
        }
    }
}