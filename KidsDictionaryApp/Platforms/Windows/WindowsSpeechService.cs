using KidsDictionaryApp.Services.Interfaces;

namespace KidsDictionaryApp.Platforms.Windows
{
    public class WindowsSpeechService : ISpeechService
    {
        public Task<string?> ListenAsync()
        {
            // Stub for Windows voice recognition. Replace with SpeechRecognitionEngine implementation.
            return Task.FromResult<string?>(null);
        }
    }
}
