using KidsDictionaryApp.Models;

namespace KidsDictionaryApp.Services.Interfaces
{
    public interface IDictionaryService
    {
        Task<Word?> GetWordAsync(string word);
    }
}