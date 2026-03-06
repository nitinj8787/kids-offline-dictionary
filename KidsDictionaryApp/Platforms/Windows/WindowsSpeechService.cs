using KidsDictionaryApp.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Speech.Recognition;

namespace KidsDictionaryApp.Platforms.Windows
{
    public class WindowsSpeechService : ISpeechService
    {
        private readonly ILogger<WindowsSpeechService> _logger;

        public WindowsSpeechService(ILogger<WindowsSpeechService> logger)
        {
            _logger = logger;
        }

        public Task<string?> ListenAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    using var recognizer = new SpeechRecognitionEngine();
                    recognizer.LoadGrammar(new DictationGrammar());
                    recognizer.SetInputToDefaultAudioDevice();
                    var result = recognizer.Recognize(TimeSpan.FromSeconds(5));
                    return result?.Text;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Windows speech recognition failed.");
                    return (string?)null;
                }
            });
        }
    }
}
