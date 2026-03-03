public interface ITextToSpeechService
{
    Task SpeakAsync(string text);
}