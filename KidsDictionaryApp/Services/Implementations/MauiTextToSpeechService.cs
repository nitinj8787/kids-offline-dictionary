using KidsDictionaryApp.Services.Interfaces;

namespace KidsDictionaryApp.Services.Implementations
{
    public class MauiTextToSpeechService : ITextToSpeechService
    {
        public Task SpeakAsync(string text)
        {
            return TextToSpeech.Default.SpeakAsync(text);
        }
    }
}
