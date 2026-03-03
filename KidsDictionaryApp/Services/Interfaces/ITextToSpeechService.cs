namespace KidsDictionaryApp.Services.Interfaces
{
    public interface ITextToSpeechService
    {
        Task SpeakAsync(string text);
    }
}