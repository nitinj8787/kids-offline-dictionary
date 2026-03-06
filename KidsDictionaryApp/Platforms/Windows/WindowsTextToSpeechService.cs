using KidsDictionaryApp.Services.Interfaces;
using System.Speech.Synthesis;

namespace KidsDictionaryApp.Platforms.Windows
{
    public class WindowsTextToSpeechService : ITextToSpeechService, IDisposable
    {
        private readonly SpeechSynthesizer _synthesizer;

        public WindowsTextToSpeechService()
        {
            _synthesizer = new SpeechSynthesizer();
            _synthesizer.SetOutputToDefaultAudioDevice();
        }

        public Task SpeakAsync(string text)
        {
            return Task.Run(() => _synthesizer.Speak(text));
        }

        public void Dispose()
        {
            _synthesizer.Dispose();
        }
    }
}
