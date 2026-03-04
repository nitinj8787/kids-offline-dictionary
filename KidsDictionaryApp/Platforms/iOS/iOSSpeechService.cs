using KidsDictionaryApp.Services.Interfaces;

namespace KidsDictionaryApp.Platforms.iOS
{
    public class iOSSpeechService : ISpeechService
    {
        public Task<string?> ListenAsync()
        {
            // Stub for iOS voice recognition. Replace with AVSpeechRecognizer implementation.
            return Task.FromResult<string?>(null);
        }
    }
}
