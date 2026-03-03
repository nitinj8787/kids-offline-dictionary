using KidsDictionaryApp.Models;

namespace KidsDictionaryApp.Services.Interfaces
{
    public interface IWordRepository
    {
        Task<Word?> GetWordAsync(string word);
        Task<List<Word>> GetAllWordsAsync();
    }
}