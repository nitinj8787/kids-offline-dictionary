using KidsDictionaryApp.Services.Interfaces;

namespace KidsDictionaryApp.Platforms.MacCatalyst
{
    public class MacSpeechService : ISpeechService
    {
        public Task<string?> ListenAsync()
        {
            // Stub for macOS voice recognition. Replace with NSSpeechRecognizer implementation.
            return Task.FromResult<string?>(null);
        }
    }
}
