namespace KidsDictionaryApp.Services.Interfaces
{
    public interface ISpeechService
    {
        Task<string?> ListenAsync();
    }
}
